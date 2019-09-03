using UnityEngine;
using TouhouMix.Storage;

namespace TouhouMix.Levels {
	public sealed class GameScheduler : MonoBehaviour {
		public static GameScheduler instance;

		public JsonStorage jsonStorage;
		public ResourceStorage resourceStorage;

		public TouhouMix.Storage.Protos.Json.V1.UiStateProto uiStateProto;

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

			uiStateProto = jsonStorage.Get(JsonStorageKeys.V1.UI_STATUS, TouhouMix.Storage.Protos.Json.V1.UiStateProto.CreateDefault());
		}

		public void Save() {
			jsonStorage.Set(JsonStorageKeys.V1.UI_STATUS, uiStateProto);

			jsonStorage.Flush();
		}
	}
}