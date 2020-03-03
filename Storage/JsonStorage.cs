using System.Collections.Generic;
using System.IO;
using Jsonf;

namespace TouhouMix.Storage {
	public sealed class JsonStorage {
		const string JSON_STORAGE_FILE_NAME = "JsonStorage";
		readonly string JSON_STORAGE_FILE_PATH = Path.Combine(UnityEngine.Application.persistentDataPath, JSON_STORAGE_FILE_NAME);

		readonly JsonContext json = new JsonContext();
		readonly Dictionary<string, string> jsonStorage = new Dictionary<string, string>();

		public void Load() {
			if (File.Exists(JSON_STORAGE_FILE_PATH)) {
				try {
					var storageDict = json.Parse<Dictionary<string, string>>(File.ReadAllText(JSON_STORAGE_FILE_PATH));
					foreach (var pair in storageDict) {
						jsonStorage[pair.Key] = pair.Value;
					}
				} catch (System.Exception e) {
					UnityEngine.Debug.LogError(e);
				}
			}
		}

		public void Flush() {
			#if THMIX_DEBUG_LOG
			var sb = new System.Text.StringBuilder();
			foreach (var pair in jsonStorage) {
				sb.Append(pair.Key);
				sb.Append(": ");
				sb.Append(pair.Value);
				sb.AppendLine();
			}
			//UnityEngine.Debug.Log(sb.ToString());
			#endif
			File.WriteAllText(JSON_STORAGE_FILE_PATH, json.Stringify(jsonStorage));
		}

		public T Get<T>(string key, T defaultValue) {
			if (jsonStorage.ContainsKey(key)) {
				try {
					return json.Parse<T>(jsonStorage[key]);
				} catch (System.Exception ex) {
					UnityEngine.Debug.LogError(ex);
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
			jsonStorage[key] = json.Stringify(value);
		}

		public void Remove(string key) {
			jsonStorage[key] = null;
		}
	}
}