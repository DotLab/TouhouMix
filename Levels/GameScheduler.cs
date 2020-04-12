using UnityEngine;
using System.Collections.Generic;
using TouhouMix.Storage;
using TouhouMix.Net;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using Sirenix.OdinInspector;

public partial class SROptions {
	public int rtt {
		get { return TouhouMix.Levels.GameScheduler.instance.netManager.rtt; }
	}
}

namespace TouhouMix.Levels {
	public sealed class GameScheduler : MonoBehaviour {
		public const int WELCOME_LEVEL_BUILD_INDEX = 0;
		public const int SONG_SELECT_LEVEL_BUILD_INDEX = 1;
		public const int GAMEPLAY_LEVEL_BUILD_INDEX = 2;
		public const int GAMEPLAY_RESULT_LEVEL_BUILD_INDEX = 3;

		public static GameScheduler instance;

		public JsonStorage jsonStorage;
		public ResourceStorage resourceStorage;
		public NetManager netManager;
		public LocalDb localDb;
		public TranslationService translationSevice;

		public Storage.Protos.Json.V1.UiStateProto uiState;
		public Storage.Protos.Json.V1.MidiSynthConfigsProto midiSynthConfigs;
		public Storage.Protos.Json.V1.GameplayConfigProto gameplayConfig;
		public Storage.Protos.Json.V1.AppConfigProto appConfig;

		public Texture2D backgroundTexture;

		public string midiId;
		public Midif.V3.MidiFile midiFile;
		public Midif.V3.NoteSequenceCollection noteSequenceCollection;

		public string title;
		public string subtitle;

		public int perfectCount;
		public int greatCount;
		public int goodCount;
		public int badCount;
		public int missCount;
		public int maxComboCount;

		public int score;
		public float accuracy;

		public string username;
		public string password;
		public JsonObj userObj;

		readonly List<System.Action> actionQueue = new List<System.Action>();

		#region LifeHook
		void Awake () {
			if (instance == null) {
				instance = this;
				DontDestroyOnLoad(gameObject);
				ImaginationOverflow.UniversalFileAssociation.FileAssociationManager.Instance.FileActivated += FileActivatedHandler;

				Init();
			} else {
				Destroy(gameObject);
			}
		}

		void OnDisable() {
			ImaginationOverflow.UniversalFileAssociation.FileAssociationManager.Instance.FileActivated -= FileActivatedHandler;
		}

		void OnApplicationFocus(bool hasFocus) {
			if (!hasFocus) Save();
		}

		void OnApplicationPause() {
			Save();
		}

		void OnApplicationQuit() {
			Save();

			netManager.Dispose();
		}
		#endregion

		[Button]
		public void TakeScreenshot() {
			ScreenCapture.CaptureScreenshot("screenshot.png");
		}

		[Button]
		public void StartTakingScreenshots() {
			StartCoroutine(ScreenshotHandler());
		}

		[Button]
		public void StopTakingScreenshots() {
			StopAllCoroutines();
		}

		System.Collections.IEnumerator ScreenshotHandler() {
			int i = 0;
			while (true) {
				ScreenCapture.CaptureScreenshot("screenshot-" + i + ".png");
				yield return new WaitForSeconds(1);
				i += 1;
			}
		}

		public void Init() {
			ResourceStorage.DecompressMidiBundle();

			jsonStorage = new JsonStorage();
			jsonStorage.Load();

			uiState = jsonStorage.Get(JsonStorageKeys.V1.UI_STATE, Storage.Protos.Json.V1.UiStateProto.CreateDefault());
			midiSynthConfigs = jsonStorage.Get(JsonStorageKeys.V1.MIDI_SYNTH_CONFIGS, Storage.Protos.Json.V1.MidiSynthConfigsProto.CreateDefault());
			gameplayConfig = jsonStorage.Get(JsonStorageKeys.V1.GAMEPLAY_CONFIG, Storage.Protos.Json.V1.GameplayConfigProto.CreateDefault());
			appConfig = jsonStorage.Get(JsonStorageKeys.V1.APP_CONFIG, Storage.Protos.Json.V1.AppConfigProto.CreateDefault());

			localDb = new LocalDb();
			localDb.Init();

			username = PlayerPrefs.GetString("TEMP_USERNAME", null);
			password = PlayerPrefs.GetString("TEMP_PASSWORD", null);

			netManager = new NetManager();
			netManager.Init(this);

			translationSevice = new TranslationService();
			translationSevice.Init(netManager);
			translationSevice.Load();
			translationSevice.lang = appConfig.displayLang;

			resourceStorage = new ResourceStorage();
			resourceStorage.Init(this);

			InitAudioConfig();
			ApplyAppAudioConfig();
		}

