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
		public Text statisticsText;
		public Text statusText;

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

		public int gameplayGenerationPreset {
			get { 
				switch (GameScheduler.instance.gameplayConfig.maxBlockCoalesceTime) {
					case 2: return 0;
					case 1: return 1;
					case .5f: return 2;
					case .05f: return 3;
					default: return 4;
				} 
			}
			set {
				GameScheduler.instance.gameplayConfig.layoutPreset = value;
				switch (value) {
					case 0: GameScheduler.instance.gameplayConfig.maxBlockCoalesceTime = 2; break;
					case 1: GameScheduler.instance.gameplayConfig.maxBlockCoalesceTime = 1; break;
					case 2: GameScheduler.instance.gameplayConfig.maxBlockCoalesceTime = .5f; break;
					default: GameScheduler.instance.gameplayConfig.maxBlockCoalesceTime = .05f; break;
				}
			}
		}

		ResourceStorage res;

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			res = game.resourceStorage;
		}

		public override void Enable() {
			base.Enable();

			InitMidiDetail();
			InitMidiRank();

			game.netManager.ClAppMidiGet(hash, (err, doc) => { 
				if (err != null) {
					Debug.LogError(err);
					return;
				}
				game.localDb.WriteDoc(LocalDb.COLLECTION_MIDIS, ((JsonObj)doc).Get<string>("_id"), doc);
			});
		}

		public override void Back() {
			level.selectedMidiId = null;
			level.selectedSongId = null;
			base.Back();
		}

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
			author = res.QueryPersonById(midi.authorId);
			
			// _id is path for custom midis
			byte[] bytes = System.IO.File.Exists(midi._id) ? System.IO.File.ReadAllBytes(midi._id) : 
				System.IO.File.ReadAllBytes(System.IO.Path.Combine(Net.WebCache.instance.rootPath, midi.hash));
			midiId = midi._id;
			midiFile = new MidiFile(bytes);
			sequenceCollection = new NoteSequenceCollection(midiFile);

			sourceText.text = string.Format("{0} • {1}", album.name.TranslateArtifact(), song.name.TranslateArtifact());
			titleText.text = midi.name;
			artistText.text = string.Format("by {0}", author?.name ?? midi.artistName);
			infoText.text = string.Format("{0:N0} Sequences • {1:N0} Notes • {2}",
				sequenceCollection.sequences.Count, sequenceCollection.noteCount, hash = MiscHelper.GetHexEncodedMd5Hash(bytes));
			statisticsText.text = string.Format(" <size=12>{0:N0}</size>   <size=12>{1:N0}</size>   <size=12>{2:N0}</size>   <size=12>{3:N0}</size>", midi.trialCount, midi.downloadCount, midi.voteSum, midi.loveCount);
			statusText.text = midi.status;

			if (string.IsNullOrWhiteSpace(midi.coverUrl)) {
				coverCutter.Cut(defaultTexture.name, defaultTexture);
			} else {
				Net.WebCache.instance.LoadTexture(midi.coverUrl, job => {
					coverCutter.Cut(job.GetKey(), job.GetData());
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
			//item.infoText.text = string.Format("{0:N0}/{1:N0} Perfects", trialObj.Get<double>("perfectCount"), totalCount);
			item.infoText.text = string.Format("<color=#B070FF>{0:N0}</color> <color=#FF7842>{1:N0}</color> <color=#3FF490>{2:N0}</color> <color=#69B6FF>{3:N0}</color> <color=#FF1A37>{4:N0}</color>", 
				trialObj.Get<double>("perfectCount"), 
				trialObj.Get<double>("greatCount"),
				trialObj.Get<double>("goodCount"), 
				trialObj.Get<double>("badCount"), 
				trialObj.Get<double>("missCount"));
			item.infoRightText.text = string.Format(" {0:N0}   {1:N0}x   {2:F2}%", trialObj.Get<double>("score"), trialObj.Get<double>("combo"), trialObj.Get<double>("accuracy") * 100);
			item.scoreText.text = string.Format("{0:F2} pp", trialObj.Get<double>("performance"));
			item.gradeText.text = trialObj.Get<string>("grade");
			item.rankText.text = rank.ToString();
			if (trialObj.ContainsKey("withdrew") && trialObj.Get<bool>("withdrew")) {
				item.group.alpha = .5f;
			} else {
				item.group.alpha = 1;
			}
			if (!string.IsNullOrEmpty(trialObj.Get<string>("userAvatarUrl"))) {
				Net.WebCache.instance.LoadTexture(trialObj.Get<string>("userAvatarUrl"), job => {
					game.ExecuteOnMain(() => { 
						item.imageCutter.Cut(job.GetKey(), job.GetData());
					});
				});
			} else {
				item.imageCutter.Cut(RefContainer.instance.imageCutterDefaultTexture.name, RefContainer.instance.imageCutterDefaultTexture);
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
			item.line2Text.text = string.Format(" {0:N0}   {1:N0}x   {2:F2}%", midi.avgScore, midi.avgCombo, midi.avgAccuracy * 100);
			item.action = () => {
				if (level.selectedMidiId != midi._id) {
					level.selectedMidiId = midi._id;
					InitMidiDetail(false);
					InitMidiRank();
				}
			};

			var coverUrl = midi.coverUrl;
			if (coverUrl != null) {
				Debug.Log("Midi cover url");
				Net.WebCache.instance.LoadTexture(coverUrl, job => {
					item.imageCutter.Cut(job.GetKey(), job.GetData());
				});
			} else {
				Debug.Log("Cut midi default");
				item.imageCutter.Cut(defaultTexture.name, defaultTexture);
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