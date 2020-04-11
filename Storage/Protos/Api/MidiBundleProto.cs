namespace TouhouMix.Storage.Protos.Api {
	[System.Serializable]
	public sealed class MidiBundleProto {
		public MidiProto[] midis;
		public AlbumProto[] albums;
		public SongProto[] songs;
		public PersonProto[] persons;
		public TranslationProto[] translations;
	}
}