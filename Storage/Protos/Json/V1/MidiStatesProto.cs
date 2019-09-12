using System.Collections.Generic;

namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class MidiStatesProto {
		public MidiConfigProto[] midiStateList;

		public static MidiStatesProto CreateDefault() {
			return new MidiStatesProto{
				midiStateList = new MidiConfigProto[0],
			};
		}
	}

	[System.Serializable]
	public sealed class MidiConfigProto {
		public string sha256Hash;
		public MidiSequenceConfigProto[] sequenceConfigList;
	}

	public sealed class MidiSequenceConfigProto {
		public bool isMuted;

	}
}
