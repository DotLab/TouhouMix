using UnityEngine.UI;
using UnityEngine;
using TouhouMix.Storage;
//using TouhouMix.Storage.Protos.Resource;
using Systemf;
using Uif;
using Uif.Tasks;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using Midif.V3;
using Jsonf;
using TouhouMix.Levels.SongSelect.MidiDetailPage;
using TouhouMix.Net;

namespace TouhouMix.Levels.SongSelect {
	public class MidiDetailPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public Text sourceText;
		public Text titleText;
		public Text artistText;
		public Text infoText;

		public RawImageCutter coverCutter;

		public RectTransform midiContentRect;
		public GameObject listItemPrefab;

		public RectTransform rankContentRect;
		public GameObject rankItemPrefab;

		public string midiId;
		public string hash;
		public Storage.Protos.Api.MidiProto midi;
		public Storage.Protos.Api.SongProto song;
		public Storage.Protos.Api.AlbumProto album;
		public Storage.Protos.Api.PersonProto author;

		public MidiFile midiFile;
		public NoteSequenceCollection sequenceCollection;

		public Texture2D defaultTexture;

		public int gameplayLayoutPreset {
			get { return GameScheduler.instance.gameplayConfig.layoutPreset; }
			set { GameScheduler.instance.gameplayConfig.layoutPreset = value; }
		}

