using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShaderServant
{
	internal class MeshUpdater : MonoBehaviour
	{
		public bool NeedsToRecalculate { get; private set; } = true;
		private SkinnedMeshRenderer _renderer;
		private bool _needsReflection;

		public static MeshUpdater GetOrAddComponent(SkinnedMeshRenderer renderer)
		{
			var updater = renderer.GetComponent<MeshUpdater>();

			if (updater != null)
			{
				updater.NeedsToRecalculate = true;
				updater.Awake();
				return updater;
			}

			updater = renderer.gameObject.AddComponent<MeshUpdater>();
			updater._renderer = renderer;
			return updater;
		}

		public void Awake()
		{
			ShaderServant.Queue.Enqueue(this);
		}

		public void LateUpdate()
		{
			if (NeedsToRecalculate == false)
			{
				return;
			}

			PostUpdateMesh();
		}

		public void OnWillRenderObject()
		{
			if (_needsReflection == false)
			{
				return;
			}

			ReflectionProbeController.DoRenderNextUpdate();
		}

		public void PostUpdateMesh()
		{
			NeedsToRecalculate = false;
			_renderer.sharedMesh.RecalculateTangents();

			foreach (var material in _renderer.sharedMaterials)
			{
				var shaderName = material.shader.name;

				ShaderServant.PluginLogger.LogInfo($"Processing: {shaderName}");

				if (shaderName.IndexOf("Reflection_", StringComparison.OrdinalIgnoreCase) == -1)
				{
					continue;
				}

				ShaderServant.PluginLogger.LogInfo($"{material.name} has called for reflection probes!");
				_needsReflection = true;
				ReflectionProbeController.Wake_Static();
				_renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
				//var prober = ReflectionProbeInstancer.GetOrAdd(_renderer.gameObject);
				//prober.transform.position = _renderer.sharedMesh.bounds.center;
				//var offset = prober.transform.position - prober.transform.TransformPoint(_renderer.sharedMesh.bounds.center);
				//prober.transform.position = offset;

				return;
			}

			_renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
			_needsReflection = false;
		}
	}
}