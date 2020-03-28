using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uif.Settables.Components;

namespace TouhouMix.Levels.Gameplay {
	public sealed class BlockSkinController : MonoBehaviour, Systemf.IIndexable {
		public int index;
		public RectTransform rect;
		public CanvasGroup group;
		public MultiGraphicColorSettable color;

		public int Index { 
			get { return index;  }
			set { index = value; }
		}

		private void Reset() {
			rect = GetComponent<RectTransform>();
			group = GetComponent<CanvasGroup>();
			color = GetComponent<MultiGraphicColorSettable>();
		}
	}
}