		ResourceStorage res;

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			res = game.resourceStorage;
		}

		public override void Enable() {
			base.Enable();

			//if (level.selectedDownloadedMidi == null) {
			//	InitLocal();
			//} else {
				InitMidiDetail();
			//}
			InitMidiRank();
		}

		public override void Back() {
			base.Back();
			level.selectedMidiId = null;
			level.selectedSongId = null;
			//level.selectedDownloadedMidi = null;
		}

		//void InitLocal() {
		//	level.backgroundImage.texture = level.defaultBackgroundTexture;
		//	game.backgroundTexture = null;
		//	midiId = null;

		//	try {
		//		album = res_.albumProtoDict[level.selectedAlbumId];
		//		song = res_.songProtoDict[Tuple.Create(level.selectedAlbumId, level.selectedSongId)];
		//		midi = res_.midiProtoDict[Tuple.Create(level.selectedAlbumId, level.selectedSongId, level.selectedMidi)];
		//	} catch (System.Exception e) {
		//		Debug.LogError(e);

		//		album = res_.albumProtoDict[level.selectedAlbumId = 6];
		//		song = res_.songProtoDict[Tuple.Create(level.selectedAlbumId, level.selectedSongId = 1)];
		//		midi = res_.midiProtoDict[Tuple.Create(level.selectedAlbumId, level.selectedSongId, level.selectedMidi = "aka_easy")];
		//	}
		//	author = res_.authorProtoDict[midi.author];

		//	byte[] bytes = midi.isFile ? System.IO.File.ReadAllBytes(midi.path) : Resources.Load<TextAsset>(midi.path).bytes;
		//	midiFile = new MidiFile(bytes);
		//	sequenceCollection = new NoteSequenceCollection(midiFile);

		//	sourceText.text = string.Format("{0} • {1}", album.name, song.name);
		//	titleText.text = midi.name;
		//	artistText.text = string.Format("by {0}", author.name);
		//	infoText.text = string.Format("{0:N0} Sequences • {1:N0} Notes • {2}",
		//		sequenceCollection.sequences.Count, sequenceCollection.noteCount, hash = MiscHelper.GetHexEncodedMd5Hash(bytes));
		//}

		void InitMidiDetail(bool renderMidiList = true) {
			midi = res.QueryMidiById(level.selectedMidiId);
			if (!string.IsNullOrEmpty(midi.coverBlurUrl)) {
				Net.WebCache.instance.LoadTexture(midi.coverBlurUrl, job => {
					level.backgroundImage.texture = job.GetData();
					game.backgroundTexture = job.GetData();
				});
			} else {
				level.backgroundImage.texture = level.defaultBackgroundTexture;
				game.backgroundTexture = null;
			}

			song = res.QuerySongById(midi.songId);
			album = res.QueryAlbumById(song.albumId);
			
			byte[] bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(Net.WebCache.instance.rootPath, midi.hash));
			midiId = midi._id;
			midiFile = new MidiFile(bytes);
			sequenceCollection = new NoteSequenceCollection(midiFile);

			sourceText.text = string.Format("{0} • {1}", album.name.TranslateArtifact(), song.name.TranslateArtifact());
			titleText.text = midi.name;
			artistText.text = string.Format("by {0}", author.name);
			infoText.text = string.Format("{0:N0} Sequences • {1:N0} Notes • {2}",
				sequenceCollection.sequences.Count, sequenceCollection.noteCount, hash = MiscHelper.GetHexEncodedMd5Hash(bytes));

			if (string.IsNullOrWhiteSpace(midi.coverUrl)) {
				coverCutter.Cut(defaultTexture);
			} else {
				Net.WebCache.instance.LoadTexture(midi.coverUrl, job => {
					coverCutter.Cut(job.GetData());
				});
			}

			if (renderMidiList) {
				RenderMidiList(midi.songId);
			}
		}

		void InitMidiRank() {
			game.netManager.ClAppMidiRecordList(hash, 0, (error, data) => {
				if (!string.IsNullOrEmpty(error)) {
					Debug.LogWarning(error);
					return;
				}
				
				game.ExecuteOnMain(() => {
					var recordList = (JsonList)data;
					int childCount = rankContentRect.childCount;
					int i = 0;
					foreach (var record in recordList) {
						RankItemController item;
						if (i < childCount) {
							item = rankContentRect.GetChild(i).GetComponent<RankItemController>();
							item.gameObject.SetActive(true);
						} else {
							item = Instantiate(rankItemPrefab, rankContentRect, false).GetComponent<RankItemController>();
						}
						RenderRankItem(item, (JsonObj)record, i + 1);

						i += 1;
					}

					for (; i < childCount; i++) {
						rankContentRect.GetChild(i).gameObject.SetActive(false);
					}
				});
			});
		}

		void RenderRankItem(RankItemController item, JsonObj trialObj, int rank) {
			item.nameText.text = trialObj.Get<string>("userName");
			int totalCount = (int)(trialObj.Get<double>("perfectCount") + trialObj.Get<double>("greatCount") 
				+ trialObj.Get<double>("goodCount") + trialObj.Get<double>("badCount") + trialObj.Get<double>("missCount"));
			item.infoText.text = string.Format("{0:N0}/{1:N0} Perfects", trialObj.Get<double>("perfectCount"), totalCount);
			item.infoRightText.text = string.Format(" {0:N0}x   {1:F2}%", trialObj.Get<double>("combo"), trialObj.Get<double>("accuracy") * 100);
			item.scoreText.text = string.Format("{0:N0}", trialObj.Get<double>("score"));
			item.gradeText.text = GetGrade(trialObj.Get<double>("accuracy"));
			item.rankText.text = rank.ToString();
			if (!string.IsNullOrEmpty(trialObj.Get<string>("userAvatarUrl"))) {
				Net.WebCache.instance.LoadTexture(trialObj.Get<string>("userAvatarUrl"), job => {
					game.ExecuteOnMain(() => { 
						item.imageCutter.Cut(job.GetData());
					});
				});
			}
		}

		void RenderMidiList(string songId) {
			int childCount = midiContentRect.childCount;
			int i = 0;
			foreach (var midi in res.QueryMidisBySongId(songId)) {
				SongSelectItemController item;
				if (i < childCount) {
					item = midiContentRect.GetChild(i).GetComponent<SongSelectItemController>();
					item.gameObject.SetActive(true);
				} else {
					item = Instantiate(listItemPrefab, midiContentRect, false).GetComponent<SongSelectItemController>();
				}
				RenderMidiListItem(item, midi);

				i += 1;
			}

			for (; i < childCount; i++) {
				midiContentRect.GetChild(i).gameObject.SetActive(false);
			}
		}

		void RenderMidiListItem(SongSelectItemController item, Storage.Protos.Api.MidiProto midi) {
			Debug.Log("Midi list item");

			item.titleText.text = DownloadedSongSelectPageScheduler.GetStringOrUnkonwn(midi.name);
			item.line1Text.text = "by " + DownloadedSongSelectPageScheduler.GetStringOrUnkonwn(midi.artistName);
			item.line2Text.text = " 0   0x   0%";
			item.action = () => {
				level.selectedMidiId = midi._id;
				InitMidiDetail(false);
				InitMidiRank();
			};

			var coverUrl = midi.coverUrl;
			if (coverUrl != null) {
				Debug.Log("Midi cover url");
				Net.WebCache.instance.LoadTexture(coverUrl, job => {
					Debug.Log("Midi cover url downloaded");
					var texture = job.GetData();
					game.ExecuteOnMain(() => {
						Debug.Log("Cut midi");
						item.imageCutter.Cut(texture);
					});
				});
			} else {
				Debug.Log("Cut midi default");
				item.imageCutter.Cut(defaultTexture);
			}
		}

		public void OnMusicButtonClicked() {
			level.Push(level.synthConfigPage);
		}

		public void OnPlayButtonClicked() {
			Debug.Log("Loading Gameplay");
			game.midiId = midiId;
			game.midiFile = midiFile;
			game.noteSequenceCollection = sequenceCollection;
			game.title = midi.name;
			game.subtitle = string.Format(
				"{0} • {1}", album.name, song.name);
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
		}

		static string GetGrade(double accuracy) {
			if (accuracy == 1) return "Ω";
			if (accuracy >= .9999) return "SSS";
			if (accuracy >= .999) return "SS";
			if (accuracy >= .99) return "S";
			if (accuracy >= .98) return "A+";
			if (accuracy >= .92) return "A";
			if (accuracy >= .9) return "A-";
			if (accuracy >= .88) return "B+";
			if (accuracy >= .82) return "B";
			if (accuracy >= .8) return "B-";
			if (accuracy >= .78) return "C+";
			if (accuracy >= .72) return "C";
			if (accuracy >= .7) return "C-";
			if (accuracy >= .68) return "D+";
			if (accuracy >= .62) return "D";
			if (accuracy >= .6) return "D-";
			return "F";
		}
	}
}