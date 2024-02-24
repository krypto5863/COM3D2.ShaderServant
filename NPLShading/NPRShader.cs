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
}