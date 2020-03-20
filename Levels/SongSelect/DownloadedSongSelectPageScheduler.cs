using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using TouhouMix.Net;
using Jsonf;
using TouhouMix.Storage;
using Systemf;
using Uif;
using Uif.Tasks;

namespace TouhouMix.Levels.SongSelect {
	public sealed class DownloadedSongSelectPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public sealed class Album {
			public string name;
			public List<Midi> midiList = new List<Midi>();
		}

		public sealed class Song {
			public string name;
			public List<Midi> midiList = new List<Midi>();
		}

		public sealed class Midi {
			public Album album;
			public Song song;

			public string id;

			public string uploaderId;
			public string uploaderName;
			public string uploaderAvatarUrl;
			
			public string hash;
			public string name;
			public string desc;
			public string artistName;
			public string artistUrl;
			public string coverPath;
			public string coverUrl;
			public string coverBlurPath;
			public string coverBlurUrl;

			public string uploadedDate;
			public string approvedDate;

			public string status;

			public string sourceArtistName;
			public string sourceAlbumName;
			public string sourceSongName;
			public int touhouAlbumIndex;
			public int touhouSongIndex;

			public int trialCount;
			public int upCount;
			public int downCount;
			public int loveCount;
			public float avgScores;
			public float avgMaxCombo;
			public float avgAccuracy;
			public int passCount;
			public int failCount;
			public float sCutoff;
			public float aCutoff;
			public float bCutoff;
			public float cCutoff;
			public float dCutoff;
		}
		
		public RectTransform albumContentRect;
		public RectTransform midiContentRect;
		public GameObject listItemPrefab;

		public Texture2D defaultTexture;

		public Dictionary<string, Album> albumDict = new Dictionary<string, Album>();
		public Dictionary<Tuple<string, string>, Song> songDict = new Dictionary<Tuple<string, string>, Song>();

		Album selectedAlbum;

		readonly JsonContext json = new JsonContext();
		LocalDb db;
		ResourceStorage res;
		WebCache web;
		GameScheduler game;

		public override void Init(SongSelectLevelScheduler levelScheduler) {
			base.Init(levelScheduler);

			db = GameScheduler.instance.localDb;
			res = GameScheduler.instance.resourceStorage;
			web = WebCache.instance;
			game = GameScheduler.instance;
		}

		public override void Back() {
			level_.Pop();
		}

		public override AnimationSequence Show(AnimationSequence seq) {
			Debug.Log("SongSelectPageScheduler Show");
			return seq.Call(LoadMidis).Append(base.Show);
		}

		public void LoadMidis() {
			albumDict.Clear();
			songDict.Clear();

			string[] docIds = db.GetAllDocIds(LocalDb.COLLECTION_MIDIS);
			foreach (var docId in docIds) {
				var midiObj = db.ReadDoc(LocalDb.COLLECTION_MIDIS, docId);
				var midi = json.Parse<Midi>(midiObj);

				if (midi.touhouAlbumIndex > 0) {
					midi.sourceAlbumName = res.albumProtoDict[midi.touhouAlbumIndex].name;
					midi.sourceSongName = res.songProtoDict[
						Tuple.Create(midi.touhouAlbumIndex, midi.touhouSongIndex)].name;
				}

				string albumName = midi.sourceAlbumName;
				string songName = midi.sourceSongName;

				if (!albumDict.TryGetValue(albumName, out Album album)) {
					album = new Album { name = albumName };
					albumDict.Add(album.name, album);
				}
				album.midiList.Add(midi);
				midi.album = album;

				Song song;
				if (!songDict.TryGetValue(Tuple.Create(albumName, songName), out song)) {
					song = new Song { name = songName };
					songDict.Add(Tuple.Create(albumName, songName), song);
				}
				song.midiList.Add(midi);
				midi.song = song;
			}

			selectedAlbum = GetFirstAlbum();

			Render();
		}

		void Render() {
			RenderAlbumList();
			RenderMidiList();
		}

		Album GetFirstAlbum() {
			foreach (var album in albumDict.Values) {
				return album;
			}
			return null;
		}

		void RenderAlbumList() {
			int childCount = albumContentRect.childCount;
			int i = 0;
			foreach (var album in albumDict.Values) {
				SongSelectItemController item;
				if (i < childCount) {
					item = albumContentRect.GetChild(i).GetComponent<SongSelectItemController>();
					item.gameObject.SetActive(true);
				} else {
					item = Instantiate(listItemPrefab, albumContentRect, false).GetComponent<SongSelectItemController>() ;
				}
				RenderAlbum(item, album);

				i += 1;
			}

			for (; i < childCount; i++) {
				albumContentRect.GetChild(i).gameObject.SetActive(false);
			}
		}

		void RenderAlbum(SongSelectItemController item, Album album) {
			item.titleText.text = GetStringOrUnkonwn(album.name);
			item.line1Text.text = album.midiList.Count + " midis";
			item.line2Text.text = "";
			item.action = () => {
				selectedAlbum = album;
				RenderMidiList();
			};

			var coverUrl = FindFirstCoverUrl(album.midiList);
			if (coverUrl != null) {
				web.LoadTexture(coverUrl, job => {
					var texture = job.GetData();
					item.imageCutter.Cut(texture);
				});
			} else {
				item.imageCutter.Cut(defaultTexture);
			}
		}

		void RenderMidiList() {
			int childCount = midiContentRect.childCount;
			int i = 0;
			if (selectedAlbum != null) {
				foreach (var midi in selectedAlbum.midiList) {
					SongSelectItemController item;
					if (i < childCount) {
						item = midiContentRect.GetChild(i).GetComponent<SongSelectItemController>();
						item.gameObject.SetActive(true);
					} else {
						item = Instantiate(listItemPrefab, midiContentRect, false).GetComponent<SongSelectItemController>();
					}
					RenderMidi(item, midi);

					i += 1;
				}
			}

			for (; i < childCount; i++) {
				midiContentRect.GetChild(i).gameObject.SetActive(false);
			}
		}

		void RenderMidi(SongSelectItemController item, Midi midi) {
			item.titleText.text = GetStringOrUnkonwn(midi.name);
			item.line1Text.text = "by " + GetStringOrUnkonwn(midi.artistName);
			item.line2Text.text = " 0   0x   0%";
			item.action = () => {
				Debug.Log("ShowMidiDetail");
				level_.selectedDownloadedMidi = midi;
				level_.Push(level_.midiDetailPage);
			};

			var coverUrl = midi.coverUrl;
			if (coverUrl != null) {
				web.LoadTexture(coverUrl, job => {
					var texture = job.GetData();
					item.imageCutter.Cut(texture);
				});
			} else {
				item.imageCutter.Cut(defaultTexture);
			}
		}

		public static string GetStringOrUnkonwn(string str) {
			return string.IsNullOrWhiteSpace(str) ? "Unknown" : str;
		}

		public static string FindFirstCoverUrl(List<Midi> midiList) {
			foreach (var midi in midiList) {
				if (!string.IsNullOrWhiteSpace(midi.coverUrl)) {
					return midi.coverUrl;
				}
			}
			return null;
		}
	}
}

