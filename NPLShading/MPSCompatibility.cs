using HarmonyLib;
using MeidoPhotoStudio.Plugin;
using UnityEngine;

namespace ShaderServant
{
	public class MpsCompatibility
	{
		[HarmonyPatch(typeof(ModelUtility), "LoadMenuModel", typeof(ModItem))]
		[HarmonyPostfix]
		public static void AddMeshTracker(ref GameObject __result)
		{
			var renderer = __result.GetComponentInChildren<SkinnedMeshRenderer>(true);
			MeshUpdater.GetOrAddComponent(renderer);
		}
	}
}