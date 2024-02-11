using System;
using UnityEngine;

namespace ShaderServant
{
	//Shamelessly (and lazily) taken from https://github.com/silver1145/scripts-com3d2/blob/main/scripts/npr_addition.cs
	public static class CubemapConverter
	{
		public static Cubemap ByTexture2D(Texture2D tempTex)
		{
			return Math.Abs((float)tempTex.width / tempTex.height - 2) < 0.05 ? ByLatLongTexture2D(tempTex) : ByCubeTexture2D(tempTex);
		}

		public static Cubemap ByCubeTexture2D(Texture2D tempTex)
		{
			tempTex = FlipPixels(tempTex, false, true);
			if (Math.Round((float)tempTex.width / tempTex.height) == 6)
			{
				var everyW = (int)(tempTex.width / 6f);
				var cubeMapSize = Mathf.Min(everyW, tempTex.height);
				var cubemap = new Cubemap(cubeMapSize, TextureFormat.RGBA32, false);
				cubemap.SetPixels(tempTex.GetPixels(0, 0, cubeMapSize, cubeMapSize), CubemapFace.PositiveX);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.NegativeX);
				cubemap.SetPixels(tempTex.GetPixels(2 * cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.PositiveY);
				cubemap.SetPixels(tempTex.GetPixels(3 * cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.NegativeY);
				cubemap.SetPixels(tempTex.GetPixels(4 * cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.PositiveZ);
				cubemap.SetPixels(tempTex.GetPixels(5 * cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.NegativeZ);
				cubemap.Apply();
				return cubemap;
			}
			else if (Math.Round((float)tempTex.height / tempTex.width) == 6)
			{
				var everyH = (int)(tempTex.height / 6f);
				var cubeMapSize = Mathf.Min(tempTex.width, everyH);
				var cubemap = new Cubemap(cubeMapSize, TextureFormat.RGBA32, false);
				cubemap.SetPixels(tempTex.GetPixels(0, 0, cubeMapSize, cubeMapSize), CubemapFace.PositiveX);
				cubemap.SetPixels(tempTex.GetPixels(0, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeX);
				cubemap.SetPixels(tempTex.GetPixels(0, 2 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.PositiveY);
				cubemap.SetPixels(tempTex.GetPixels(0, 3 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeY);
				cubemap.SetPixels(tempTex.GetPixels(0, 4 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.PositiveZ);
				cubemap.SetPixels(tempTex.GetPixels(0, 5 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeZ);
				cubemap.Apply();
				return cubemap;
			}
			else if (Math.Abs((float)tempTex.width / tempTex.height - 4.0 / 3.0) < 0.05)
			{
				var everyW = (int)(tempTex.width / 4f);
				var everyH = (int)(tempTex.height / 3f);
				var cubeMapSize = Mathf.Min(everyW, everyH);
				var cubemap = new Cubemap(cubeMapSize, TextureFormat.RGBA32, false);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.PositiveY);
				cubemap.SetPixels(tempTex.GetPixels(0, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeX);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.PositiveZ);
				cubemap.SetPixels(tempTex.GetPixels(2 * cubeMapSize, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.PositiveX);
				cubemap.SetPixels(tempTex.GetPixels(3 * cubeMapSize, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeZ);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, 2 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeY);
				cubemap.Apply();
				return cubemap;
			}
			else if (Math.Abs((float)tempTex.height / tempTex.width - 4.0 / 3.0) < 0.05)
			{
				var everyW = (int)(tempTex.width / 3f);
				var everyH = (int)(tempTex.height / 4f);
				var cubeMapSize = Mathf.Min(everyW, everyH);
				var cubemap = new Cubemap(cubeMapSize, TextureFormat.RGBA32, false);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, 0, cubeMapSize, cubeMapSize), CubemapFace.PositiveY);
				cubemap.SetPixels(tempTex.GetPixels(0, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeX);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.PositiveZ);
				cubemap.SetPixels(tempTex.GetPixels(2 * cubeMapSize, cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.PositiveX);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, 2 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeY);
				cubemap.SetPixels(tempTex.GetPixels(cubeMapSize, 3 * cubeMapSize, cubeMapSize, cubeMapSize), CubemapFace.NegativeZ);
				cubemap.Apply();
				return cubemap;
			}
			ShaderServant.PluginLogger.LogWarning($"Cannot be converted to Cubemap: {tempTex} ({tempTex.width}x{tempTex.height})");
			return null;
		}

		// from https://assetstore.unity.com/packages/tools/utilities/panorama-to-cubemap-13616
		public static Cubemap ByLatLongTexture2D(Texture2D tempTex)
		{
			var everyW = (int)(tempTex.width / 4f);
			var everyH = (int)(tempTex.height / 3f);
			var cubeMapSize = Mathf.Min(everyW, everyH);
			var cubemap = new Cubemap(cubeMapSize, TextureFormat.RGBA32, false);
			cubemap.SetPixels(CreateCubemapTexture(tempTex, cubeMapSize, 0).GetPixels(), CubemapFace.NegativeX);
			cubemap.SetPixels(CreateCubemapTexture(tempTex, cubeMapSize, 1).GetPixels(), CubemapFace.PositiveX);
			cubemap.SetPixels(CreateCubemapTexture(tempTex, cubeMapSize, 2).GetPixels(), CubemapFace.PositiveZ);
			cubemap.SetPixels(CreateCubemapTexture(tempTex, cubeMapSize, 3).GetPixels(), CubemapFace.NegativeZ);
			cubemap.SetPixels(CreateCubemapTexture(tempTex, cubeMapSize, 4).GetPixels(), CubemapFace.PositiveY);
			cubemap.SetPixels(CreateCubemapTexture(tempTex, cubeMapSize, 5).GetPixels(), CubemapFace.NegativeY);
			cubemap.Apply();
			return cubemap;
		}

		static Texture2D CreateCubemapTexture(Texture2D m_srcTexture, int texSize, int faceIndex)
		{
			var tex = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);

			var vDirA = new Vector3[4];
			if (faceIndex == 0)
			{
				vDirA[0] = new Vector3(-1.0f, 1.0f, -1.0f);
				vDirA[1] = new Vector3(1.0f, 1.0f, -1.0f);
				vDirA[2] = new Vector3(-1.0f, -1.0f, -1.0f);
				vDirA[3] = new Vector3(1.0f, -1.0f, -1.0f);
			}
			if (faceIndex == 1)
			{
				vDirA[0] = new Vector3(1.0f, 1.0f, 1.0f);
				vDirA[1] = new Vector3(-1.0f, 1.0f, 1.0f);
				vDirA[2] = new Vector3(1.0f, -1.0f, 1.0f);
				vDirA[3] = new Vector3(-1.0f, -1.0f, 1.0f);
			}
			if (faceIndex == 2)
			{
				vDirA[0] = new Vector3(1.0f, 1.0f, -1.0f);
				vDirA[1] = new Vector3(1.0f, 1.0f, 1.0f);
				vDirA[2] = new Vector3(1.0f, -1.0f, -1.0f);
				vDirA[3] = new Vector3(1.0f, -1.0f, 1.0f);
			}
			if (faceIndex == 3)
			{
				vDirA[0] = new Vector3(-1.0f, 1.0f, 1.0f);
				vDirA[1] = new Vector3(-1.0f, 1.0f, -1.0f);
				vDirA[2] = new Vector3(-1.0f, -1.0f, 1.0f);
				vDirA[3] = new Vector3(-1.0f, -1.0f, -1.0f);
			}
			if (faceIndex == 4)
			{
				vDirA[0] = new Vector3(-1.0f, 1.0f, -1.0f);
				vDirA[1] = new Vector3(-1.0f, 1.0f, 1.0f);
				vDirA[2] = new Vector3(1.0f, 1.0f, -1.0f);
				vDirA[3] = new Vector3(1.0f, 1.0f, 1.0f);
			}
			if (faceIndex == 5)
			{
				vDirA[0] = new Vector3(1.0f, -1.0f, -1.0f);
				vDirA[1] = new Vector3(1.0f, -1.0f, 1.0f);
				vDirA[2] = new Vector3(-1.0f, -1.0f, -1.0f);
				vDirA[3] = new Vector3(-1.0f, -1.0f, 1.0f);
			}
			var rotDX1 = (vDirA[1] - vDirA[0]) / texSize;
			var rotDX2 = (vDirA[3] - vDirA[2]) / texSize;
			var dy = 1.0f / texSize;
			var fy = 0.0f;
			var cols = new Color[texSize];
			for (var y = 0; y < texSize; y++)
			{
				var xv1 = vDirA[0];
				var xv2 = vDirA[2];
				for (var x = 0; x < texSize; x++)
				{
					var v = ((xv2 - xv1) * fy) + xv1;
					v.Normalize();
					cols[x] = CalcProjectionSpherical(m_srcTexture, v);
					xv1 += rotDX1;
					xv2 += rotDX2;
				}
				tex.SetPixels(0, y, texSize, 1, cols);
				fy += dy;
			}
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.Apply();
			return tex;
		}

		static Color CalcProjectionSpherical(Texture2D m_srcTexture, Vector3 vDir)
		{
			var theta = Mathf.Atan2(vDir.z, vDir.x);  // -π ～ +π (vertical rotation)
			var phi = Mathf.Acos(vDir.y);             //  0  ～ +π (horizontal rotation)

			while (theta < -Mathf.PI) theta += Mathf.PI + Mathf.PI;
			while (theta > Mathf.PI) theta -= Mathf.PI + Mathf.PI;

			var dx = theta / Mathf.PI;        // -1.0 ～ +1.0.
			var dy = phi / Mathf.PI;          //  0.0 ～ +1.0.

			dx = dx * 0.5f + 0.5f;
			var px = (int)(dx * m_srcTexture.width);
			if (px < 0)
			{
				px = 0;
			}
			if (px >= m_srcTexture.width)
			{
				px = m_srcTexture.width - 1;
			}
			var py = (int)(dy * m_srcTexture.height);
			if (py < 0)
			{
				py = 0;
			}
			if (py >= m_srcTexture.height)
			{
				py = m_srcTexture.height - 1;
			}
			var col = m_srcTexture.GetPixel(px, m_srcTexture.height - py - 1);
			return col;
		}

		static Texture2D FlipPixels(Texture2D texture, bool flipX, bool flipY)
		{
			if (!flipX && !flipY)
			{
				return texture;
			}
			if (flipX)
			{
				for (var i = 0; i < texture.width / 2; i++)
				{
					for (var j = 0; j < texture.height; j++)
					{
						var tempC = texture.GetPixel(i, j);
						texture.SetPixel(i, j, texture.GetPixel(texture.width - 1 - i, j));
						texture.SetPixel(texture.width - 1 - i, j, tempC);
					}
				}
			}
			if (flipY)
			{
				for (var i = 0; i < texture.width; i++)
				{
					for (var j = 0; j < texture.height / 2; j++)
					{
						var tempC = texture.GetPixel(i, j);
						texture.SetPixel(i, j, texture.GetPixel(i, texture.height - 1 - j));
						texture.SetPixel(i, texture.height - 1 - j, tempC);
					}
				}
			}
			texture.Apply();
			return texture;
		}
	}
}
