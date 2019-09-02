namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public struct AlbumsProto {
		public AlbumProto[] albumList;
	}

	[System.Serializable]
	public struct AlbumProto {
		public int album;
		public string tag;
		public string name;

		public int midiCount;
		public int songCount;
	}
}
