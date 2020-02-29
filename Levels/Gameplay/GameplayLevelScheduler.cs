using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using Midif.V3;
using TouhouMix.Prefabs;
using TouhouMix.Storage.Protos.Json.V1;

namespace TouhouMix.Levels.Gameplay {
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		const int MOUSE_TOUCH_ID = -100;

		public CanvasSizeWatcher sizeWatcher;

		[Space]
		public CanvasGroup readyPageGroup;
		public Text readyPageText;
		public CanvasGroup gameplayPageGroup;
		public Button pauseButton;
		public RectTransform judgeRect;
		public float judgeHeight = 80;
		float cacheHeight;
		public CanvasGroup pausePageGroup;

		[Space]
		public int cacheEsType;
		public float cacheBeats = 2;
		public int graceEsType;
		public float graceBeats = 1;
		float cacheTicks;
		float graceTicks;

		[Space]
		public GameObject instantBlockPrefab;
		public GameObject shortBlockPrefab;
		public GameObject longBlockPrefab;
		public RectTransform instantBlockPageRect;
		public RectTransform shortBlockPageRect;
		public RectTransform longBlockPageRect;
		public float maxInstantBlockSeconds = .2f;
		public float maxShortBlockSeconds = .8f;
		public float endDelayBeats = 2;
		float beatsPerTick;
		float endTicks;

		[Space]
		public int laneCount;
		public float[] laneXDict;
		public float blockWidth = 100;
		public float blockJudgingWidth = 120;
		float blockJudgingHalfWidth;

		[Space]
		public ScoringManager scoringManager;

		GameScheduler game_;
		AnimationManager anim_;

		float ticks;
		bool hasStarted;
		bool isPaused;
		bool hasEnded;

		int sampleRate;

		string midiFileSha256Hash;
		Sf2File sf2File;
		MidiFile midiFile;
		NoteSequenceCollection sequenceCollection;

		Sf2Synth sf2Synth;
		MidiSequencer midiSequencer;

		MidiSynthConfigProto synthConfig;

		readonly List<NoteSequenceCollection.Sequence> gameSequences = new List<NoteSequenceCollection.Sequence>();
		readonly List<NoteSequenceCollection.Sequence> backgroundSequences = new List<NoteSequenceCollection.Sequence>();

		public sealed class Block {
			public enum BlockType {
				Instant,
				Short,
				Long,
			}

			public BlockType type;
			public NoteSequenceCollection.Note note;
			public List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();

			public float end;
			public RectTransform rect;
			public ISettable<Color> color;

			public int lane;
			public float x;
			public int index;

			public int holdingFingerId;
			public float holdingOffset;
			public float holdingX;

			public void Reset() {
				rect.gameObject.SetActive(true);
				holdingFingerId = -1;
			}
		}

		sealed class BackgroundTrack {
			public int seqNoteIndex;
		}
		int backgroundNoteFreeStartIndex;
		readonly List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();
		BackgroundTrack[] backgroundTracks;
		BackgroundTrack[] gameTracks;

		readonly List<Block> instantBlocks = new List<Block>();
		readonly List<Block> shortBlocks = new List<Block>();
		readonly List<Block> longBlocks = new List<Block>();
		int instantBlocksFreeStartIndex;
		int shortBlocksFreeStartIndex;
		int longBlocksFreeStartIndex;

		readonly Dictionary<int, Block> holdingBlockDict = new Dictionary<int, Block>();
		readonly Dictionary<int, Block> tentativeBlockDict = new Dictionary<int, Block>();
		readonly HashSet<int> touchedLaneSet = new HashSet<int>();

