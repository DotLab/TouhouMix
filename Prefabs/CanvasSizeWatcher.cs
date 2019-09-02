using UnityEngine;

namespace TouhouMix.Prefabs {
	public sealed class CanvasSizeWatcher : MonoBehaviour {
		public event System.Action<Vector2, float> CanvasSizeChange;

		public RectTransform canvasRect;

		[Space]
		public Vector2 canvasSize;
		public float canvasAspect;

		int resolutionX_;
		int resolutionY_;

		void Update() {
			if (resolutionX_ != Screen.width || resolutionY_ != Screen.height) {
				Recalculate();
				if (CanvasSizeChange != null) CanvasSizeChange(canvasSize, canvasAspect);
			}
		}

		[ContextMenu("Recalculate")]
		public void Recalculate() {
			resolutionX_ = Screen.width;
			resolutionY_ = Screen.height;
			canvasSize = canvasRect.sizeDelta;
			canvasAspect = canvasSize.x / canvasSize.y;
		}
	}
}