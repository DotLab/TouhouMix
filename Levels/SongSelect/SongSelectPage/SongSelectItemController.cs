using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Levels.SongSelect {
	public sealed class SongSelectItemController : MonoBehaviour {
		public Text titleText;
		public Text line1Text;
		public Text line2Text;
		public RawImageCutter imageCutter;

		public System.Action action;

		public void Init(string titleText, string line1Text, string line2Text, System.Action action) {
			this.titleText.text = titleText;
			this.line1Text.text = line1Text;
			this.line2Text.text = line2Text;
			this.action = action;
		}

		public void OnClicked() {
			action();
		}
	}
}
