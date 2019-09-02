namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public struct SongsProto {
		public SongProto[] songList;
	}

	[System.Serializable]
	public struct SongProto {
		public int album;
		public int song;
		public string name;

		public int midiCount;
	}
}