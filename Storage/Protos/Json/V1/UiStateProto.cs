namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class UiStateProto {
		public float albumSelectScrollViewPositionY;
		public int selectedAlbum;
		public float songSelectScrollViewPositionY;
		public int selectedSong;
		public float midiSelectScrollViewPositionY;
		public string selectedMidi;

		public static UiStateProto CreateDefault() {
			return new UiStateProto{
				albumSelectScrollViewPositionY = 0,
				selectedAlbum = -1, 
				songSelectScrollViewPositionY = 0,
				selectedSong = -1, 
				midiSelectScrollViewPositionY = 0,
				selectedMidi = null, 
			};
		}
	}
}
