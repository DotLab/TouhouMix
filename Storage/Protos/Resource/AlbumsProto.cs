﻿namespace TouhouMix.Storage.Protos.Resource {
	[System.Serializable]
	public sealed class AlbumsProto {
		public AlbumProto[] albumList;
	}

	[System.Serializable]
	public sealed class AlbumProto {
		public int album;
		public string tag;
		public string name;

		public int midiCount;
		public int songCount;
	}
}
