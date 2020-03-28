using UnityEngine.UI;
using UnityEngine;
using TouhouMix.Storage;
using TouhouMix.Storage.Protos.Resource;
using Systemf;
using Uif;
using Uif.Tasks;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using Midif.V3;
using Jsonf;
using TouhouMix.Levels.SongSelect.MidiDetailPage;

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

		public AlbumProto album;
		public SongProto song;
		public MidiProto midi;
		public AuthorProto author;

		public MidiFile midiFile;
		public NoteSequenceCollection sequenceCollection;

		public Texture2D defaultTexture;

		public string hash;

		public int gameplayLayoutPreset {
			get { return GameScheduler.instance.gameplayConfig.layoutPreset; }
			set { GameScheduler.instance.gameplayConfig.layoutPreset = value; }
		}

		ResourceStorage res_;

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			res_ = game_.resourceStorage;
		}

		public void Init() {
			if (level_.selectedDownloadedMidi == null) {
				InitLocal();
			} else {
				InitDownloaded();
			}
			InitRank();
		}

		void InitLocal() {
			level_.backgroundImage.texture = level_.defaultBackgroundTexture;
			game_.backgroundTexture = null;

			try {
				album = res_.albumProtoDict[level_.selectedAlbum];
				song = res_.songProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong)];
				midi = res_.midiProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong, level_.selectedMidi)];
			} catch (System.Exception e) {
				Debug.LogError(e);

				album = res_.albumProtoDict[level_.selectedAlbum = 6];
				song = res_.songProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong = 1)];
				midi = res_.midiProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong, level_.selectedMidi = "aka_easy")];
			}
			author = res_.authorProtoDict[midi.author];

			byte[] bytes = midi.isFile ? System.IO.File.ReadAllBytes(midi.path) : Resources.Load<TextAsset>(midi.path).bytes;
			midiFile = new MidiFile(bytes);
			sequenceCollection = new NoteSequenceCollection(midiFile);

			sourceText.text = string.Format("{0} • {1}", album.name, song.name);
			titleText.text = midi.name;
			artistText.text = string.Format("by {0}", author.name);
			infoText.text = string.Format("{0:N0} Sequences • {1:N0} Notes • {2}",
				sequenceCollection.sequences.Count, sequenceCollection.noteCount, hash = MiscHelper.GetHexEncodedMd5Hash(bytes));
		}

		void InitDownloaded(bool renderList = true) {
			var downloadedMidi = level_.selectedDownloadedMidi;
			if (!string.IsNullOrEmpty(downloadedMidi.coverBlurUrl)) {
				Net.WebCache.instance.LoadTexture(downloadedMidi.coverBlurUrl, job => {
					level_.backgroundImage.texture = job.GetData();
					game_.backgroundTexture = job.GetData();
				});
			} else {
				level_.backgroundImage.texture = level_.defaultBackgroundTexture;
				game_.backgroundTexture = null;
			}

			album = new AlbumProto { name = downloadedMidi.sourceAlbumName };
			song = new SongProto { name = downloadedMidi.sourceSongName };
			midi = new MidiProto { name = downloadedMidi.name };

			byte[] bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(Net.WebCache.instance.rootPath, downloadedMidi.hash));
			midiFile = new MidiFile(bytes);
			sequenceCollection = new NoteSequenceCollection(midiFile);

			sourceText.text = string.Format("{0} • {1}", album.name, song.name);
			titleText.text = midi.name;
			artistText.text = string.Format("by {0}", author.name);
			infoText.text = string.Format("{0:N0} Sequences • {1:N0} Notes • {2}",
				sequenceCollection.sequences.Count, sequenceCollection.noteCount, hash = MiscHelper.GetHexEncodedMd5Hash(bytes));

			if (string.IsNullOrWhiteSpace(downloadedMidi.coverUrl)) {
				coverCutter.Cut(defaultTexture);
			} else {
				Net.WebCache.instance.LoadTexture(downloadedMidi.coverUrl, job => {
					coverCutter.Cut(job.GetData());
				});
			}

			if (renderList) {
				RenderDownloadedMidiList();
			}
		}

		void InitRank() {
			game_.netManager.ClAppMidiRecordList(hash, 0, (error, data) => {
				if (!string.IsNullOrEmpty(error)) {
					Debug.LogWarning(error);
					return;
				}
				
				game_.ExecuteOnMain(() => {
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
					game_.ExecuteOnMain(() => { 
						item.imageCutter.Cut(job.GetData());
					});
				});
			}
		}

		void RenderDownloadedMidiList() {
			int childCount = midiContentRect.childCount;
			int i = 0;
			foreach (var midi in level_.selectedDownloadedMidi.song.midiList) {
				SongSelectItemController item;
				if (i < childCount) {
					item = midiContentRect.GetChild(i).GetComponent<SongSelectItemController>();
					item.gameObject.SetActive(true);
				} else {
					item = Instantiate(listItemPrefab, midiContentRect, false).GetComponent<SongSelectItemController>();
				}
				RenderDownloadedMidi(item, midi);

				i += 1;
			}

			for (; i < childCount; i++) {
				midiContentRect.GetChild(i).gameObject.SetActive(false);
			}
		}

		void RenderDownloadedMidi(SongSelectItemController item, DownloadedSongSelectPageScheduler.Midi midi) {
			item.titleText.text = DownloadedSongSelectPageScheduler.GetStringOrUnkonwn(midi.name);
			item.line1Text.text = "by " + DownloadedSongSelectPageScheduler.GetStringOrUnkonwn(midi.artistName);
			item.line2Text.text = " 0   0x   0%";
			item.action = () => {
				level_.selectedDownloadedMidi = midi;
				InitDownloaded(false);
			};

			var coverUrl = midi.coverUrl;
			if (coverUrl != null) {
				Net.WebCache.instance.LoadTexture(coverUrl, job => {
					var texture = job.GetData();
					item.imageCutter.Cut(texture);
				});
			} else {
				item.imageCutter.Cut(defaultTexture);
			}
		}

		public override AnimationSequence Show(AnimationSequence seq) {
			return seq.Call(Init).Append(base.Show);
		}

		public override void Back() {
			level_.selectedMidi = null;
			level_.selectedDownloadedMidi = null;
			level_.Pop();
		}

		public void OnMusicButtonClicked() {
			level_.Push(level_.synthConfigPage);
		}

		public void OnPlayButtonClicked() {
			Debug.Log("Loading Gameplay");
			game_.midiFile = midiFile;
			game_.noteSequenceCollection = sequenceCollection;
			game_.title = midi.name;
			game_.subtitle = string.Format(
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