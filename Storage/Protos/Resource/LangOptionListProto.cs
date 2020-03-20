namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public sealed class LangOptionListProto {
		public LangOptionProto[] langOptionList;
	}

	[System.Serializable]
	public sealed class LangOptionProto {
		public string name;
		public string lang;
		public int index;
	}
}
