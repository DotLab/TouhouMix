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
			if (!string.IsNullOrEmpty(level.selectedMidi)) {  // show midi detail
				ShowMidiDetail();
			} else if (level.selectedSong != -1) {  // show midi scroll
				PopulateMidis();
				scrollViewPositionYSafe = level.midiSelectScrollViewPositionY;
			} else if (level.selectedAlbum != -1) {  // show song scroll
				PopulateSongs();
				scrollViewPositionYSafe = level.songSelectScrollViewPositionY;
			} else {  // song album scroll
				PopulateAlbums();
				scrollViewPositionYSafe = level.albumSelectScrollViewPositionY;
			}
		}

		void OnScrollViewItemClicked(int album, int song, string midi) {
			Debug.Log("OnScrollViewItemClicked " + midi);
			if (midi == null) {
				RefreshScrollView(-200, 400, () => {
					if (song != -1) {  // select song
						anim.New().EditTo(titleText, res.albumProtoDict[album].name + " • " + res.songProtoDict[Systemf.Tuple.Create(album, song)].name, .2f, 0);
						level.selectedSong = song;
						level.songSelectScrollViewPositionY = scrollViewPositionY;
						PopulateMidis();
						scrollViewPositionY = 0;
					} else if (album != -1) {  // select album
						anim.New().EditTo(titleText, res.albumProtoDict[album].name, .2f, 0);
						level.selectedAlbum = album;
						level.albumSelectScrollViewPositionY = scrollViewPositionY;
						PopulateSongs();
						scrollViewPositionY = 0;
					}
				});
			} else {  // select midi
				level.selectedMidi = midi;
				level.midiSelectScrollViewPositionY = scrollViewPositionY;
				ShowMidiDetail();
			}
		}

		public override void Back() {
			if (level.selectedAlbum != -1) {
				RefreshScrollView(200, -400, () => {
					if (level.selectedSong != -1) {  // back from midi select
						level.selectedSong = -1;
						PopulateSongs();
						scrollViewPositionYSafe = level.songSelectScrollViewPositionY;
					} else if (level.selectedAlbum != -1) {  // back from song select
						level.selectedAlbum = -1;
						PopulateAlbums();
						scrollViewPositionYSafe = level.albumSelectScrollViewPositionY;
					}
				});
			}
		}

		void PopulateAlbums() {
			PopulateScrollViewContent(res.QueryAlbums(), (controller, data) => {
				var album = data as TouhouMix.Storage.Protos.Resource.AlbumProto;
				controller.Init(album.tag, album.songCount.ToString("N0"), album.name, () => {
					OnScrollViewItemClicked(album.album, -1, null);
				});
			});
		}

		void PopulateSongs() {
			PopulateScrollViewContent(res.QuerySongsByAlbum(level.selectedAlbum), (controller, data) => {
				var song = data as TouhouMix.Storage.Protos.Resource.SongProto;
				controller.Init(song.song.ToString("N0"), song.midiCount.ToString("N0"), song.name, () => {
					OnScrollViewItemClicked(song.album, song.song, null);
				});
			});
		}

		void PopulateMidis() {
			PopulateScrollViewContent(res.QueryMidisByAlbumAndSong(level.selectedAlbum, level.selectedSong), (controller, data) => {
				var midi = data as TouhouMix.Storage.Protos.Resource.MidiProto;
				var author = res.authorProtoDict[midi.author];
				controller.Init(author.tag, "", midi.name, () => {
					OnScrollViewItemClicked(midi.album, midi.song, midi.name);
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
				.ShiftTo(scrollViewRect, new Vector2(hideShift, 0), .1f, EsType.QuadIn).Then()
				.Call(() => {
					action(); 
					scrollViewRect.anchoredPosition = new Vector2(pos.x + showShift, pos.y);
				})
				.FadeIn(scrollViewGroup, .1f, 0)
				.MoveTo(scrollViewRect, pos, .1f, EsType.QuadOut).Then()
				.Call(() => {
					scrollViewGroup.interactable = true;
					level.EnableBackButton();
				});
		}
	}
}
