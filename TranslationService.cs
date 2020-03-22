using System.Collections.Generic;
using Systemf;
using TranslateCallback = System.Action<string>;
using System.Linq;
using Jsonf;

namespace TouhouMix.Net {
	public sealed class TranslationService {
		const string FILE_PATH = "Translations.json";

		public string lang = "en";
		public readonly Dictionary<Tuple<string, string>, string> dict = new Dictionary<Tuple<string, string>, string>();

		public NetManager net;

		public string filePath;
		public JsonContext json = new JsonContext();

		public void Init(NetManager net) {
			this.net = net;

			filePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, FILE_PATH);
		}

		public void Translate(string src, TranslateCallback callback) {
			//UnityEngine.Debug.Log(src + " to " + lang);
			if (lang == "en") {
				callback(src);
				return;
			}

			string text;
			var key = Tuple.Create(src, lang);
			if (dict.TryGetValue(key, out text)) {
				callback(text);
			} else {
				net.ClAppTranslate(src, lang, (err, data) => {
					if (!string.IsNullOrEmpty(err)) {
						UnityEngine.Debug.LogError("Translation: " + err);
						callback(src);
						return;
					}
					var res = (string)data;
					dict.Add(key, res);
					callback(res);
				});
			}
		}

		public sealed class Translation {
			public string src;
			public string lang;
			public string text;
		}

		public void Flush() {
			var dictList = dict
				.Select(pair => new Translation {src = pair.Key.item1, lang = pair.Key.item2, text = pair.Value})
				.ToList();
			System.IO.File.WriteAllText(filePath, json.Stringify(dictList));
		}

		public void Load() {
			if (System.IO.File.Exists(filePath)) {
				var jsonText = System.IO.File.ReadAllText(filePath);
				var dictList = json.Parse<List<Translation>>(jsonText);
				dictList.ForEach(x => dict[Tuple.Create(x.src, x.lang)] = x.text);
			}
		}
	}
}