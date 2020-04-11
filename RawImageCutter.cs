using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace TouhouMix {
	public class RawImageCutter : MonoBehaviour {
		public RawImage image;
		public Texture2D texture;
		public Texture2D cuttedTexture;
		public float borderRadius;
		public bool useRuntimeSize;
		public Vector2 size;

		const float FEATHER = 1;
		static readonly Color32 CLEAR = new Color32(0, 0, 0, 0);
		//static readonly Color32 CLEAR = new Color32(0, 0, 255, 255);

		[Button]
		public void GetCurrentSize() {
			size = GetComponent<RectTransform>().rect.size;
		}

		[Button]
		public void Clear() {
			image.texture = null;
			DestroyTexture();
		}

		//[Button]
		//public void Recut() {
		//	Cut(texture);
		//}

		//[Button]
		//public void Cut(string _, Texture2D texture) {
		//	DestroyTexture();
		//	this.texture = texture;
		//	if (useRuntimeSize) {
		//		size = GetComponent<RectTransform>().rect.size;
		//	}
		//	Debug.Log("Cut " + size + " for " + gameObject.name);
		//	if (size.x == 0 || size.y == 0) {
		//		Debug.LogWarning("Cutter exit, size 0");
		//		return;
		//	}
		//	cuttedTexture = CutImage(texture, size, borderRadius);
		//	image.texture = cuttedTexture;
		//	image.color = Color.white;
		//}

		Net.WebCache.ILoadJob cutJob;

		[Button]
		public void Cut(string textureKey, Texture2D texture) {
			this.texture = texture;
			if (useRuntimeSize) {
				size = GetComponent<RectTransform>().rect.size;
			}
			//Debug.Log("Cut " + size + " for " + gameObject.name);
			if (size.x == 0 || size.y == 0) {
				Debug.LogWarning("Cutter exit, size 0");
				return;
			}

			cutJob?.Abort();
			cutJob = Net.WebCache.instance.CutTexture(textureKey, texture, size, borderRadius, job => {
				cuttedTexture = job.GetData();
				image.texture = cuttedTexture;
				image.color = Color.white;
				cutJob = null;
			});
		}

		private void OnDisable() {
			cutJob?.Abort();
			cutJob = null;
		}

		public static Texture2D CutImage(Texture2D texture, Vector2 size, float borderRadius) {
			float aspect = size.x / size.y;
			int pixelWidth;
			int pixelHeight;
			if (texture.width / (float)texture.height < aspect) {
				pixelWidth = texture.width;
				pixelHeight = Mathf.RoundToInt(texture.width / aspect);
			} else {
				pixelHeight = texture.height;
				pixelWidth = Mathf.RoundToInt(texture.height * aspect);
			}
			var cuttedTexture = new Texture2D(pixelWidth, pixelHeight);
			cuttedTexture.wrapMode = TextureWrapMode.Mirror;
			//cuttedTexture.filterMode = FilterMode.Point;
			cuttedTexture.SetPixels(
				texture.GetPixels((texture.width - pixelWidth) >> 1, (texture.height - pixelHeight) >> 1, pixelWidth, pixelHeight));

			float pixelRadius = borderRadius * pixelWidth / size.x;
			//float pixelRadius2 = pixelRadius * pixelRadius;
			int pixelRadiusI = (int)(pixelRadius) + 1;
			for (int y = 0; y <= pixelRadiusI; y++) {
				for (int x = 0; x <= pixelRadiusI; x++) {
					float xx = pixelRadius - (x + .5f);
					float yy = pixelRadius - (y + .5f);
					float distance = Mathf.Sqrt(xx * xx + yy * yy);
					if (distance > pixelRadius) {
						float diff = distance - pixelRadius;
						if (diff < FEATHER) {
							diff = FEATHER - diff;
							SetAlpha(cuttedTexture, x, y, diff);
							SetAlpha(cuttedTexture, x, pixelHeight - y - 1, diff);
							SetAlpha(cuttedTexture, pixelWidth - x - 1, y, diff);
							SetAlpha(cuttedTexture, pixelWidth - x - 1, pixelHeight - y - 1, diff);
						} else {
							cuttedTexture.SetPixel(x, y, CLEAR);
							cuttedTexture.SetPixel(x, pixelHeight - y - 1, CLEAR);
							cuttedTexture.SetPixel(pixelWidth - x - 1, y, CLEAR);
							cuttedTexture.SetPixel(pixelWidth - x - 1, pixelHeight - y - 1, CLEAR);
						}
					} else {
						break;
					}
				}
			}

			cuttedTexture.Apply();
			return cuttedTexture;
		}

		static void SetAlpha(Texture2D text, int x, int y, float a) {
			text.SetPixel(x, y, SetAlpha(text.GetPixel(x, y), a));
			//text.SetPixel(x, y, SetAlpha(Color.magenta, a));
		}

		static Color SetAlpha(Color c, float a) {
			c.a = a;
			return c;
		}

		void DestroyTexture() {
			if (cuttedTexture != null) {
#if UNITY_EDITOR
				DestroyImmediate(cuttedTexture);
#else
			Destroy(cuttedTexture);
#endif
			}
		}
	}
}
