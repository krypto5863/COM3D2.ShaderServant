using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CM3D2.Serialization;
using CM3D2.Serialization.Files;
using HarmonyLib;
using Mono.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Material = UnityEngine.Material;
using SecurityAction = System.Security.Permissions.SecurityAction;

//These two lines tell your plugin to not give a flying fuck about accessing private variables/classes whatever. It requires a publicized stub of the library with those private objects though.
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShaderServant
{
	//This is the metadata set for your plugin.
	[BepInPlugin(GUID, Name, Version)]
	public class ShaderServant : BaseUnityPlugin
	{
		public const string GUID = "org.krypto5863.com3d2.shaderservant";
		public const string Name = "ShaderServant";
		public const string Version = "1.4";

		//static saving of the main instance. This makes it easier to run stuff like co-routines from static methods or accessing non-static vars.
		public static ShaderServant Instance { get; private set; }

		//Static var for the logger so you can log from other classes.
		internal static ManualLogSource PluginLogger => Instance.Logger;

		//Config entry variable. You set your configs to this.

		//Deep searching is so fast and so beneficial, it makes sense to make this standard behavior.
		//internal static ConfigEntry<bool> DeepSearchForSKMs;

		internal static ConfigEntry<int> ReflectionResolution;
		internal static ConfigEntry<float> ReflectionRange;
		internal static ConfigEntry<string> ReflectionTimeSlicingMode;

		internal static Queue<MeshUpdater> Queue = new Queue<MeshUpdater>();

		private static Material[] _materials;
		public static ReadOnlyCollection<Material> Materials => new ReadOnlyCollection<Material>(_materials.Select(m => new Material(m)).ToArray());

		public static readonly string ShaderDirectory = Paths.GameRootPath + "\\ShaderServantPacks";
		public const string ShaderReportFile = "ShaderReport.txt";

		private void Awake()
		{
			//Useful for engaging co-routines or accessing variables non-static variables. Completely optional though.
			Instance = this;

			var plugs = Directory.GetFiles(Paths.PluginPath, "*", SearchOption.AllDirectories)
				.Select(t => Path.GetFileName(t).ToLower())
				.ToArray();

			var hasDependencies = plugs.Contains("cm3d2.serialization.dll");

			if (!hasDependencies)
			{
				PluginLogger.LogFatal("ShaderServant is missing some dependencies! Your game will now quit.");

				const string message = "ShaderServant is missing CM3D2.Serialization!\nShaderServant が CM3D2.Serialization がないよ!";
				Assert(message, "Missing Reference!");
				//NUty.WinMessageBox(NUty.GetWindowHandle(), message, "Missing Reference!", 0x00000010 | 0x00000000);
			}

			var acceptableValues = new AcceptableValueList<string>(Enum.GetNames(typeof(ReflectionProbeTimeSlicingMode)));

			//Binds the configuration. In other words it sets your ConfigEntry var to your config setup.
			//DeepSearchForSKMs = Config.Bind("General", "Deep Search SkinnedMeshRenderers", false, "Not suggested, it can cause performance hikes when a third party plugin spawns an NPR item and it isn't directly supported by SS. However, it will make things work properly.");
			ReflectionTimeSlicingMode = Config.Bind("Reflections", "Time Slicing Mode", acceptableValues.AcceptableValues.FirstOrDefault(), new ConfigDescription("The refresh mode. This is how the reflections are rendered. NoTimeSlicing is worst performance, quickest reflection reaction. AllFacesAtOnce is the middle ground. IndividualFaces is the lightest but least responsive.", acceptableValues));
			ReflectionResolution = Config.Bind("Reflections", "Resolution", 128, new ConfigDescription("The resolution of reflections. More means better but also more intensive.", new AcceptableValueList<int>(16, 32, 64, 128, 256, 512, 1024, 2048)));
			ReflectionRange = Config.Bind("Reflections", "Range", 512f, "From how far away a reflecting object will reflect. This is the relationship between your camera and object's distance.");

			ReflectionResolution.SettingChanged += (s, e) =>
			{
				if (ReflectionProbeController._instance == null)
				{
					return;
				}

				ReflectionProbeController._instance.Probe.resolution = ReflectionResolution.Value;
			};

			ReflectionRange.SettingChanged += (s, e) =>
			{
				if (ReflectionProbeController._instance == null)
				{
					return;
				}

				ReflectionProbeController._instance.Probe.size = new Vector3(ReflectionRange.Value, ReflectionRange.Value, ReflectionRange.Value);
			};

			ReflectionTimeSlicingMode.SettingChanged += (s, e) =>
			{
				if (ReflectionProbeController._instance == null)
				{
					return;
				}

				ReflectionProbeController._instance.Probe.timeSlicingMode = (ReflectionProbeTimeSlicingMode)Enum.Parse(typeof(ReflectionProbeTimeSlicingMode), ReflectionTimeSlicingMode.Value); ;
			};
			
			SceneManager.sceneLoaded += OnSceneLoaded;
			SceneManager.sceneLoaded += NprShader.OnSceneLoaded;

			//Installs the patches in the Main class.
			var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));

#if DEBUG
			PluginLogger.LogMessage("Boots are strapped!");
#endif
		}

		private static void Assert(string message, string title)
		{
			NUty.WinMessageBox(NUty.GetWindowHandle(), message, title, 0x00000010 | 0x00000000);
			Application.Quit();
		}

		private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			LoadAndListShaders();
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private static bool LoadAndListShaders()
		{
			if (Directory.Exists(ShaderDirectory) == false)
			{
				PluginLogger.LogError("ShaderServant has nothing to load! ShaderServant will disable itself now...");
				return false;
			}

			var shaderPackages = Directory.GetFiles(ShaderDirectory);

			if (shaderPackages.Length == 0)
			{
				PluginLogger.LogError("ShaderServant has nothing to load! ShaderServant will disable itself now...");
				return false;
			}

			IEnumerable<Material> materialList = new List<Material>();

			foreach (var shaderFile in shaderPackages)
			{
				if (Path.GetFileName(shaderFile).Equals(ShaderReportFile))
				{
					continue;
				}

				try
				{
					var shaderBundle = AssetBundle.LoadFromFile(shaderFile);
					materialList = materialList.Concat(shaderBundle.LoadAllAssets<Material>()).ToArray();
					shaderBundle.Unload(false);
				}
				catch
				{
					PluginLogger.LogError($"Failed to load {shaderFile}, are you sure this is an asset bundle?");
				}
			}

			_materials = materialList.ToArray();
			var shaderReport = new StringBuilder();

			foreach (var mat in _materials)
			{
				DontDestroyOnLoad(mat);
				PluginLogger.LogInfo($"\"{mat.shader.name}\" is declared by \"{mat.name}\"");
				shaderReport.AppendLine($"\nShader: {mat.shader.name}\nMaterial: {mat.name}\n");
			}

			File.WriteAllText(ShaderDirectory + "\\" + ShaderReportFile, shaderReport.ToString());
			return true;
		}

		public static bool LoadExternalMaterial(string shaderFileName, ref Material targetMaterial)
		{
			var materialFile = _materials.FirstOrDefault(r => r.shader.name.Equals(shaderFileName, StringComparison.OrdinalIgnoreCase));

			if (materialFile == null)
			{
				return true;
			}

			targetMaterial.shader = materialFile.shader;
			targetMaterial.shaderKeywords = materialFile.shaderKeywords.Clone() as string[];
			targetMaterial.CopyPropertiesFromMaterial(materialFile);

#if DEBUG
			PluginLogger.LogDebug($"Located un-found shader: {targetMaterial.name} | {targetMaterial.shader}");
#endif

			return false;
		}

		public static bool LoadExternalMaterial2(string shaderMaterialName, ref Material material)
		{
			var someMaterial = _materials
				.FirstOrDefault(m => m.name.Equals(shaderMaterialName, StringComparison.OrdinalIgnoreCase));

			if (someMaterial == null)
			{
				return true;
			}

			material = someMaterial;
			return false;
		}

		public static BinaryReader ForceNprCompatibility(BinaryReader reader, string file)
		{
			if (file.IndexOf("_NPRMAT_", 0, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return reader;
			}

			if (!NprMaterialSwap(file, out var shaderName, out var shaderFile))
			{
				PluginLogger.LogError($"{file} has NPR keyword but a shader could not be resolved!");
				return reader;
			}

#if DEBUG
			PluginLogger.LogDebug($"Now reading NPR Material!");
#endif

			var oldPosition = reader.BaseStream.Position;

			reader.BaseStream.Position = 0;

			var serializer = new CM3D2Serializer();

			var mate = serializer.Deserialize<Mate>(reader.BaseStream);

			mate.material.shaderName = shaderName;
			mate.material.shaderFilename = shaderFile;

#if DEBUG
			PluginLogger.LogDebug($"Fields set to {shaderName} {shaderFile}");
#endif

			foreach (var property in mate.material.properties)
			{
#if DEBUG
				PluginLogger.LogDebug($"{property.name} {property.type}");
#endif


				if (property.type.Equals("f", StringComparison.OrdinalIgnoreCase) &&
					property.name.Contains("Toggle"))
				{
					property.name += "_ON_SSKEYWORD";
#if DEBUG
					PluginLogger.LogDebug($"{property.type} renamed to {property.name}");
#endif
				}
			}

			var newStream = new MemoryStream();
			serializer.Serialize(newStream, mate);

			newStream.Position = oldPosition;
			reader = new BinaryReader(newStream);

			return reader;
		}

		private static bool NprMaterialSwap(string fileName, out string shaderName, out string shaderFile)
		{
			const string separator = "_NPRMAT_";
			var regex = new Regex(separator, RegexOptions.IgnoreCase);
			var shaderString = regex.Split(fileName).Last().Replace(".mate", string.Empty);

			shaderFile = "com3d2mod_" + shaderString;

			var materialFileName = shaderFile;
			var materialFile = _materials.FirstOrDefault(r => r.name.Equals(materialFileName, StringComparison.OrdinalIgnoreCase));

			if (materialFile == null)
			{
				shaderName = string.Empty;
				shaderFile = string.Empty;
				return false;
			}

			shaderName = materialFile.shader.name;
			return true;
		}

		public static bool HandleExtraTextureTypes(string propertyName, string textureType, ref BinaryReader reader, ref Material material)
		{
#if DEBUG
			PluginLogger.LogDebug($"Was called to work on: {propertyName} {textureType} Gonna see if this is a viable type we handle...");
#endif

			if (textureType.Equals("cube"))
			{
				var text5 = reader.ReadString();

				//Discarding. Currently useless.
				reader.ReadString();

#if DEBUG
				PluginLogger.LogDebug($"Loading texture {text5}");
#endif
				var tempText = ImportCM.CreateTexture(text5 + ".tex");
				tempText.name = text5;

				var cubeText = CubemapConverter.ByTexture2D(tempText);
				material.SetTexture(propertyName, cubeText);

				//Discarding, useless currently.
				reader.ReadSingle();
				reader.ReadSingle();
				reader.ReadSingle();
				reader.ReadSingle();

				return true;
			}

			return false;
		}

		public static bool DoFloatStuff(string propertyName, float value, ref TBodySkin bodySkin, ref Material material)
		{
#if DEBUG
			PluginLogger.LogDebug($"Now doing Float stuff... {propertyName} {value} {bodySkin.m_strModelFileName} {material.name}");
#endif

			if (bodySkin != null && (propertyName == "_StencilID" || propertyName == "_StencilID2") && (bodySkin.SlotId.ToString() == "head" || bodySkin.SlotId.ToString() == "hairF" || bodySkin.SlotId.ToString() == "hairS"))
			{
				var num5 = 0;
				if (propertyName == "_StencilID2")
				{
					num5 = 1;
				}

				//PluginLogger.LogInfo("Fetching GUID");

				var characterMgr = GameMain.Instance.CharacterMgr;
				var guid = bodySkin.body.maid.status.guid;

				//PluginLogger.LogInfo("Fetched GUID");

				if (!NprShader.SId.ContainsKey(guid) && NprShader.IsValid(bodySkin.body.maid) &&
					characterMgr.GetStockMaid(guid) && NprShader.SId.Count < 32)
				{
					for (var k = 0; k < characterMgr.GetStockMaidCount(); k++)
					{
						if (characterMgr.GetStockMaid(k) != null &&
							characterMgr.GetStockMaid(k).status.guid == guid)
						{
							NprShader.SId[guid] = 32 + NprShader.SId.Count * 2;
						}
					}
				}
				else if (!NprShader.SId.ContainsKey("def"))
				{
					NprShader.SId["def"] = 30 + NprShader.SId.Count * 2;
				}

				//PluginLogger.LogInfo("Did Stuff with GUID");

				if (NprShader.SId.ContainsKey(guid))
				{
					material.SetFloat(propertyName, NprShader.SId[guid] + num5);
				}
				else
				{
					material.SetFloat(propertyName, NprShader.SId["def"] + num5);
				}

				//PluginLogger.LogInfo("Done with Stencil GUID Stuff.");

				return false;
			}

			const string keywordTag = "_SSKEYWORD";

			if (propertyName.EndsWith(keywordTag, StringComparison.OrdinalIgnoreCase))
			{
				var targetPropName = propertyName.ToUpper().Replace(keywordTag, string.Empty);

#if DEBUG
				PluginLogger.LogDebug($"Noticed Keyword {targetPropName}");
#endif

				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (value == 1f)
				{
					material.EnableKeyword(targetPropName);
				}
				else
				{
					material.DisableKeyword(targetPropName);
				}

				return false;
			}

			return true;
		}

		private static TextureWrapMode ShouldWrap(string materialName)
		{
#if DEBUG
			PluginLogger.LogDebug($"Checking WrapMode target for {materialName}");
#endif

			return materialName.StartsWith("gp03_", StringComparison.OrdinalIgnoreCase) ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
		}

		private static IEnumerator SkmDeepSearch(Material material)
		{
			yield return null;

			var watch = Stopwatch.StartNew();

			//PluginLogger.LogWarning("Starting deep search for SkinnedMeshRenderer!");

			var attemptFrames = 0;
			while (++attemptFrames < 5)
			{
				//PluginLogger.LogWarning($"Deep search: #{attemptFrames}");

				var matchingSkinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>()
					.Where(r => r.sharedMaterials.Contains(material)).ToArray();

				if (!matchingSkinnedMeshRenderers.Any())
				{
					watch.Stop();
					yield return null;
					watch.Start();
					continue;
				}

				foreach (var skinnedMeshRenderer in matchingSkinnedMeshRenderers)
				{
					MeshUpdater.GetOrAddComponent(skinnedMeshRenderer);
				}

				break;
			}

			PluginLogger.LogInfo($"Deep search complete! Attempts: {attemptFrames} | Success: {attemptFrames < 5} | Time: {watch.Elapsed}");
		}

		private static IEnumerator UpdaterCallBack(TBodySkin objectToAdd, Material material)
		{
			var attemptFrames = 0;
			while (attemptFrames < 5 && objectToAdd != null)
			{
				if (objectToAdd?.obj?.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
				{
					attemptFrames++;
					yield return null;
					continue;
				}

				Hooks.AddMeshTracker(ref objectToAdd, ref material);
				yield break;
			}

			//PluginLogger.LogWarning("Couldn't find the SKM in the object... Resorting to a deep search...");
			yield return SkmDeepSearch(material);
		}

		private static class Hooks
		{
			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadMaterial))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> LoadMaterialInjector(IEnumerable<CodeInstruction> instructions)
			{
				var codeMatch = new CodeMatcher(instructions);

				codeMatch = codeMatch.MatchForward(false,
						new CodeMatch(OpCodes.Dup),
					new CodeMatch(OpCodes.Ldarg_1),
					new CodeMatch(OpCodes.Ldarg_2),
					new CodeMatch(OpCodes.Ldloc_2),
					new CodeMatch(OpCodes.Call)
					)
					.ThrowIfNotMatch("Couldn't match LoadMaterial's ReadMaterial block!")
					.Insert(
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call,
							typeof(ShaderServant).GetMethod(nameof(ForceNprCompatibility))));

				return codeMatch.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.ReadMaterial))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> LoadShaderMaterial2(IEnumerable<CodeInstruction> instructions,
				ILGenerator generator)
			{
				var codeMatch = new CodeMatcher(instructions);

				codeMatch = codeMatch.MatchForward(true,
					new CodeMatch(OpCodes.Ldloc_S),
					new CodeMatch(OpCodes.Ldnull),
					new CodeMatch(OpCodes.Call),
					new CodeMatch(OpCodes.Brfalse)
				)
				.ThrowIfNotMatch("Couldn't match ReadMaterial's branching!");

				var newLabel = codeMatch.Instruction.operand;
				codeMatch = codeMatch.Advance(1);

				codeMatch = codeMatch
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_2),
						new CodeInstruction(OpCodes.Ldloca_S, 4),
						new CodeInstruction(OpCodes.Call,
							typeof(ShaderServant).GetMethod(nameof(LoadExternalMaterial2))),
						new CodeInstruction(OpCodes.Brfalse_S, newLabel));

				return codeMatch.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.ReadMaterial))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> LoadShaderMaterial(IEnumerable<CodeInstruction> instructions,
				ILGenerator generator)
			{
				var codeMatch = new CodeMatcher(instructions);

				codeMatch = codeMatch.MatchForward(true,
					new CodeMatch(OpCodes.Ldstr),
					new CodeMatch(OpCodes.Ldloc_1),
					new CodeMatch(OpCodes.Call),
					new CodeMatch(OpCodes.Call),
					new CodeMatch(OpCodes.Br)
				)
				.ThrowIfNotMatch("Couldn't match 2nd ReadMaterial's branching!"); ;

				var newLabel = generator.DefineLabel();
				codeMatch.Labels.Add(newLabel);
				codeMatch.Advance(-4);

				codeMatch = codeMatch
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_1),
						new CodeInstruction(OpCodes.Ldloca_S, 3),
						new CodeInstruction(OpCodes.Call,
							typeof(ShaderServant).GetMethod(nameof(LoadExternalMaterial))),
						new CodeInstruction(OpCodes.Brfalse_S, newLabel));

				return codeMatch.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.ReadMaterial))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> ExtraTextureTypes(IEnumerable<CodeInstruction> instructions,
				ILGenerator generator)
			{
				var codeMatch = new CodeMatcher(instructions);

				//Find the block we want to inject
				codeMatch = codeMatch.MatchForward(false,
					new CodeMatch(OpCodes.Ldloc_S),
					new CodeMatch(OpCodes.Ldstr, "null"),
					new CodeMatch(OpCodes.Call),
					new CodeMatch(OpCodes.Brfalse)
				)
				.ThrowIfNotMatch("Couldn't match texture type loading!"); ; ;

				var newLabel = generator.DefineLabel();
				codeMatch.Labels.Add(newLabel);

				//Get the br destination
				var brMatch = codeMatch
					.MatchForward(false, new CodeMatch(OpCodes.Br)).Operand;

				//Go back and inject where we first started.
				codeMatch
					.Start()
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Ldstr, "null"),
						new CodeMatch(OpCodes.Call),
						new CodeMatch(OpCodes.Brfalse)
					);

				//Add branch
				codeMatch = codeMatch
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, 12),
						new CodeInstruction(OpCodes.Ldloc_S, 13),
						new CodeInstruction(OpCodes.Ldarga_S, 0),
						new CodeInstruction(OpCodes.Ldloca_S, 3),
						new CodeInstruction(OpCodes.Call,
							typeof(ShaderServant).GetMethod(nameof(HandleExtraTextureTypes))),
						new CodeInstruction(OpCodes.Brfalse_S, newLabel),
						new CodeInstruction(OpCodes.Br, brMatch));

				return codeMatch.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.ReadMaterial))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> SetAllTexturesToRepeat(
				IEnumerable<CodeInstruction> instructions)
			{

				try
				{
					var result = new CodeMatcher(instructions)
						.MatchForward(false,
							new CodeMatch(OpCodes.Ldloc_S),
							new CodeMatch(OpCodes.Ldc_I4_1),
							new CodeMatch(OpCodes.Callvirt,
								typeof(Texture).GetProperty(nameof(Texture.wrapMode))?.GetSetMethod())
						)
						.ThrowIfNotMatch("Could not match! Maybe WrapModeExtend was here?")
						.Advance(1)
						.RemoveInstruction()
						.Insert(new CodeInstruction(OpCodes.Ldloc_0), CodeInstruction.Call(typeof(ShaderServant), nameof(ShouldWrap)))
						.InstructionEnumeration();

					return result;
				}
				catch (InvalidOperationException)
				{
					var myType = AppDomain.CurrentDomain.GetAssemblies()
						.Select(r => r.GetTypes().FirstOrDefault(m => m.Name.Equals("WrapModeExtend")));

					if (myType != null)
					{
						PluginLogger.LogInfo(
							"Skipping WrapMode patch as it seems WrapModeExtend has taken care of it...");
						return instructions;
					}

					throw;
				}
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.ReadMaterial))]
			[HarmonyTranspiler]
			public static IEnumerable<CodeInstruction> FloatValueExtensions(IEnumerable<CodeInstruction> instructions,
				ILGenerator generator)
			{
				var codeMatch = new CodeMatcher(instructions);

				codeMatch = codeMatch.MatchForward(true,
					new CodeMatch(OpCodes.Ldloc_3),
					new CodeMatch(OpCodes.Ldloc_S),
					new CodeMatch(OpCodes.Ldloc_S),
					new CodeMatch(OpCodes.Callvirt,
						typeof(Material).GetMethod(nameof(Material.SetFloat), new[] { typeof(string), typeof(float) })),
					new CodeMatch(OpCodes.Br)
				)
				.ThrowIfNotMatch("Couldn't match float extensions type loading!"); ; ; ;

				var newLabel = generator.DefineLabel();
				codeMatch.Labels.Add(newLabel);
				codeMatch.Advance(-4);

				codeMatch = codeMatch
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, 12),
						new CodeInstruction(OpCodes.Ldloc_S, 20),
						new CodeInstruction(OpCodes.Ldarga_S, 1),
						new CodeInstruction(OpCodes.Ldloca_S, 3),
						new CodeInstruction(OpCodes.Call,
							typeof(ShaderServant).GetMethod(nameof(DoFloatStuff))),
						new CodeInstruction(OpCodes.Brfalse_S, newLabel));

				return codeMatch.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.ReadMaterial))]
			[HarmonyPostfix]
			public static void SetNprLightPoints(ref Material __result)
			{
				if (__result.HasProperty("_CUSTOMSPOTLIGHTDIR") == false &&
				    __result.HasProperty("_CUSTOMPOINTLIGHTDIR") == false)
				{
					return;
				}

				var enableLightDir = NprShader.IsDance ? 1.0f : 0.0f;

				__result.SetFloat("_CUSTOMSPOTLIGHTDIR", enableLightDir);
				__result.SetFloat("_CUSTOMPOINTLIGHTDIR", enableLightDir);
			}

			[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadMaterial))]
			[HarmonyPostfix]
			public static void AddMeshTracker(ref TBodySkin __1, ref Material __result)
			{
				//PluginLogger.LogInfo($"Done loading material {__result.name} with {__result.shader.name} @\n{Environment.StackTrace}");

				if (__1 == null)
				{
					//PluginLogger.LogWarning("TBodySkin was passed as null! Resorting to a deep search...");
					Instance.StartCoroutine(SkmDeepSearch(__result));

					return;
				}

				var renderer = __1.obj?.GetComponentInChildren<SkinnedMeshRenderer>(true);
				if (renderer == null)
				{
					//PluginLogger.LogWarning("Could not find an SKM in the processed material!! Will attempt adding the updater next frame...");
					Instance.StartCoroutine(UpdaterCallBack(__1, __result));
					return;
				}

				MeshUpdater.GetOrAddComponent(renderer);
			}

			//Hackish fix for Kiss changing the shader in these particular items and in those exact slots for some reason...
			[HarmonyPatch(typeof(TBodySkin), nameof(TBodySkin.ChangeShader), typeof(int), typeof(Shader))]
			[HarmonyPrefix]
			public static bool FilterShaderChangeRequests(ref TBodySkin __instance, ref int __0, ref Shader __1)
			{
				if ((__instance.Category.Equals("body") == false || __0 != 0) &&
				    (__instance.Category.Equals("head") == false || __0 != 5))
				{
					return true;
				}

				var gameObject = __instance.obj;
				if (gameObject == null)
				{
					return false;
				}

				foreach (var transform in gameObject.transform.GetComponentsInChildren<Transform>(true))
				{
					var component = transform.GetComponent<Renderer>();
					if (component == null || __0 >= component.sharedMaterials.Length)
					{
						continue;
					}

					var previousShader = component.sharedMaterials[__0].shader;

					if (_materials.Any(r => r.shader == previousShader))
					{
#if DEBUG
						PluginLogger.LogDebug($"Denied a shader change for {transform.gameObject.name}, our shaders may not be changed.");
#endif
						return false;
					}
				}

				return true;
			}
		}
	}
}