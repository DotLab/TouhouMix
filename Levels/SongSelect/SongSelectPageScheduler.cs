using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using TouhouMix.Storage;
using System.Linq;
using TouhouMix.Net;

namespace TouhouMix.Levels.SongSelect {
	public class SongSelectPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public CanvasGroup scrollViewGroup;
		public RectTransform scrollViewRect;
		public RectTransform scrollViewContentRect;

		public Texture2D defaultTexture;

		[Space]
		public Text titleText;

		[Space]
		public GameObject scrollViewItemPrefab;

		ResourceStorage res;

		float scrollViewPositionY { 
			get { return scrollViewContentRect.anchoredPosition.y; } 
			set { scrollViewContentRect.anchoredPosition = new Vector2(0, value); } 
		}

		float scrollViewPositionYSafe { 
			set { 
				scrollViewContentRect.sizeDelta = new Vector2(0, 1000000);  // prevent ScrollView from buncing back
				scrollViewContentRect.anchoredPosition = new Vector2(0, value); 
			} 
		}

		public override void Init(SongSelectLevelScheduler levelScheduler) {
			base.Init(levelScheduler);

			titleText.text = "";

			res = game.resourceStorage;
		}

		public override void Enable() {
			base.Enable();
			Init();
		}

		public void Init() {
			if (!string.IsNullOrEmpty(level.selectedMidiId)) {  // show midi detail
				ShowMidiDetail();
			} else if (level.selectedSongId != null) {  // show midi detail with first midi selected
				level.selectedMidiId = res.QueryMidisBySongId(level.selectedSongId).First()._id;
				ShowMidiDetail();
				//scrollViewPositionYSafe = level.midiSelectScrollViewPositionY;
			} else if (level.selectedAlbumId != null) {  // show song scroll
				PopulateSongs();
				scrollViewPositionYSafe = level.songSelectScrollViewPositionY;
			} else {  // song album scroll
				PopulateAlbums();
				scrollViewPositionYSafe = level.albumSelectScrollViewPositionY;
			}
		}

		void OnScrollViewItemClicked(string albumId, string songId, string midi) {
			Debug.Log("OnScrollViewItemClicked " + midi);
			if (midi == null) {
				RefreshScrollView(-200, 400, () => {
					if (songId != null) {  // select song
						//anim.New().EditTo(titleText, res.QueryAlbumById(albumId).name + " • " + res.QuerySongById(songId).name, .2f, 0);
						level.selectedSongId = songId;
						level.selectedMidiId = res.QueryMidisBySongId(songId).First()._id;
						level.songSelectScrollViewPositionY = scrollViewPositionY;
						ShowMidiDetail();
						//PopulateMidis();
						//scrollViewPositionY = 0;
					} else if (albumId != null) {  // select album
						anim.New().EditTo(titleText, res.QueryAlbumById(albumId).name.TranslateArtifact(), .2f, 0);
						level.selectedAlbumId = albumId;
						level.albumSelectScrollViewPositionY = scrollViewPositionY;
						PopulateSongs();
						scrollViewPositionY = 0;
					}
				});
			} else {  // select midi
				level.selectedMidiId = midi;
				level.midiSelectScrollViewPositionY = scrollViewPositionY;
				ShowMidiDetail();
			}
		}

		public override void Back() {
			if (level.selectedAlbumId != null) {
				RefreshScrollView(200, -400, () => {
					if (level.selectedSongId != null) {  // back from midi select
						level.selectedSongId = null;
						PopulateSongs();
						scrollViewPositionYSafe = level.songSelectScrollViewPositionY;
					} else if (level.selectedAlbumId != null) {  // back from song select
						level.selectedAlbumId = null;
						PopulateAlbums();
						scrollViewPositionYSafe = level.albumSelectScrollViewPositionY;
					}
				});
			}
		}

