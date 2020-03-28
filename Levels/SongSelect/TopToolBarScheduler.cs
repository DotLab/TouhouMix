using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using TouhouMix.Levels.SongSelect.Popups;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using Jsonf;

namespace TouhouMix.Levels.SongSelect {
	public sealed class TopToolBarScheduler : MonoBehaviour {
		public Text rttText;

		public CanvasGroup versionPopupGroup;
		public VersionPopupController versionPopup;
		public CanvasGroup userInfoPopupGroup;
		public CanvasGroup socialPopupGroup;

		GameScheduler game;
		AnimationManager anim;

		public void Start() {
			game = GameScheduler.instance;
			anim = AnimationManager.instance;

			versionPopupGroup.HideAndDeactivate();
			userInfoPopupGroup.HideAndDeactivate();
			socialPopupGroup.HideAndDeactivate();

			game.netManager.onNetStatusChangedEvent += OnNetStatusChanged;
		}

		private void OnDestroy() {
			game.netManager.onNetStatusChangedEvent -= OnNetStatusChanged;
		}

		public void OnVersionButtonClicked() {
			if (versionPopupGroup.IsVisible()) {
				anim.New(versionPopupGroup)
					.FadeOut(versionPopupGroup, .2f, 0).Then().Deactivate(versionPopupGroup.gameObject);
				return;
			}
			anim.New(versionPopupGroup).Activate(versionPopupGroup.gameObject)
					.ScaleFromTo(versionPopupGroup.transform, new Vector3(1, 0, 1), Vector3.one, .2f, EsType.BackOut)
					.FadeIn(versionPopupGroup, .2f, 0);

			versionPopup.installedVersionText.text = Application.version;

			game.netManager.ClAppCheckVersion((err, data) => {
				if (!string.IsNullOrEmpty(err)) {
					Debug.LogError(err);
					return;
				}

				var obj = (JsonObj)data;

				game.ExecuteOnMain(() => {
					versionPopup.androidVersionText.text = obj.Get<string>("androidVersion");
					versionPopup.androidBetaVersionText.text = obj.Get<string>("androidBetaVersion");
					versionPopup.androidAlphaVersionText.text = obj.Get<string>("androidAlphaVersion");
					versionPopup.iosVersionText.text = obj.Get<string>("iosVersion");
					versionPopup.iosBetaVersionText.text = obj.Get<string>("iosBetaVersion");

#if UNITY_ANDROID
					if (GetBuild(obj.Get<string>("androidVersion")) > GetBuild(Application.version)) {
						NewBobingAnim(versionPopup.androidVersionText.transform);
					}
					if (GetBuild(obj.Get<string>("androidBetaVersion")) > GetBuild(Application.version)) {
						NewBobingAnim(versionPopup.androidBetaVersionText.transform);
					}
					if (GetBuild(obj.Get<string>("androidAlphaVersion")) > GetBuild(Application.version)) {
						NewBobingAnim(versionPopup.androidAlphaVersionText.transform);
					}
#elif UNITY_IOS
					if (GetBuild(obj.Get<string>("iosVersion")) > GetBuild(Application.version)) {
						NewBobingAnim(versionPopup.iosVersionText.transform);
					}
					if (GetBuild(obj.Get<string>("iosBetaVersion")) > GetBuild(Application.version)) {
						NewBobingAnim(versionPopup.iosBetaVersionText.transform);
					}
#endif

					versionPopup.androidUrl = obj.Get<string>("androidUrl");
					versionPopup.androidBetaUrl = obj.Get<string>("androidBetaUrl");
					versionPopup.androidAlphaUrl = obj.Get<string>("androidAlphaUrl");
					versionPopup.iosUrl = obj.Get<string>("iosUrl");
					versionPopup.iosBetaUrl = obj.Get<string>("iosBetaUrl");
				});
			});
		}

		void NewBobingAnim(Transform trans) {
			anim.New(trans)
			.ScaleFromTo(trans, Vector3.one, Vector3.one * 1.5f, .5f, EsType.CubicOut).Then()
			.ScaleTo(trans, Vector3.one, 1f, EsType.CubicOut)
			.Repeat();
		}

		static int GetBuild(string version) {
			string[] segs = version.Split('.');
			return int.Parse(segs[segs.Length - 1]);
		}

		public void OnUserInfoButtonClicked() {
			if (userInfoPopupGroup.IsVisible()) {
				anim.New(userInfoPopupGroup)
					.FadeOut(userInfoPopupGroup, .2f, 0).Then().Deactivate(userInfoPopupGroup.gameObject);
				return;
			}
			anim.New(userInfoPopupGroup).Activate(userInfoPopupGroup.gameObject)
					.ScaleFromTo(userInfoPopupGroup.transform, new Vector3(1, 0, 1), Vector3.one, .2f, EsType.BackOut)
					.FadeIn(userInfoPopupGroup, .2f, 0);
		}

		public void OnSocialButtonClicked() {
			if (socialPopupGroup.IsVisible()) {
				anim.New(socialPopupGroup)
					.FadeOut(socialPopupGroup, .2f, 0).Then().Deactivate(socialPopupGroup.gameObject);
				return;
			}
			anim.New(socialPopupGroup).Activate(socialPopupGroup.gameObject)
					.ScaleFromTo(socialPopupGroup.transform, new Vector3(1, 0, 1), Vector3.one, .2f, EsType.BackOut)
					.FadeIn(socialPopupGroup, .2f, 0);
		}

		public void OnGoToWebsiteButtonClicked() {
			Application.OpenURL("https://asia.thmix.org");
		}

		public void OnJoinQqButtonClicked() {
			Application.OpenURL("https://jq.qq.com/?_wv=1027&k=5udUHRp");
		}

		public void OnJoinDiscordButtonClicked() {
			Application.OpenURL("https://discord.gg/m2BeMbj");
		}

		public void OnNetStatusChanged(Net.NetManager.NetStatus status) {
			game.ExecuteOnMain(() => { 
				Debug.Log(status);

				switch (status) {
					case Net.NetManager.NetStatus.ONLINE:
						if (game.netManager.rtt == 0) {
							rttText.Translate("Connected");
						} else {
							rttText.text = string.Format("{0:N0}ms", game.netManager.rtt);
						}
						break;
					case Net.NetManager.NetStatus.OFFLINE:
						rttText.Translate("Offline");
						break;
					case Net.NetManager.NetStatus.CONNECTING:
						rttText.Translate("Connecting");
						break;
				}
			});
		}
	}
}
