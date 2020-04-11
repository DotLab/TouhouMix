namespace TouhouMix.Storage.Protos.Api {
	[System.Serializable]
	public sealed class MidiProto {
		public string _id;
		public string uploaderId;
		public string uploaderName;
		public string uploaderAvatarUrl;
		
		public string name;
		public string desc;
		public string hash;

		public string coverUrl;
		public string coverBlurUrl;

		public string artistName;
		public string artistUrl;
		
		public string songId;
		public SongProto song;
		public AlbumProto album;
		public PersonProto author;
		public PersonProto composer;
		public string uploadedDate;
		public string approvedDate;
		public string status;
		
		public string sourceArtistName;
		public string sourceAlbumName;
		public string sourceSongName;
		
		public int trialCount;
		public int loveCount;
		public int voteCount;
		public int voteSum;
		public int upCount;
		public int downCount;
		
		public float avgScore;
		public float avgCombo;
		public float avgAccuracy;
		
		public int passCount;
		public int failCount;

		public int sCount;
		public int aCount;
		public int bCount;
		public int cCount;
		public int dCount;
		public int fCount;
	}
}