		[ContextMenu("RestoreDefaultGameplayConfig")]
		public void RestoreDefaultGameplayConfig() {
			gameplayConfig = Storage.Protos.Json.V1.GameplayConfigProto.CreateDefault();
		}

		public void Save() {
			Debug.Log("Save");

			jsonStorage.Set(JsonStorageKeys.V1.UI_STATE, uiState);
			jsonStorage.Set(JsonStorageKeys.V1.MIDI_SYNTH_CONFIGS, midiSynthConfigs);
			jsonStorage.Set(JsonStorageKeys.V1.GAMEPLAY_CONFIG, gameplayConfig);
			jsonStorage.Set(JsonStorageKeys.V1.APP_CONFIG, appConfig);
			jsonStorage.Flush();

			translationSevice.Flush();

			PlayerPrefs.SetString("TEMP_USERNAME", username);
			PlayerPrefs.SetString("TEMP_PASSWORD", password);
		}

		private void FileActivatedHandler(ImaginationOverflow.UniversalFileAssociation.Data.FileInformation fileInfo) {
			var localPath = System.IO.Path.Combine(Application.persistentDataPath, fileInfo.Name);
			using (var fileStream = System.IO.File.Create(localPath)) {
				fileInfo.Stream.Seek(0, System.IO.SeekOrigin.Begin);
				fileInfo.Stream.CopyTo(fileStream);
			}
			Debug.Log("File " + fileInfo.Name + " written");
			resourceStorage.LoadMidis();
		}

		public void ExecuteOnMain(System.Action action) {
			lock(actionQueue) {
				actionQueue.Add(action);
			}
		}

		public void SetDisplayLanguageByIndex(int index) {
			string lang = resourceStorage.langOptionDictByIndex[index].lang;
			appConfig.displayLang = lang;
			translationSevice.lang = lang;
		}

		public int GetDisplayLanguageIndex() {
			return resourceStorage.langOptionDictByLang[appConfig.displayLang].index;
		}

		AudioConfiguration initialAudioConfig;

		public void InitAudioConfig() {
			initialAudioConfig = AudioSettings.GetConfiguration();
			AudioSettings.Reset(new AudioConfiguration {
				speakerMode = AudioSpeakerMode.Stereo,
				dspBufferSize = initialAudioConfig.dspBufferSize,
				sampleRate = 96000,  // As high as possible
				numRealVoices = 0,
				numVirtualVoices = 0,
			});
			initialAudioConfig = AudioSettings.GetConfiguration();
			Debug.Log("Init " + initialAudioConfig.dspBufferSize + " " + initialAudioConfig.sampleRate);
		}

		public void ApplyAppAudioConfig() {
			Debug.Log("Apply " + appConfig.audioBufferUpscale + " " + appConfig.sampleRateDownscale);
			AudioSettings.Reset(new AudioConfiguration {
				speakerMode = AudioSpeakerMode.Stereo,
				dspBufferSize = initialAudioConfig.dspBufferSize << appConfig.audioBufferUpscale,
				sampleRate = initialAudioConfig.sampleRate >> appConfig.sampleRateDownscale,
				numRealVoices = initialAudioConfig.numRealVoices,
				numVirtualVoices = initialAudioConfig.numVirtualVoices,
			});
			var audioConfig = AudioSettings.GetConfiguration();
			Debug.Log("Get " + audioConfig.dspBufferSize + " " + audioConfig.sampleRate);
		}

		private void Update() {
			lock (actionQueue) {
				int count = actionQueue.Count;
				for (int i = 0; i < count; i++) {
#if UNITY_EDITOR
					actionQueue[i].Invoke();
#else
					try {
						actionQueue[i].Invoke();
					} catch(System.Exception ex) {
						Debug.LogError(ex);
					}
#endif
			}

				actionQueue.RemoveRange(0, count);
			}
		}
	}
}