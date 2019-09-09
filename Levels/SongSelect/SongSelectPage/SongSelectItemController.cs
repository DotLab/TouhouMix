using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Levels.SongSelect.SongSelectPage {
	public sealed class SongSelectItemController : MonoBehaviour {
		public Text tagText;
		public Text leftText;
		public Text rightText;

		public System.Action action;

		public void Init(string tagText, string leftText, string rightText, System.Action action) {
			this.tagText.text = tagText;
			this.leftText.text = leftText;
			this.rightText.text = rightText;
			this.action = action;
		}

		public void OnClicked() {
			action();
		}
	}
}
