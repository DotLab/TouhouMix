using System.Collections.Generic;

namespace TouhouMix.Storage.Protos.Json.V1 {
	public sealed class MidiSynthConfigsProto {
		public Dictionary<string, MidiSynthConfigProto> synthConfigDict;

		public static MidiSynthConfigsProto CreateDefault() {
			return new MidiSynthConfigsProto{
				synthConfigDict = new Dictionary<string, MidiSynthConfigProto>(),
			};
		}
	}

	public sealed class MidiSynthConfigProto {
		public sealed class SequenceConfigProto {
			public int sequenceIndex;

			public int track;
			public int trackGroup;
			public int channel;
			public int channelGroup;
			public int program;

			public bool shouldUseInGame;

			public bool isMuted;
		}

		public int trackGroupCount;
		public int channelGroupCount;

		public int soloSequenceIndex;

		public List<SequenceConfigProto> sequenceStateList;
	}
}
