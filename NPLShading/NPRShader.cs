using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ShaderServant
{
	//Code from NPR shader is here. Mostly heavily modified and optimized.
	internal static class NprShader
	{
		public static bool IsDance;
		internal static Dictionary<string, int> SId;

		internal static bool IsValid(Maid m)
		{
			return m != null && m.body0 != null && m.Visible;
		}

		internal static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
		{
			IsDance = scene.name.Contains("SceneDance_");
			SId = new Dictionary<string, int>();
			RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
			RenderSettings.reflectionBounces = 1;
			//DynamicGI.UpdateEnvironment();
			//ReflectionProbeController.Sleep_Static();
		}
	}

	/*
	public class ReflectionProbeInstancer : MonoBehaviour
	{
		public ReflectionProbe Probe;
		private bool _renderNextUpdate;
		private int _renderId = -1;

		public static ReflectionProbeInstancer GetOrAdd(GameObject gameObject)
		{
			var reflectionProber = new GameObject("SSReflectionProbe")
			{
				hideFlags = HideFlags.DontSave & HideFlags.DontSaveInEditor & HideFlags.DontSaveInBuild & HideFlags.DontUnloadUnusedAsset
			};
			reflectionProber.transform.parent = gameObject.transform;
			return reflectionProber.GetOrAddComponent<ReflectionProbeInstancer>();
		}

		private void Awake()
		{
			Probe = gameObject.AddComponent<ReflectionProbe>();
			Probe.mode = ReflectionProbeMode.Realtime;
			Probe.resolution = 512;
			Probe.size = new Vector3(500f, 500f, 500f);
			Probe.backgroundColor = new Color32(0, 0, 0, 0);
			Probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
			Probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			Probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
			Probe.boxProjection = true;
			Probe.hdr = true;
			Probe.cullingMask = -1;
			Probe.transform.position = new Vector3(0f, 0f, 0f);
		}

		public void Update()
		{
			if (_renderNextUpdate == false)
			{
				return;
			}

			_renderNextUpdate = false;

			if (_renderId != -1 && Probe.IsFinishedRendering(_renderId) == false)
			{
				//ShaderServant.PluginLogger.LogInfo("Probe is rendering. Please hold...");
				return;
			}

			_renderId = Probe.RenderProbe();
		}
		public void OnWillRenderObject()
		{
			_renderNextUpdate = true;
		}
	}
	*/

	public class ReflectionProbeController : MonoBehaviour
	{
		public ReflectionProbe Probe;
		private static ReflectionProbeController _instance;
		private static bool _renderNextUpdate;
		private int _renderId = -1;

		public static ReflectionProbeController GetOrInitialize()
		{
			if (_instance != null)
			{
				return _instance;
			}

			var reflectionProber = new GameObject("SSReflectionProbe")
			{
				hideFlags = HideFlags.DontSave & HideFlags.DontSaveInEditor & HideFlags.DontSaveInBuild & HideFlags.DontUnloadUnusedAsset
			};
			DontDestroyOnLoad(reflectionProber);
			var probeController = reflectionProber.GetOrAddComponent<ReflectionProbeController>();

			_instance = probeController;
			return _instance;
		}

		private void Awake()
		{
			Probe = gameObject.AddComponent<ReflectionProbe>();
			Probe.mode = ReflectionProbeMode.Realtime;
			Probe.resolution = 512;
			Probe.size = new Vector3(500f, 500f, 500f);
			Probe.backgroundColor = new Color32(0, 0, 0, 0);
			Probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
			Probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
			Probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
			Probe.boxProjection = true;
			Probe.hdr = true;
			Probe.cullingMask = -1;
			Probe.transform.position = new Vector3(0f, 0f, 0f);

			Sleep();
		}

		public void Update()
		{
			if (_renderNextUpdate == false)
			{
				return;
			}

			_renderNextUpdate = false;

			if (_renderId != -1 && Probe.IsFinishedRendering(_renderId) == false)
			{
				//ShaderServant.PluginLogger.LogInfo("Probe is rendering. Please hold...");
				return;
			}

			Probe.transform.rotation = Quaternion.identity;

			if (GameMain.Instance.VRDummyMode)
			{
				Probe.transform.position = GameMain.Instance.OvrMgr.OvrCamera.GetRealHeadTransform().position;
				Probe.center = GameMain.Instance.OvrMgr.OvrCamera.GetRealHeadTransform().position;
			}
			else if (NprShader.IsDance)
			{
				Probe.transform.position = new Vector3(GameMain.Instance.MainCamera.GetPos().x, 0.95894f, GameMain.Instance.MainCamera.GetPos().z);
				Probe.center = new Vector3(GameMain.Instance.MainCamera.GetPos().x, 0.95894f, GameMain.Instance.MainCamera.GetPos().z);
			}
			else
			{
				Probe.transform.position = GameMain.Instance.MainCamera.GetPos();
				Probe.center = GameMain.Instance.MainCamera.GetPos();
			}

			//ShaderServant.PluginLogger.LogInfo("Probe completed a render!");

			_renderId = Probe.RenderProbe();
		}

		private void Wake()
		{
			if (isActiveAndEnabled)
			{
				return;
			}

			gameObject.SetActive(true);
		}

		public static void Wake_Static()
		{
			var instance = GetOrInitialize();
			instance.Wake();
		}

		private void Sleep()
		{
			gameObject.SetActive(false);
		}

		public static void Sleep_Static()
		{
			var instance = GetOrInitialize();
			instance.Sleep();
		}

		public static void DoRenderNextUpdate()
		{
			_renderNextUpdate = true;
		}
	}
}