using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using TouhouMix.Storage;

namespace TouhouMix.Levels.SongSelect {
	public class SongSelectPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public CanvasGroup scrollViewGroup;
		public RectTransform scrollViewRect;
		public RectTransform scrollViewContentRect;

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
			} else if (level.selectedSongId != null) {  // show midi scroll
				PopulateMidis();
				scrollViewPositionYSafe = level.midiSelectScrollViewPositionY;
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
						anim.New().EditTo(titleText, res.QueryAlbumById(albumId).name + " • " + res.QuerySongById(songId).name, .2f, 0);
						level.selectedSongId = songId;
						level.songSelectScrollViewPositionY = scrollViewPositionY;
						PopulateMidis();
						scrollViewPositionY = 0;
					} else if (albumId != null) {  // select album
						anim.New().EditTo(titleText, res.QueryAlbumById(albumId).name, .2f, 0);
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
				controller.Init(album.abbr, "", album.name, () => {
					OnScrollViewItemClicked(album._id, null, null);
				});
			});
		}

		void PopulateSongs() {
			PopulateScrollViewContent(res.QuerySongsByAlbumId(level.selectedAlbumId), (controller, data) => {
				var song = data as Storage.Protos.Api.SongProto;
				controller.Init(song.track.ToString("N0"), "", song.name, () => {
					OnScrollViewItemClicked(song.albumId, song._id, null);
				});
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
