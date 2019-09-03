namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public sealed class AuthorsProto {
		public AuthorProto[] authorList;
	}

	[System.Serializable]
	public sealed class AuthorProto {
		public int author;
		public string tag;
		public string name;

		public int midiCount;
	}
}
