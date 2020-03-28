using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class RawImageCutter : MonoBehaviour {
	public RawImage image;
	public Texture2D texture;
	public Texture2D cuttedTexture;
	public float borderRadius;
	public bool useRuntimeSize;
	public Vector2 size;

	static readonly Color32 CLEAR = new Color32(0, 0, 0, 0);

	[Button]
	public void GetCurrentSize() {
		size = GetComponent<RectTransform>().rect.size;
	}

	[Button]
	public void Clear() {
		image.texture = null;
		DestroyTexture();
	}

	[Button]
	public void Recut() {
		Cut(texture);
	}

	[Button]
	public void Cut(Texture2D texture) {
		DestroyTexture();
		this.texture = texture;
		if (useRuntimeSize) {
			size = GetComponent<RectTransform>().rect.size;
		}
		//Debug.Log("Cut " + size + " for " + gameObject.name);
		if (size.x == 0 || size.y == 0) {
			Debug.LogWarning("Cutter exit, size 0");
			return;
		}
		float aspect = size.x / size.y;
		int pixelWidth = texture.width;
		int pixelHeight = (int)(texture.width / aspect);
		cuttedTexture = new Texture2D(pixelWidth, pixelHeight);
		cuttedTexture.wrapMode = TextureWrapMode.Mirror;
		cuttedTexture.SetPixels(
			texture.GetPixels(0, (texture.height - pixelHeight) >> 1, pixelWidth, pixelHeight));

		if (borderRadius != 0) {
			float pixelRadius = borderRadius * pixelWidth / size.x;
			float pixelRadius2 = pixelRadius * pixelRadius;
			int pixelRadiusI = (int)pixelRadius;
			for (int y = 0; y <= pixelRadiusI; y++) {
				for (int x = 0; x <= pixelRadiusI; x++) {
					float xx = pixelRadius - x;
					float yy = pixelRadius - y;
					if (xx * xx + yy * yy > pixelRadius2) {
						cuttedTexture.SetPixel(x, y, CLEAR);
						cuttedTexture.SetPixel(x, pixelHeight - y, CLEAR);
						cuttedTexture.SetPixel(pixelWidth - x, y, CLEAR);
						cuttedTexture.SetPixel(pixelWidth - x, pixelHeight - y, CLEAR);
					} else {
						break;
					}
				}
			}

			cuttedTexture.Apply();
			image.texture = cuttedTexture;
		}
	}

	static void SetAlpha(Texture2D text, int x, int y, float a) {
		text.SetPixel(x, y, SetAlpha(text.GetPixel(x, y), a));
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
