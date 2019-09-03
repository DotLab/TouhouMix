namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public sealed class SongsProto {
		public SongProto[] songList;
	}

	[System.Serializable]
	public sealed class SongProto {
		public int album;
		public int song;
		public string name;

		public int midiCount;
	}
}