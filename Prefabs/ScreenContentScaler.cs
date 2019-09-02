using UnityEngine;

namespace TouhouMix.Prefabs {
	public sealed class ScreenContentScaler : MonoBehaviour {
		public event System.Action ScreenSizeChange;

		public RectTransform canvasRect;
		public RectTransform paddingUpRect;
		public RectTransform paddingDownRect;
		public RectTransform contentRect;
		
		[Space]
		public float targetAspectWidth = 16;
		public float targetAspectHeight = 9;
		public float minPaddingHeight = 50;

		[Space]
		public Vector2 screenSize;
		public float screenAspect;
		public float targetAspect;
		public Vector2 contentSize;
		public float paddingHeight;

		int resolutionX_;
		int resolutionY_;

		void Update() {
			if (resolutionX_ != Screen.width || resolutionY_ != Screen.height) {
				Recalculate();
				if (ScreenSizeChange != null) ScreenSizeChange();
			}
		}

		[ContextMenu("Recalculate")]
		public void Recalculate() {
			resolutionX_ = Screen.width;
			resolutionY_ = Screen.height;
			screenSize = canvasRect.sizeDelta;
			screenAspect = screenSize.x / screenSize.y;
			targetAspect = targetAspectWidth / targetAspectHeight;

			paddingHeight = (screenSize.y - screenSize.x / targetAspect) * .5f;
			if (paddingHeight < minPaddingHeight) paddingHeight = 0;

			contentSize = screenSize;
			if (screenAspect < targetAspect) contentSize.y = contentSize.x / targetAspect;

			contentRect.sizeDelta = new Vector2(0, contentSize.y);
			paddingUpRect.sizeDelta = new Vector2(0, paddingHeight);
			paddingDownRect.sizeDelta = new Vector2(0, paddingHeight);
		}
	}
}