using UnityEngine;
using TouhouMix.Storage;

namespace TouhouMix.Levels {
	public sealed class GameScheduler : MonoBehaviour {
		public static GameScheduler instance;

		public JsonStorage jsonStorage;
		public ResourceStorage resourceStorage;

		public TouhouMix.Storage.Protos.Json.V1.UiStateProto uiState;
		public TouhouMix.Storage.Protos.Json.V1.MidiSynthConfigsProto midiSynthConfigs;

		void Awake () {
			if (instance == null) {
				Init();
				instance = this;
				DontDestroyOnLoad(gameObject);
			} else {
				Destroy(gameObject);
			}
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

			uiState = jsonStorage.Get(JsonStorageKeys.V1.UI_STATE, TouhouMix.Storage.Protos.Json.V1.UiStateProto.CreateDefault());
			midiSynthConfigs = jsonStorage.Get(JsonStorageKeys.V1.MIDI_SYNTH_CONFIGS, TouhouMix.Storage.Protos.Json.V1.MidiSynthConfigsProto.CreateDefault());
		}

		public void Save() {
			jsonStorage.Set(JsonStorageKeys.V1.UI_STATE, uiState);
			jsonStorage.Set(JsonStorageKeys.V1.MIDI_SYNTH_CONFIGS, midiSynthConfigs);

			jsonStorage.Flush();
		}
	}
}