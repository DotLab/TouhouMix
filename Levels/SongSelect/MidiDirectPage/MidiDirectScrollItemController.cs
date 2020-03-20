using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Levels.SongSelect.MidiDirectPage {
	public sealed class MidiDirectScrollItemController : MonoBehaviour {
		public Texture2D defaultTexture;
		public RawImageCutter coverImageCutter;
		public Text nameText;
		public Text authorText;
		public Text uploaderText;
		public Text albumText;
		public Text songText;
		public Button downloadButton;
		public Text iconText;
		//public CanvasGroup group;
	}
}
