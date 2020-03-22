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

		public static MidiSynthConfigProto LoadOrCreateDefault(TouhouMix.Levels.GameScheduler game, Midif.V3.NoteSequenceCollection collection, string sha256Hash) {
			MidiSynthConfigProto state;

			try {
				state = game.midiSynthConfigs.synthConfigDict[sha256Hash];
				Systemf.Assert.Equal(collection.sequences.Count, state.sequenceStateList.Count);
			} catch (System.Exception e) {
				UnityEngine.Debug.LogWarning("Cannot find synth config\n" + e);

				state = new MidiSynthConfigProto{
					trackGroupCount = collection.trackGroups.Length,
					channelGroupCount = collection.channelGroups.Length,
					soloSequenceIndex = -1,
					sequenceStateList = new List<MidiSynthConfigProto.SequenceConfigProto>(),
				};

				for (int i = 0; i < collection.sequences.Count; i++) {
					var seq = collection.sequences[i];
					state.sequenceStateList.Add(new MidiSynthConfigProto.SequenceConfigProto{
						sequenceIndex = i,
						track = seq.track,
						trackGroup = seq.trackGroup,
						channel = seq.channel,
						channelGroup = seq.channelGroup,
						program = seq.program,
						shouldUseInGame = i == 0,
						isMuted = false,
					});
				}
			}

			return state;
		}
	}
}
