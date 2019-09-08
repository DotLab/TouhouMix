using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TouhouMix.Storage {
	public sealed class JsonStorage {
		[System.Serializable]
		struct JsonStorageProto {
			[System.Serializable]
			public struct KeyValuePairProto {
				public string key;
				public string value;
			}
			public KeyValuePairProto[] pairs;
		}

		const string JSON_STORAGE_FILE_NAME = "JsonStorage";
		readonly string JSON_STORAGE_FILE_PATH = Path.Combine(Application.persistentDataPath, JSON_STORAGE_FILE_NAME);

		readonly Dictionary<string, string> jsonStorage = new Dictionary<string, string>();

		public void Load() {
			if (File.Exists(JSON_STORAGE_FILE_PATH)) {
				try {
					var storageProto = JsonUtility.FromJson<JsonStorageProto>(File.ReadAllText(JSON_STORAGE_FILE_PATH));
					foreach (var pair in storageProto.pairs) {
						jsonStorage[pair.key] = pair.value;
					}
				} catch (System.Exception ex) {
					Debug.LogWarning(ex);
				}
			}
		}

		public void Flush() {
			var pairProtoList = new List<JsonStorageProto.KeyValuePairProto>();
			foreach (var key in jsonStorage.Keys) {
				string value = jsonStorage[key];
				if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) continue;
				Debug.Log(key + ": " + jsonStorage[key]);
				pairProtoList.Add(new JsonStorageProto.KeyValuePairProto{
					key = key,
					value = jsonStorage[key],
				});
			}
			var storageProto = new JsonStorageProto{pairs = pairProtoList.ToArray()};
			File.WriteAllText(JSON_STORAGE_FILE_PATH, JsonUtility.ToJson(storageProto));
		}

		public T Get<T>(string key, T defaultValue) {
			if (jsonStorage.ContainsKey(key)) {
				try {
					return JsonUtility.FromJson<T>(jsonStorage[key]);
				} catch (System.Exception ex) {
					Debug.LogWarning(ex);
					return defaultValue;
				}
			} else {
				return defaultValue;
			}
		}

		public T Get<T>(string key) {
			return Get(key, default(T));
		}

		public void Set<T>(string key, T value) {
			jsonStorage[key] = JsonUtility.ToJson(value);
		}

		public void Remove(string key) {
			jsonStorage[key] = null;
		}
	}
}