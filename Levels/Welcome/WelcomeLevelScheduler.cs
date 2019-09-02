using Uif;
using Uif.Settables;
using Uif.Tasks;
using UnityEngine;

namespace TouhouMix.Levels.Welcome {
	public sealed class WelcomeLevelScheduler : MonoBehaviour {
		public CanvasGroup dotlabPageGroup;
		public CanvasGroup dmbnPageGroup;

		void Start() {
			AnimationManager.instance.New().Wait(.5f)
				.FadeIn(dotlabPageGroup, .5f, 0).Then().Wait(1)
				.FadeOut(dotlabPageGroup, .5f, 0)
				.FadeIn(dmbnPageGroup, .5f, 0).Then().Wait(1)
				.FadeOut(dmbnPageGroup, .5f, 0).Then().Call(() => {
					Debug.Log("Loading SongSelect");
					UnityEngine.SceneManagement.SceneManager.LoadScene("SongSelect");
				});
		}
	}
}