		void PopulateAlbums() {
			PopulateScrollViewContent(res.QueryAllAlbums(), (controller, data) => {
				var album = data as Storage.Protos.Api.AlbumProto;

				controller.titleText.text = GetStringOrUnkonwn(album.name).TranslateArtifact();
				controller.line1Text.text = string.Format("{0:N0} songs", res.QuerySongsByAlbumId(album._id).Count());
				controller.line2Text.text = System.DateTime.Parse(album.date).ToLongDateString();
				controller.action = () => {
					OnScrollViewItemClicked(album._id, null, null);
				};

				var coverUrl = album.coverUrl;
				if (coverUrl != null) {
					Net.WebCache.instance.LoadTexture(coverUrl, job => {
						var texture = job.GetData();
						controller.imageCutter.Cut(texture);
					});
				} else {
					controller.imageCutter.Cut(defaultTexture);
				}
			});
		}

		public static string GetStringOrUnkonwn(string str) {
			return string.IsNullOrWhiteSpace(str) ? "Unknown" : str;
		}

		public static string FindFirstCoverUrl(IEnumerable<Storage.Protos.Api.MidiProto> midiList) {
			foreach (var midi in midiList) {
				if (!string.IsNullOrWhiteSpace(midi.coverUrl)) {
					return midi.coverUrl;
				}
			}
			return null;
		}

		void PopulateSongs() {
			PopulateScrollViewContent(res.QuerySongsByAlbumId(level.selectedAlbumId), (controller, data) => {
				var song = data as Storage.Protos.Api.SongProto;

				controller.titleText.text = string.Format("{0:N0}: {1}", song.track, song.name.TranslateArtifact());
				controller.line1Text.text = string.Format("{0:N0} midis", res.QueryMidisBySongId(song._id).Count());
				controller.line2Text.text = "";
				controller.action = () => {
					OnScrollViewItemClicked(song.albumId, song._id, null);
				};

				var coverUrl = res.QueryAlbumById(song.albumId).coverUrl;
				if (coverUrl != null) {
					Net.WebCache.instance.LoadTexture(coverUrl, job => {
						var texture = job.GetData();
						controller.imageCutter.Cut(texture);
					});
				} else {
					controller.imageCutter.Cut(defaultTexture);
				}
			});
		}

		void PopulateMidis() {
			PopulateScrollViewContent(res.QueryMidisBySongId(level.selectedSongId), (controller, data) => {
				var midi = data as Storage.Protos.Api.MidiProto;
				var song = res.QuerySongById(midi.songId);
				controller.Init(midi.artistName, "", midi.name, () => {
					OnScrollViewItemClicked(song.albumId, midi.songId, midi._id);
				});
			});
		}

		void ShowMidiDetail() {
			Debug.Log("ShowMidiDetail");
			level.Push(level.midiDetailPage);
		}

		void PopulateScrollViewContent(IEnumerable<object> dataSource, System.Action<SongSelectItemController, object> action) {
			int itemCount = scrollViewContentRect.childCount;
			int i = 0;
			foreach (var data in dataSource) {
				var item = i < itemCount ? scrollViewContentRect.GetChild(i++).gameObject : Instantiate(scrollViewItemPrefab, scrollViewContentRect);
				item.SetActive(true);
				action(item.GetComponent<SongSelectItemController>(), data);
			}
			for (; i < itemCount; i++) {
				scrollViewContentRect.GetChild(i).gameObject.SetActive(false);
			}
		}

		void RefreshScrollView(float hideShift, float showShift, System.Action action) {
			var pos = scrollViewRect.anchoredPosition;
			scrollViewGroup.interactable = false;
			level.DisableBackButton();
			AnimationManager.instance.New()
				.FadeOut(scrollViewGroup, .1f, 0)
				.ShiftTo(scrollViewRect, new Vector2(hideShift, 0), .1f, EsType.CubicIn).Then()
				.Call(() => {
					action(); 
					scrollViewRect.anchoredPosition = new Vector2(pos.x + showShift, pos.y);
				})
				.FadeIn(scrollViewGroup, .1f, 0)
				.MoveTo(scrollViewRect, pos, .1f, EsType.CubicOut).Then()
				.Call(() => {
					scrollViewGroup.interactable = true;
					level.EnableBackButton();
				});
		}
	}
}
