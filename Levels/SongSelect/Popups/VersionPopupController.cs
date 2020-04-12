using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Levels.SongSelect.Popups {
	public sealed class VersionPopupController : MonoBehaviour {
		public Text installedVersionText;
		public Text androidVersionText;
		public Text androidBetaVersionText;
		public Text androidAlphaVersionText;
		public Text iosVersionText;
		public Text iosBetaVersionText;

		public string androidUrl;
		public string androidBetaUrl;
		public string androidAlphaUrl;
		public string iosUrl;
		public string iosBetaUrl;

		public void OnAndroidVersionButtonClicked() {
			Application.OpenURL(androidUrl);
		}

		public void OnAndroidBetaVersionButtonClicked() {
			Application.OpenURL(androidBetaUrl);
		}

		public void OnAndroidAlphaVersionButtonClicked() {
			string url = androidAlphaUrl;
			if (Levels.GameScheduler.instance.appConfig.networkEndpoint == 1) {
				url = url
					.Replace("https://storage.thmix.org", "https://asia.storage.thmix.org")
					.Replace("https://storage.googleapis.com/microvolt-bucket-1", "https://asia.storage.thmix.org");
			}
			Application.OpenURL(url);
		}

		public void OnIosVersionButtonClicked() {
			Application.OpenURL(iosUrl);
		}

		public void OnIosBetaVersionButtonClicked() {
			Application.OpenURL(iosBetaUrl);
		}
	}
}