using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using System.Collections.Generic;

namespace TouhouMix.Levels.SongSelect {
	public sealed class SongSelectLevelScheduler : MonoBehaviour {
		public CanvasGroup scrollViewGroup;
		public RectTransform scrollViewRect;
		public RectTransform scrollViewContentRect;
		public Button backButton;

		[Space]
		public GameObject scrollViewItemPrefab;

		public int selectedAlbum { 
			get { return GameScheduler.instance.uiStateProto.selectedAlbum; }
			set { GameScheduler.instance.uiStateProto.selectedAlbum = value; } 
		}
		public float albumSelectScrollViewPositionY { 
			get { return GameScheduler.instance.uiStateProto.albumSelectScrollViewPositionY; } 
			set { Debug.Log(value); GameScheduler.instance.uiStateProto.albumSelectScrollViewPositionY = value; } 
		}
		public int selectedSong { 
			get { return GameScheduler.instance.uiStateProto.selectedSong; }
			set { GameScheduler.instance.uiStateProto.selectedSong = value; } 
		}
		public float songSelectScrollViewPositionY { 
			get { return GameScheduler.instance.uiStateProto.songSelectScrollViewPositionY; } 
			set { GameScheduler.instance.uiStateProto.songSelectScrollViewPositionY = value; } 
		}
		public string selectedMidi { 
			get { return GameScheduler.instance.uiStateProto.selectedMidi; }
			set { GameScheduler.instance.uiStateProto.selectedMidi = value; } 
		}

		TouhouMix.Storage.ResourceStorage resourceStorage_;

		void Start() {
			resourceStorage_ = GameScheduler.instance.resourceStorage;
			PopulateAlbums();
		}

		void OnScrollViewItemClicked(int album, int song, string midi) {
			RefreshScrollView(-200, 400, () => {
				if (song != -1) {  // song selected
					selectedSong = song;
					songSelectScrollViewPositionY = scrollViewContentRect.anchoredPosition.y;
					PopulateMidis();
					scrollViewContentRect.sizeDelta = new Vector2(0, 1000000);  // prevent ScrollView from buncing back
					scrollViewContentRect.anchoredPosition = new Vector2();
				} else if (album != -1) {  // album selected
					selectedAlbum = album;
					albumSelectScrollViewPositionY = scrollViewContentRect.anchoredPosition.y;
					PopulateSongs();
					scrollViewContentRect.sizeDelta = new Vector2(0, 1000000);  // prevent ScrollView from buncing back
					scrollViewContentRect.anchoredPosition = new Vector2();
				}
			});
		}

		public void OnBackButtonClicked() {
			RefreshScrollView(200, -400, () => {
				if (selectedSong != -1) {  // back from midi select
					selectedSong = -1;
					PopulateSongs();
					scrollViewContentRect.sizeDelta = new Vector2(0, 1000000);  // prevent ScrollView from buncing back
					scrollViewContentRect.anchoredPosition = new Vector2(0, songSelectScrollViewPositionY);
				} else if (selectedAlbum != -1) {  // back from song select
					selectedAlbum = -1;
					PopulateAlbums();
					scrollViewContentRect.sizeDelta = new Vector2(0, 1000000);  // prevent ScrollView from buncing back
					scrollViewContentRect.anchoredPosition = new Vector2(0, albumSelectScrollViewPositionY);
				}
			});
		}

		void PopulateAlbums() {
			PopulateScrollViewContent(resourceStorage_.QueryAlbums(), (controller, data) => {
				var album = data as TouhouMix.Storage.Protos.Resource.AlbumProto;
				controller.Init(album.tag, album.songCount.ToString("N0"), album.name, () => {
					OnScrollViewItemClicked(album.album, -1, null);
				});
			});
		}

		void PopulateSongs() {
			PopulateScrollViewContent(resourceStorage_.QuerySongsByAlbum(selectedAlbum), (controller, data) => {
				var song = data as TouhouMix.Storage.Protos.Resource.SongProto;
				controller.Init(song.song.ToString("N0"), song.midiCount.ToString("N0"), song.name, () => {
					OnScrollViewItemClicked(song.album, song.song, null);
				});
			});
		}

		void PopulateMidis() {
			PopulateScrollViewContent(resourceStorage_.QueryMidisByAlbumAndSong(selectedAlbum, selectedSong), (controller, data) => {
				var midi = data as TouhouMix.Storage.Protos.Resource.MidiProto;
				var author = resourceStorage_.authorProtoDict[midi.author];
				controller.Init(author.tag, "", midi.name, () => {
					OnScrollViewItemClicked(midi.album, midi.song, midi.name);
				});
			});
		}

		void PopulateScrollViewContent(IEnumerable<object> dataSource, System.Action<ScrollViewItemController, object> action) {
			int itemCount = scrollViewContentRect.childCount;
			int i = 0;
			foreach (var data in dataSource) {
				var item = i < itemCount ? scrollViewContentRect.GetChild(i++).gameObject : Instantiate(scrollViewItemPrefab, scrollViewContentRect);
				item.SetActive(true);
				action(item.GetComponent<ScrollViewItemController>(), data);
			}
			for (; i < itemCount; i++) {
				scrollViewContentRect.GetChild(i).gameObject.SetActive(false);
			}
		}

		void RefreshScrollView(float hideShift, float showShift, System.Action action) {
			var pos = scrollViewRect.anchoredPosition;
			scrollViewGroup.interactable = false;
			backButton.interactable = false;
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
					backButton.interactable = true;
				});
		}
	}
}