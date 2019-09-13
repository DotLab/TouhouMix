using UnityEngine;

namespace TouhouMix.Prefabs {
	public sealed class CanvasSizeWatcher : MonoBehaviour {
		public event System.Action<Vector2, float> CanvasSizeChange;

		public RectTransform canvasRect;

		[Space]
		public Vector2 canvasSize;
		public float canvasAspect;

		public int resolutionX;
		public int resolutionY;
		public Vector2 resolution;

		void Update() {
			if (resolutionX != Screen.width || resolutionY != Screen.height) {
				Recalculate();
				if (CanvasSizeChange != null) CanvasSizeChange(canvasSize, canvasAspect);
			}
		}

		[ContextMenu("Recalculate")]
		public void Recalculate() {
			resolution.x = resolutionX = Screen.width;
			resolution.y = resolutionY = Screen.height;
			
			canvasSize = canvasRect.sizeDelta;
			canvasAspect = canvasSize.x / canvasSize.y;
		}
	}
}