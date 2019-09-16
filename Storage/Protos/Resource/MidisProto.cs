namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public sealed class MidisProto {
		public MidiProto[] midiList;
	}

	[System.Serializable]
	public sealed class MidiProto {
		public int author;
		public int album;
		public int song;
		public string name;
		public string path;
		public bool isFile;
	}
}