using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uif.Settables.Components;

namespace TouhouMix.Levels.Gameplay {
	public sealed class BlockSkinController : MonoBehaviour, Systemf.IIndexable {
		public int index;
		[SerializeField]
		private RectTransform rect;
		public CanvasGroup group;
		public MultiGraphicColorSettable color;

		public UnityEngine.UI.RawImage rawImage;
		public float pixelsPerUnit;

		public int Index { 
			get { return index;  }
			set { index = value; }
		}

		public Vector2 Pos {
			get => rect.anchoredPosition;
			set => rect.anchoredPosition = value;
		}

		public Vector2 Size {
			get => rect.sizeDelta;
			set { 
				rect.sizeDelta = value;
				if (rawImage != null) {
					float offset = value.y / pixelsPerUnit / rawImage.texture.height;
					rawImage.uvRect = new Rect(0, 0, 1, offset);
				}
			}
		}

		public Vector3 Scale {
			get => rect.localScale;
			set => rect.localScale = value;
		}

		public Vector3 Rot {
			get => rect.eulerAngles;
			set => rect.eulerAngles = value;
		}

		private void Reset() {
			rect = GetComponent<RectTransform>();
			group = GetComponent<CanvasGroup>();
			color = GetComponent<MultiGraphicColorSettable>();
		}

		public void MoveToFront() {
			rect.SetAsLastSibling();
		}

		public void MoveToBack() {
			rect.SetAsFirstSibling();
		}
	}
}