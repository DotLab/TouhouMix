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

		ResourceStorage res_;

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

			res_ = game_.resourceStorage;

			Init();
		}

		public void Init() {
			if (!string.IsNullOrEmpty(level_.selectedMidi)) {  // show midi detail
				ShowMidiDetail();
			} else if (level_.selectedSong != -1) {  // show midi scroll
				PopulateMidis();
				scrollViewPositionYSafe = level_.midiSelectScrollViewPositionY;
			} else if (level_.selectedAlbum != -1) {  // show song scroll
				PopulateSongs();
				scrollViewPositionYSafe = level_.songSelectScrollViewPositionY;
			} else {  // song album scroll
				PopulateAlbums();
				scrollViewPositionYSafe = level_.albumSelectScrollViewPositionY;
			}
		}

		void OnScrollViewItemClicked(int album, int song, string midi) {
			Debug.Log("OnScrollViewItemClicked " + midi);
			if (midi == null) {
				RefreshScrollView(-200, 400, () => {
					if (song != -1) {  // select song
						anim_.New().EditTo(titleText, res_.albumProtoDict[album].name + " • " + res_.songProtoDict[Systemf.Tuple.Create(album, song)].name, 1, 0);
						level_.selectedSong = song;
						level_.songSelectScrollViewPositionY = scrollViewPositionY;
						PopulateMidis();
						scrollViewPositionY = 0;
					} else if (album != -1) {  // select album
						anim_.New().EditTo(titleText, res_.albumProtoDict[album].name, 1, 0);
						level_.selectedAlbum = album;
						level_.albumSelectScrollViewPositionY = scrollViewPositionY;
						PopulateSongs();
						scrollViewPositionY = 0;
					}
				});
			} else {  // select midi
				level_.selectedMidi = midi;
				level_.midiSelectScrollViewPositionY = scrollViewPositionY;
				ShowMidiDetail();
			}
		}

		public override void Back() {
			if (level_.selectedAlbum != -1) {
				RefreshScrollView(200, -400, () => {
					if (level_.selectedSong != -1) {  // back from midi select
						level_.selectedSong = -1;
						PopulateSongs();
						scrollViewPositionYSafe = level_.songSelectScrollViewPositionY;
					} else if (level_.selectedAlbum != -1) {  // back from song select
						level_.selectedAlbum = -1;
						PopulateAlbums();
						scrollViewPositionYSafe = level_.albumSelectScrollViewPositionY;
					}
				});
			}
		}

		public override AnimationSequence Show(AnimationSequence seq) {
			Debug.Log("SongSelectPageScheduler Show");
			return seq.Call(Init).Append(base.Show);
		}

		void PopulateAlbums() {
			PopulateScrollViewContent(res_.QueryAlbums(), (controller, data) => {
				var album = data as TouhouMix.Storage.Protos.Resource.AlbumProto;
				controller.Init(album.tag, album.songCount.ToString("N0"), album.name, () => {
					OnScrollViewItemClicked(album.album, -1, null);
				});
			});
		}

		void PopulateSongs() {
			PopulateScrollViewContent(res_.QuerySongsByAlbum(level_.selectedAlbum), (controller, data) => {
				var song = data as TouhouMix.Storage.Protos.Resource.SongProto;
				controller.Init(song.song.ToString("N0"), song.midiCount.ToString("N0"), song.name, () => {
					OnScrollViewItemClicked(song.album, song.song, null);
				});
			});
		}

		void PopulateMidis() {
			PopulateScrollViewContent(res_.QueryMidisByAlbumAndSong(level_.selectedAlbum, level_.selectedSong), (controller, data) => {
				var midi = data as TouhouMix.Storage.Protos.Resource.MidiProto;
				var author = res_.authorProtoDict[midi.author];
				controller.Init(author.tag, "", midi.name, () => {
					OnScrollViewItemClicked(midi.album, midi.song, midi.name);
				});
			});
		}

		void ShowMidiDetail() {
			Debug.Log("ShowMidiDetail");
			level_.Push(level_.midiDetailPage);
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
			level_.DisableBackButton();
			AnimationManager.instance.New()
				.FadeOut(scrollViewGroup, .2f, 0)
				.ShiftTo(scrollViewRect, new Vector2(hideShift, 0), .2f, EsType.QuadIn).Then()
				.Call(() => {
					action(); 
					scrollViewRect.anchoredPosition = new Vector2(pos.x + showShift, pos.y);
				})
				.FadeIn(scrollViewGroup, .5f, 0)
				.MoveTo(scrollViewRect, pos, .5f, EsType.BackOut).Then()
				.Call(() => {
					scrollViewGroup.interactable = true;
					level_.EnableBackButton();
				});
		}
	}
}
