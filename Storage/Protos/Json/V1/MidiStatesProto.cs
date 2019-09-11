namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class MidiStatesProto {
		public MidiStateProto[] midiStateList;

		public static MidiStatesProto CreateDefault() {
			return new MidiStatesProto{
				midiStateList = new MidiStateProto[0],
			};
		}
	}

	[System.Serializable]
	public sealed class MidiStateProto {
		public string sha256Hash;
		
	}
}
