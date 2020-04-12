using System.Collections.Generic;
using Systemf;
using TranslateCallback = System.Action<string>;
using System.Linq;
using Jsonf;

namespace TouhouMix.Net {
	public static class TranslationServiceExtension {
		public static string TranslateVolatile(this string self) {
			var game = Levels.GameScheduler.instance;
			//UnityEngine.Debug.Log("translate " + self);
			if (game.appConfig.displayLang != "en") {
				return game.translationSevice.Translate(self, TranslationService.UI_VOLATILE);
			} else {
				return self;
			}
		}

		public static string TranslateArtifact(this string self) {
			var game = Levels.GameScheduler.instance;
				//UnityEngine.Debug.Log("translate " + self);
			if (game.appConfig.translateUserGeneratedContent) {
				return game.translationSevice.Translate(self, TranslationService.NAME_ARTIFACT);
			} else {
				return self;
			}
		}

		public static string Translate(this string self, string ns) {
			var game = Levels.GameScheduler.instance;
			if (ns != TranslationService.UI_APP && game.appConfig.translateUserGeneratedContent) {
				return game.translationSevice.Translate(self, ns);
			} else {
				return self;
			}
		}

		public static string Translate(this string self) {
			return Levels.GameScheduler.instance.translationSevice.Translate(self);
		}
	}

	public sealed class TranslationService {
		public const string UI_APP = "ui.app";
		public const string NAME_ARTIFACT = "name.artifact";
		public const string UI_VOLATILE = "ui.volatile";

		const string FILE_PATH = "Translations.json";

		public string lang = "en";
		public readonly Dictionary<Tuple<string, string, string>, string> dict = new Dictionary<Tuple<string, string, string>, string>();

		public NetManager net;

		public string filePath;
		public JsonContext json = new JsonContext();

		public void Init(NetManager net) {
			this.net = net;

			filePath = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, FILE_PATH);
		}

		public void Translate(string src, TranslateCallback callback) {
			Translate(src, UI_APP, callback);
		}

		public void Translate(string src, string ns, TranslateCallback callback) {
			//UnityEngine.Debug.Log(src + " to " + lang);
			if (lang == "en" && ns == UI_APP) {
				callback(src);
				return;
			}

			string text;
			var key = Tuple.Create(src, lang, ns);
			if (dict.TryGetValue(key, out text)) {
				callback(text);
			} else {
				net.ClAppTranslate(src, lang, ns, (err, data) => {
					if (!string.IsNullOrEmpty(err)) {
						UnityEngine.Debug.LogError("Translation: " + err);
						callback(src);
						return;
					}
					var res = (string)data;
					// Possible duplicates, fix later
					dict[key] = res;
					callback(res);
				});
			}
		}

		public string Translate(string src) {
			return Translate(src, UI_APP);
		}

		public string Translate(string src, string ns) {
			if (lang == "en" && ns == UI_APP) {
				return src;
			}
			var key = Tuple.Create(src, lang, ns);
			if (dict.TryGetValue(key, out string text)) {
				return text;
			} else {
				net.ClAppTranslate(src, lang, ns, (err, data) => {
					if (!string.IsNullOrEmpty(err)) {
						UnityEngine.Debug.LogError("Translation: " + err);
					}
					var res = (string)data;
					// Possible duplicates, fix later
					dict[key] = res;
				});
				return src;
			}
		}

		public void Set(string src, string lang, string ns, string text) {
			dict[Tuple.Create(src, lang, ns)] = text;
		}

		public void Flush() {
			var dictList = dict
				.Select(pair => new Storage.Protos.Api.TranslationProto {src = pair.Key.item1, lang = pair.Key.item2, ns = pair.Key.item3, text = pair.Value})
				.ToList();
			System.IO.File.WriteAllText(filePath, json.Stringify(dictList));
			UnityEngine.Debug.Log("Translation flushed");
		}

		public void Load() {
			if (System.IO.File.Exists(filePath)) {
				var jsonText = System.IO.File.ReadAllText(filePath);
				var dictList = json.Parse<List<Storage.Protos.Api.TranslationProto>>(jsonText);
				dictList.ForEach(x => dict[Tuple.Create(x.src, x.lang, x.ns)] = x.text);
			}
			UnityEngine.Debug.Log("Translation loaded");
		}
	}
}