		public void Start() {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;

			hasStarted = false;

			judgeRect.anchoredPosition = new Vector2(0, judgeHeight);
			cacheHeight = sizeWatcher.canvasSize.y - judgeHeight;

			sf2File = new Sf2File(Resources.Load<TextAsset>("sf2/GeneralUser GS v1.471").bytes);

			var audioConfig = AudioSettings.GetConfiguration();
			sampleRate = audioConfig.sampleRate;
			sf2Synth = new Sf2Synth(sf2File, new Sf2Synth.Table(sampleRate), 64);
			sf2Synth.SetVolume(-10);

			midiFile = game_.midiFile ?? new MidiFile(Resources.Load<TextAsset>("test").bytes);
			sequenceCollection = game_.noteSequenceCollection ?? new NoteSequenceCollection(midiFile);

			midiFileSha256Hash = MiscHelper.GetBase64EncodedSha256Hash(midiFile.bytes);

			cacheTicks = cacheBeats * midiFile.ticksPerBeat;
			graceTicks = graceBeats * midiFile.ticksPerBeat;
			beatsPerTick = 1 / (float)midiFile.ticksPerBeat;
			endTicks = sequenceCollection.end + graceTicks + endDelayBeats * midiFile.ticksPerBeat;

			blockJudgingHalfWidth = blockJudgingWidth * .5f;

			float canvasWidth = sizeWatcher.canvasSize.x;
			laneXDict = new float[laneCount];
			float laneStart = blockWidth * .5f;
			float laneSpacing = (canvasWidth - blockWidth) / (laneCount - 1);
			for (int i = 0; i < laneXDict.Length; i++) {
				laneXDict[i] = laneStart + i * laneSpacing;
			}

			midiSequencer = new MidiSequencer(midiFile, sf2Synth);
			midiSequencer.isMuted = true;

			synthConfig = MidiSynthConfigProto.LoadOrCreateDefault(game_, sequenceCollection, midiFileSha256Hash);
			foreach (var seqConfig in synthConfig.sequenceStateList) {
				if (seqConfig.shouldUseInGame) {
					gameSequences.Add(sequenceCollection.sequences[seqConfig.sequenceIndex]);
				} else if (!seqConfig.isMuted) {
					backgroundSequences.Add(sequenceCollection.sequences[seqConfig.sequenceIndex]);
				}
			}
			Debug.LogFormat("background tracks: {0}, game tracks: {1}", backgroundSequences.Count, gameSequences.Count);

			backgroundTracks = new BackgroundTrack[backgroundSequences.Count];
			for (int i = 0; i < backgroundTracks.Length; i++) backgroundTracks[i] = new BackgroundTrack();
			gameTracks = new BackgroundTrack[gameSequences.Count];
			for (int i = 0; i < gameTracks.Length; i++) gameTracks[i] = new BackgroundTrack();

			pausePageGroup.gameObject.SetActive(false);

			scoringManager.Init(this);

			midiSequencer.ticks = -cacheTicks;
			ShowReadyAnimation();
		}

		void ShowReadyAnimation() {
			readyPageGroup.gameObject.SetActive(true);
			readyPageGroup.alpha = 0;
			anim_.New().FadeIn(readyPageGroup, .5f, 0).Then()
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
//				.Set(readyPageText.GetStringSettable(), "3")
//				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
//				.Set(readyPageText.GetStringSettable(), "2")
//				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
//				.Set(readyPageText.GetStringSettable(), "1")
//				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "Go")
				.Set(readyPageText.GetAlphaFloatSettable(), 1)
				.FadeOutFromOne(readyPageGroup, 1, EsType.QuadIn).Then()
				.Call(StartGame);
		}

		void StartGame() {
			#if UNITY_ANDROID
			Screen.autorotateToLandscapeLeft = false;
			Screen.autorotateToLandscapeRight = false;
			#endif

			readyPageGroup.gameObject.SetActive(false);
			hasStarted = true;
		}

		void EndGame() {
			Debug.Log("game end");
			anim_.New()
				.FadeOut(gameplayPageGroup, 1, 0).Then()
				.Call(() => {
					scoringManager.ReportScores();

					#if UNITY_ANDROID
					Screen.autorotateToLandscapeLeft = true;
					Screen.autorotateToLandscapeRight = true;
					#endif

					UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_RESULT_LEVEL_BUILD_INDEX);
				});
		}

		void Update() {
			if (!hasStarted || isPaused || hasEnded) return;

			midiSequencer.AdvanceTime(Time.deltaTime);
			ticks = midiSequencer.ticks;

			if (ticks >= endTicks) {
				hasEnded = true;
				EndGame();
				return;
			}

			UpdateBackgroundNotes();

			GenerateGameNotes();

			#if UNITY_EDITOR || UNITY_STANDALONE
			ProcessMouse();
			#else
			ProcessTouches();
			#endif

			UpdateBlocks();

			scoringManager.SetProgress(ticks / sequenceCollection.end);

			sf2Synth.Panic();
		}

		public void OnPauseButtonClicked() {
			pauseButton.interactable = false;
			isPaused = true;

			pausePageGroup.gameObject.SetActive(true);
			pausePageGroup.alpha = 0;
			anim_.New().FadeIn(pausePageGroup, .2f, 0);
		}

		public void OnRestartButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
		}

		public void OnResumeButtonClicked() {
			anim_.New().FadeOut(pausePageGroup, .2f, 0).Then()
				.Call(() => {
					pausePageGroup.gameObject.SetActive(false);
					pauseButton.interactable = true;
					isPaused = false;
				});
		}

		public void OnStopButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.SONG_SELECT_LEVEL_BUILD_INDEX);
		}

		void OnAudioFilterRead (float[] buffer, int channel) {
			if (sf2Synth != null) sf2Synth.Process(buffer);
		}
	}
}
