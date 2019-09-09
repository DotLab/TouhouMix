using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Levels.SongSelect.SynthConfigPage {
	public sealed class SequenceConfigItemController : MonoBehaviour {
		public Text leftText;
		public Text iconText;
		public Image iconFrameImage;
		public Image itemFrameImage;

		[Space]
		public Text button1Text;
		public Text button2Text;
		public Text button3Text;

		[Space]
		public Text muteButtonText;

		[Space]
		public RectTransform previewRect;
		public Text previewText;

		public System.Action button1Action;
		public System.Action muteButtonAction;
		public System.Action soloButtonAction;

		public bool isUsingInGame;

		public void OnButton1Clicked() {
			button1Action();
		}

		public void OnMuteButtonClicked() {
			muteButtonAction();
		}

		public void OnSoloButtonClicked() {
			soloButtonAction();
		}
	}
}