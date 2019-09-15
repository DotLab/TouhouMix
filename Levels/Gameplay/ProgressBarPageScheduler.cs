using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;

namespace TouhouMix.Levels.Gameplay {
	public sealed class ProgressBarPageScheduler : MonoBehaviour {
		public TouhouMix.Prefabs.CanvasSizeWatcher sizeWatcher;
		public RectTransform progressBarRect;
		public RawImage progressBarImage;
		public Image progressBarLightImage;
		Texture2D progressBarTexture;
		float canvasWidth;
		int textureWidth;
		Color stroke;

		public void Start() {
			canvasWidth = sizeWatcher.canvasSize.x;
			textureWidth = 100;
			progressBarTexture = new Texture2D(textureWidth, 1, TextureFormat.RGB24, false);
			progressBarTexture.filterMode = FilterMode.Point;
			progressBarImage.texture = progressBarTexture;

			SetStrock(Color.black);
			SetProgress(0);
		}

		public void SetStrock(Color color) {
			stroke = color;
//			progressBarLightImage.color = color;
			AnimationManager.instance.New(progressBarLightImage)
				.FadeTo(progressBarLightImage, color, .5f, 0);
		}

		public void SetProgress(float t) {
			progressBarRect.sizeDelta = new Vector2(canvasWidth * t, 2);
			progressBarImage.uvRect = new Rect(Vector2.zero, new Vector2(t, 1));
			
			progressBarTexture.SetPixel((int)(t * textureWidth), 0, stroke);
			progressBarTexture.Apply();
		}
	}
}

