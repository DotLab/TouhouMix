namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class UiStateProto {
		public int selectedAlbum;
		public float albumSelectScrollViewPositionY;
		public int selectedSong;
		public float songSelectScrollViewPositionY;
		public string selectedMidi;

		public static UiStateProto CreateDefault() {
			return new UiStateProto{
				selectedAlbum = -1, 
				albumSelectScrollViewPositionY = 0,
				selectedSong = -1, 
				songSelectScrollViewPositionY = 0,
				selectedMidi = null, 
			};
		}
	}
}
