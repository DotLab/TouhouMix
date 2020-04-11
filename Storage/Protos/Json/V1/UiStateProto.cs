namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class UiStateProto {
		public float albumSelectScrollViewPositionY;
		public string selectedAlbumId;
		public float songSelectScrollViewPositionY;
		public string selectedSongId;
		public float midiSelectScrollViewPositionY;
		public string selectedMidiId;

		public static UiStateProto CreateDefault() {
			return new UiStateProto{
				albumSelectScrollViewPositionY = 0,
				selectedAlbumId = null, 
				songSelectScrollViewPositionY = 0,
				selectedSongId = null, 
				midiSelectScrollViewPositionY = 0,
				selectedMidiId = null, 
			};
		}
	}
}
