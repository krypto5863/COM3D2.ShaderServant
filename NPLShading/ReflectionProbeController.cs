using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderServant
{
	public class ReflectionProbeController : MonoBehaviour
	{
		public ReflectionProbe Probe { get; private set; }
		public static ReflectionProbeController _instance { get; private set; }
		//private static bool _renderNextUpdate;
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
#if DEBUG
			ShaderServant.PluginLogger.LogInfo("Probe is building...");
#endif

			Probe = gameObject.AddComponent<ReflectionProbe>();
			Probe.mode = ReflectionProbeMode.Realtime;
			//Probe.resolution = 512;
			//Probe.size = new Vector3(512,512,512);
			Probe.resolution = ShaderServant.ReflectionResolution.Value;
			Probe.size = new Vector3(ShaderServant.ReflectionRange.Value, ShaderServant.ReflectionRange.Value, ShaderServant.ReflectionRange.Value);
			Probe.backgroundColor = new Color32(0, 0, 0, 0);
			Probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
			Probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
			Probe.timeSlicingMode = (ReflectionProbeTimeSlicingMode)Enum.Parse(typeof(ReflectionProbeTimeSlicingMode), ShaderServant.ReflectionTimeSlicingMode.Value);
			Probe.boxProjection = true;
			Probe.hdr = true;
			Probe.cullingMask = -1;
			Probe.transform.position = new Vector3(0f, 0f, 0f);

			Sleep();
#if DEBUG
			ShaderServant.PluginLogger.LogInfo("Probe away!");
#endif
		}

		public void Update()
		{
			/*
			if (_renderNextUpdate == false)
			{
				return;
			}

			_renderNextUpdate = false;
			*/

			if (_renderId != -1 && Probe.IsFinishedRendering(_renderId) == false)
			{
#if DEBUG
				ShaderServant.PluginLogger.LogInfo("Probe is rendering. Please hold...");
#endif
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

#if DEBUG
			ShaderServant.PluginLogger.LogInfo("Probe completed a render!");
#endif

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

		/*
		public static void DoRenderNextUpdate()
		{
			_renderNextUpdate = true;
		}
		*/
	}
}