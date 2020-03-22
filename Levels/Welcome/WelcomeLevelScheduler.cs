using Uif;
using Uif.Settables;
using Uif.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TouhouMix.Levels.Welcome {
	public sealed class WelcomeLevelScheduler : MonoBehaviour {
		public Text versionText;
		public CanvasGroup[] splashGroups;

		void Start() {
			Application.targetFrameRate = Screen.currentResolution.refreshRate;
			Debug.Log("refresh rate: " + Screen.currentResolution.refreshRate);

			versionText.text = Application.version + " (Ctd 特供版)";

			var seq = AnimationManager.instance.New().Wait(.5f);
			for (int i = 0; i < splashGroups.Length; i++) {
				splashGroups[i].alpha = 0;
				seq.FadeIn(splashGroups[i], .5f, 0).Then().Wait(2)
					.FadeOut(splashGroups[i], .5f, 0);
			}

			seq.Then().Call(() => {
				Debug.Log("Loading SongSelect");
				UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.SONG_SELECT_LEVEL_BUILD_INDEX);
			});
		}
	}
}