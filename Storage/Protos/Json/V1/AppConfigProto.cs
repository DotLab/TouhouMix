namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class AppConfigProto {
		public string displayLang;
		public bool translateUserGeneratedContent;

		public int sampleRateDownscale;
		public int audioBufferUpscale;

		public int networkEndpoint;

		public static AppConfigProto CreateDefault() {
			return new AppConfigProto {
				displayLang = GetDefaultLang(),
				translateUserGeneratedContent = false,

				sampleRateDownscale = 0,
				audioBufferUpscale = 0,
				networkEndpoint = 0,
			};
		}

		public static string GetDefaultLang() {
			var language = UnityEngine.Application.systemLanguage;

			switch (language) {
				case UnityEngine.SystemLanguage.Afrikaans: return "af";
				case UnityEngine.SystemLanguage.Arabic: return "ar";
				case UnityEngine.SystemLanguage.Basque: return "eu";
				case UnityEngine.SystemLanguage.Belarusian: return "by";
				case UnityEngine.SystemLanguage.Bulgarian: return "bg";
				case UnityEngine.SystemLanguage.Catalan: return "ca";
				case UnityEngine.SystemLanguage.ChineseSimplified: return "zh-CN";
				case UnityEngine.SystemLanguage.ChineseTraditional: return "zh-TW";
				case UnityEngine.SystemLanguage.Chinese: return "zh";
				case UnityEngine.SystemLanguage.Czech: return "cs";
				case UnityEngine.SystemLanguage.Danish: return "da";
				case UnityEngine.SystemLanguage.Dutch: return "nl";
				case UnityEngine.SystemLanguage.English: return "en";
				case UnityEngine.SystemLanguage.Estonian: return "et";
				case UnityEngine.SystemLanguage.Faroese: return "fo";
				case UnityEngine.SystemLanguage.Finnish: return "fi";
				case UnityEngine.SystemLanguage.French: return "fr";
				case UnityEngine.SystemLanguage.German: return "de";
				case UnityEngine.SystemLanguage.Greek: return "el";
				case UnityEngine.SystemLanguage.Hebrew: return "iw";
				case UnityEngine.SystemLanguage.Hungarian: return "hu";
				case UnityEngine.SystemLanguage.Icelandic: return "is";
				case UnityEngine.SystemLanguage.Indonesian: return "in";
				case UnityEngine.SystemLanguage.Italian: return "it";
				case UnityEngine.SystemLanguage.Japanese: return "ja";
				case UnityEngine.SystemLanguage.Korean: return "ko";
				case UnityEngine.SystemLanguage.Latvian: return "lv";
				case UnityEngine.SystemLanguage.Lithuanian: return "lt";
				case UnityEngine.SystemLanguage.Norwegian: return "no";
				case UnityEngine.SystemLanguage.Polish: return "pl";
				case UnityEngine.SystemLanguage.Portuguese: return "pt";
				case UnityEngine.SystemLanguage.Romanian: return "ro";
				case UnityEngine.SystemLanguage.Russian: return "ru";
				case UnityEngine.SystemLanguage.SerboCroatian: return "sh";
				case UnityEngine.SystemLanguage.Slovak: return "sk";
				case UnityEngine.SystemLanguage.Slovenian: return "sl";
				case UnityEngine.SystemLanguage.Spanish: return "es";
				case UnityEngine.SystemLanguage.Swedish: return "sv";
				case UnityEngine.SystemLanguage.Thai: return "th";
				case UnityEngine.SystemLanguage.Turkish: return "tr";
				case UnityEngine.SystemLanguage.Ukrainian: return "uk";
				case UnityEngine.SystemLanguage.Unknown: return "en";
				case UnityEngine.SystemLanguage.Vietnamese: return "vi";
			}

			return "en";
		}
	}
}
