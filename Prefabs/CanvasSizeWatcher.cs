using UnityEngine;

namespace TouhouMix.Prefabs {
	public sealed class CanvasSizeWatcher : MonoBehaviour {
		public enum Match {
			Width,
			Height,
		}

		public event System.Action<Vector2, float> CanvasSizeChange;

		public RectTransform canvasRect;

		[Space]
		public Vector2 canvasReferenceSize;
		public Match sizeMatch;

		[Space]
		public Vector2 canvasSize;
		public float canvasAspect;

		public int resolutionX;
		public int resolutionY;
		public Vector2 resolution;

		void Awake() {
			Update();
		}

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
			canvasAspect = resolution.x / resolution.y;

			if (sizeMatch == Match.Width) {
				canvasSize.x = canvasReferenceSize.x;
				canvasSize.y = canvasReferenceSize.x / canvasAspect;
			} else {  // match == Match.Height
				canvasSize.y = canvasReferenceSize.y;
				canvasSize.x = canvasReferenceSize.y / canvasAspect;
			}
			Debug.Log("canvas size: " + canvasSize);
		}
	}
}