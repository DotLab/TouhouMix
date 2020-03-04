using UnityEngine;
using TouhouMix.Storage;
using TouhouMix.Net;

namespace TouhouMix.Levels {
	public sealed class GameScheduler : MonoBehaviour {
		public const int WELCOME_LEVEL_BUILD_INDEX = 0;
		public const int SONG_SELECT_LEVEL_BUILD_INDEX = 1;
		public const int GAMEPLAY_LEVEL_BUILD_INDEX = 2;
		public const int GAMEPLAY_RESULT_LEVEL_BUILD_INDEX = 3;

		public static GameScheduler instance;

		public JsonStorage jsonStorage;
		public ResourceStorage resourceStorage;

		public Storage.Protos.Json.V1.UiStateProto uiState;
		public Storage.Protos.Json.V1.MidiSynthConfigsProto midiSynthConfigs;
		public Storage.Protos.Json.V1.GameplayConfigProto gameplayConfig;

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

		public NetManager netManager;

		void Awake () {
			if (instance == null) {
				Init();
				instance = this;
				DontDestroyOnLoad(gameObject);
				ImaginationOverflow.UniversalFileAssociation.FileAssociationManager.Instance.FileActivated += FileActivatedHandler;
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
		}

		public void Init() {
			jsonStorage = new JsonStorage();
			jsonStorage.Load();

			resourceStorage = new ResourceStorage();
			resourceStorage.Load();

			uiState = jsonStorage.Get(JsonStorageKeys.V1.UI_STATE, Storage.Protos.Json.V1.UiStateProto.CreateDefault());
			midiSynthConfigs = jsonStorage.Get(JsonStorageKeys.V1.MIDI_SYNTH_CONFIGS, Storage.Protos.Json.V1.MidiSynthConfigsProto.CreateDefault());
			gameplayConfig = jsonStorage.Get(JsonStorageKeys.V1.GAMEPLAY_CONFIG, Storage.Protos.Json.V1.GameplayConfigProto.CreateDefault());

			netManager = new NetManager();
			//netManager.Init();
		}

		[ContextMenu("RestoreDefaultGameplayConfig")]
		public void RestoreDefaultGameplayConfig() {
			gameplayConfig = Storage.Protos.Json.V1.GameplayConfigProto.CreateDefault();
		}

		public void Save() {
			jsonStorage.Set(JsonStorageKeys.V1.UI_STATE, uiState);
			jsonStorage.Set(JsonStorageKeys.V1.MIDI_SYNTH_CONFIGS, midiSynthConfigs);
			jsonStorage.Set(JsonStorageKeys.V1.GAMEPLAY_CONFIG, gameplayConfig);

			jsonStorage.Flush();
		}

		private void FileActivatedHandler(ImaginationOverflow.UniversalFileAssociation.Data.FileInformation fileInfo) {
			var localPath = System.IO.Path.Combine(Application.persistentDataPath, fileInfo.Name);
			using (var fileStream = System.IO.File.Create(localPath)) {
				fileInfo.Stream.Seek(0, System.IO.SeekOrigin.Begin);
				fileInfo.Stream.CopyTo(fileStream);
			}
			Debug.Log("File " + fileInfo.Name + " written");
			resourceStorage.LoadCustomMidis();
		}
	}
}