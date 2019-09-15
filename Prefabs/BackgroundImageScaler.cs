using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Prefabs {
	public sealed class BackgroundImageScaler : MonoBehaviour {
		public CanvasSizeWatcher sizeWatcher;
		public Image backgroundImage;

		[Space]
		public Sprite sprite;

		void OnEnable() {
			sizeWatcher.CanvasSizeChange += RescaleBackground;
		}

		void OnDisable() {
			sizeWatcher.CanvasSizeChange -= RescaleBackground;
		}

		void Start() {
			RescaleBackground(sizeWatcher.canvasSize, sizeWatcher.canvasAspect);
		}

		public void RescaleBackground(Vector2 canvasSize, float canvasAspect) {
			Vector2 backgroundSize;
			if (sprite) {
				var spriteSize = sprite.rect.size;
				float spriteAspect = spriteSize.x / spriteSize.y;
				if (canvasAspect < spriteAspect) {
					backgroundSize = new Vector2(canvasSize.y * spriteAspect, canvasSize.y);
				} else {
					backgroundSize = new Vector2(canvasSize.x, canvasSize.x / spriteAspect);
				}
			} else {
				backgroundSize = canvasSize;
			}
			backgroundImage.sprite = sprite;
			backgroundImage.rectTransform.sizeDelta = backgroundSize;
		}
	}
}