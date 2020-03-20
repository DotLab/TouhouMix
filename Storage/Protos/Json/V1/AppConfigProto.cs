namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class AppConfigProto {
		public string displayLang;
		public bool translateAllText;

		public static AppConfigProto CreateDefault() {
			return new AppConfigProto {
				displayLang = "en",
			};
		}
	}
}
