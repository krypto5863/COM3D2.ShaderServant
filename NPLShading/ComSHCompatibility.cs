using COM3D2.ComSh.Plugin;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ShaderServant
{
	public static class ComShCompatibility
	{
		public static void PatchMethod(Harmony harmony)
		{
			var targetMethod = typeof(ObjUtil).GetNestedType("MenuObj").GetMethod("SetMaterial", BindingFlags.NonPublic | BindingFlags.Static);
			var patchMethod = new HarmonyMethod(typeof(ComShCompatibility).GetMethod("AddMeshTracker"));

			harmony.Patch(targetMethod, null, patchMethod);
		}

		public static void AddMeshTracker(ref Transform __0)
		{
			var renderer = __0.GetComponentInChildren<SkinnedMeshRenderer>(true);
			MeshUpdater.GetOrAddComponent(renderer);
		}
	}
}