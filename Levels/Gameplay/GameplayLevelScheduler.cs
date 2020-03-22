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
	public sealed partial class GameplayLevelScheduler : MonoBehaviour, IGameplayHost {
		const int MOUSE_TOUCH_ID = -100;

		public CanvasSizeWatcher sizeWatcher;
		public bool shouldLoadGameplayConfig;

		[Space]
		public CanvasGroup readyPageGroup;
		public CanvasGroup configPageGroup;
		public Text readyPageText;
		public CanvasGroup gameplayPageGroup;
		public Button pauseButton;
		public CanvasGroup pausePageGroup;

		[Space]
		public float cacheBeats = 2;
		public float endDelayBeats = 2;
		float cacheTicks;
		float endTicks;

		[Space]
		public ScoringManager scoringManager;

		[Space]
		public OneOnlyGameplayManager oneOnlyGameplayManager;
		IGameplayManager gameplayManager;

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

		sealed class BackgroundTrack {
			public int seqNoteIndex;
		}
		int backgroundNoteFreeStartIndex;
		readonly List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();
		BackgroundTrack[] backgroundTracks;
		BackgroundTrack[] gameTracks;

		public void Start() {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;

			gameplayManager = oneOnlyGameplayManager;

			if (shouldLoadGameplayConfig) {
				LoadGameplayConfig();
			}

			hasStarted = false;

			sf2File = new Sf2File(Resources.Load<TextAsset>("sf2/GeneralUser GS v1.471").bytes);

			var audioConfig = AudioSettings.GetConfiguration();
			sampleRate = audioConfig.sampleRate;
			sf2Synth = new Sf2Synth(sf2File, new Sf2Synth.Table(sampleRate), 64);
			sf2Synth.SetVolume(-10);

			midiFile = game_.midiFile ?? new MidiFile(Resources.Load<TextAsset>("test").bytes);
			sequenceCollection = game_.noteSequenceCollection ?? new NoteSequenceCollection(midiFile);

			midiFileSha256Hash = MiscHelper.GetBase64EncodedSha256Hash(midiFile.bytes);

			cacheTicks = cacheBeats * midiFile.ticksPerBeat;
			endTicks = sequenceCollection.end + endDelayBeats * midiFile.ticksPerBeat;

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
			gameplayManager.Init(this);

			ShowReadyAnimation();
		}

		void LoadGameplayConfig() {
			var config = game_.gameplayConfig;
			try {
				var instantBlockPrefab = LoadBlockPreset(config.instantBlockPreset);
				var shortBlockPrefab = LoadBlockPreset(config.shortBlockPreset);
				var longBlockPrefab = LoadBlockPreset(config.longBlockPreset);
				oneOnlyGameplayManager.instantBlockPrefab = instantBlockPrefab ? instantBlockPrefab : oneOnlyGameplayManager.instantBlockPrefab;
				oneOnlyGameplayManager.shortBlockPrefab = shortBlockPrefab ? shortBlockPrefab : oneOnlyGameplayManager.shortBlockPrefab;
				oneOnlyGameplayManager.longBlockPrefab = longBlockPrefab ? longBlockPrefab : oneOnlyGameplayManager.longBlockPrefab;

				oneOnlyGameplayManager.laneCount = config.laneCount;
				oneOnlyGameplayManager.blockWidth = config.blockSize;
				oneOnlyGameplayManager.blockJudgingWidth = config.blockJudgingWidth;

				oneOnlyGameplayManager.judgeHeight = config.judgeLinePosition;
				oneOnlyGameplayManager.judgeThickness = config.judgeLineThickness;

				cacheBeats = config.cacheTime;
				oneOnlyGameplayManager.cacheBeats = config.cacheTime;
				oneOnlyGameplayManager.cacheEsType = config.cacheEasingType;
				oneOnlyGameplayManager.graceBeats = config.graceTime;
				oneOnlyGameplayManager.graceEsType = config.graceEasingType;

				oneOnlyGameplayManager.maxInstantBlockSeconds = config.instantBlockMaxTime;
				oneOnlyGameplayManager.maxShortBlockSeconds = config.shortBlockMaxTime;

				scoringManager.LoadGameplayConfig(config);
			} catch (System.Exception e) {
				Debug.LogError(e);
			}
		}

		GameObject LoadBlockPreset(string path) {
			return Resources.Load<GameObject>("Blocks/" + path);
		}

		void ShowReadyAnimation() {
			readyPageGroup.gameObject.SetActive(true);
			readyPageGroup.alpha = 0;
			anim_.New(this).FadeIn(readyPageGroup, .5f, 0).Then()
				.RotateFromTo(readyPageText.transform, -180, 0, .8f, EsType.BackOut)
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "3")
				.RotateFromTo(readyPageText.transform, 180, 0, .8f, EsType.BackOut)
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "2")
				.RotateFromTo(readyPageText.transform, -180, 0, .8f, EsType.BackOut)
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "1")
				.RotateFromTo(readyPageText.transform, 180, 0, .8f, EsType.BackOut)
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "GO")
				.Set(readyPageText.GetAlphaFloatSettable(), 1)
				.ScaleTo(readyPageText.transform, new Vector3(2, 2, 1), 1, EsType.CubicIn)
				.FadeOutFromOne(readyPageGroup, 1, EsType.QuadIn).Then()
				.Call(StartGame);
		}

		public void OnConfigButtonClicked() {
			anim_.Clear(this);
			configPageGroup.gameObject.SetActive(true);
			anim_.New(this).FadeOut(readyPageGroup, .5f, 0)
				.FadeIn(configPageGroup, .5f, 0);
		}

		public void OnConfigPageBackButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
		}

		public void OnConfigPageUndoButtonClicked() {
			game_.RestoreDefaultGameplayConfig();
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
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

		public Vector2 GetCanvasSize() {
			return sizeWatcher.canvasSize;
		}

		public float GetBeatsPerSecond() {
			return midiSequencer.beatsPerSecond;
		}

		public ScoringManager GetScoringManager() {
			return scoringManager;
		}

		public MidiSequencer GetMidiSequencer() {
			return midiSequencer;
		}

		public MidiFile GetMidiFile() {
			return midiFile;
		}
	}
}
