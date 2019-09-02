namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public struct MidisProto {
		public string authorName;
		public string authorTag;
		public string pathPrefix;
		public MidiProto[] midiList;
	}

	[System.Serializable]
	public struct MidiProto {
		public int album;
		public int song;
		public string name;

		public string path;
	}
}