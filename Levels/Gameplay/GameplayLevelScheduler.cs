using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using Midif.V3;
using Systemf;
using Uif.Extensions;
using Uif.Settables.Components;
using TouhouMix.Prefabs;
using TouhouMix.Storage.Protos.Json.V1;

namespace TouhouMix.Levels.Gameplay {
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		const int MOUSE_TOUCH_ID = -100;

		public TouhouMix.Prefabs.CanvasSizeWatcher sizeWatcher;

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
		public float perfectTiming = .05f;
		public float greatTiming = .15f;
		public float goodTiming = .2f;
		public float badTiming = .5f;
		// easy 1.6, normal 1.3, hard 1, expert .8
		public float timingMultiplier = 1;

		[Space]
		public GameObject instantBlockPrefab;
		public GameObject shortBlockPrefab;
		public GameObject longBlockPrefab;
		public RectTransform instantBlockPageRect;
		public RectTransform shortBlockPageRect;
		public RectTransform longBlockPageRect;
		public float maxInstantBlockBeats = 1f / 16;
		public float maxShortBlockBeats = 1f / 4;
		public float endDelayBeats = 2;
		float maxInstantBlockTicks;
		float maxShortBlockTicks;
		float endTicks;

		[Space]
		public int laneCount;
		float[] laneXDict;
		public float blockWidth = 100;
		public float blockJudgingWidth = 120;
		float blockJudgingHalfWidth;

		[Space]
		public ProgressBarPageScheduler progressBar;
		public Text scoreText;
		public Text scoreAdditionText;
		public Text comboText;
		public Text judgmentText;
		public Text accuracyText;
		public float scoreBase = 125;
		public Color perfectColor;
		public Color greatColor;
		public Color goodColor;
		public Color badColor;
		public Color missColor;
		int score;
		int combo;
		float accuracyFactor;
		float perfectAccuracyFactor;
		float accuracy;

		[Space]
		public ParticleBurstEmitter perfectEmitter;
		public ParticleBurstEmitter greatEmitter;
		public ParticleBurstEmitter goodEmitter;
		public ParticleBurstEmitter badEmitter;

		enum Judgment {
			Perfect,
			Great,
			Good,
			Bad,
			Miss,
		}

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

		sealed class Block {
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

		int perfectCount = 0;
		int greatCount = 0;
		int goodCount = 0;
		int badCount = 0;
		int missCount = 0;
		int maxCombo;

		void Start() {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;

			scoreText.text = "";
			scoreAdditionText.text = "";
			comboText.text = "";
			judgmentText.text = "";
			accuracyText.text = "";

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
			maxInstantBlockTicks = maxInstantBlockBeats * midiFile.ticksPerBeat;
			maxShortBlockTicks = maxShortBlockBeats * midiFile.ticksPerBeat;
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

			perfectTiming *= timingMultiplier;
			greatTiming *= timingMultiplier;
			goodTiming *= timingMultiplier;
			badTiming *= timingMultiplier;

			backgroundTracks = new BackgroundTrack[backgroundSequences.Count];
			for (int i = 0; i < backgroundTracks.Length; i++) backgroundTracks[i] = new BackgroundTrack();
			gameTracks = new BackgroundTrack[gameSequences.Count];
			for (int i = 0; i < gameTracks.Length; i++) gameTracks[i] = new BackgroundTrack();

			pausePageGroup.gameObject.SetActive(false);

			midiSequencer.ticks = -cacheTicks;
			ShowReadyAnimation();
//			StartGame();
		}

		void ShowReadyAnimation() {
			readyPageGroup.gameObject.SetActive(true);
			readyPageGroup.alpha = 0;
			anim_.New().FadeIn(readyPageGroup, .5f, 0).Then()
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "3")
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "2")
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "1")
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
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
					game_.perfectCount = perfectCount;
					game_.greatCount = greatCount;
					game_.goodCount = goodCount;
					game_.badCount = badCount;
					game_.missCount = missCount;
					game_.score = score;
					game_.accuracy = accuracy;
					game_.maxComboCount = maxCombo;

					#if UNITY_ANDROID
					Screen.autorotateToLandscapeLeft = true;
					Screen.autorotateToLandscapeRight = true;
					#endif

					UnityEngine.SceneManagement.SceneManager.LoadScene("GameplayResult");
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

			#if UNITY_EDITOR
			ProcessMouse();
			#else
			ProcessTouches();
			#endif

			UpdateBlocks();

			progressBar.SetProgress(ticks / sequenceCollection.end);

			sf2Synth.Panic();
		}

		public void OnPauseButtonClicked() {
			pauseButton.interactable = false;
			isPaused = true;

			pausePageGroup.gameObject.SetActive(true);
			pausePageGroup.alpha = 0;
			anim_.New().FadeIn(pausePageGroup, .5f, 0);
		}

		public void OnRestartButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
		}

		public void OnResumeButtonClicked() {
			anim_.New().FadeOut(pausePageGroup, .5f, 0).Then()
				.Call(() => {
					pausePageGroup.gameObject.SetActive(false);
					pauseButton.interactable = true;
					isPaused = false;
				});
		}

		public void OnStopButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene("SongSelect");
		}

		void OnAudioFilterRead (float[] buffer, int channel) {
			if (sf2Synth != null) sf2Synth.Process(buffer);
		}
	}
}
