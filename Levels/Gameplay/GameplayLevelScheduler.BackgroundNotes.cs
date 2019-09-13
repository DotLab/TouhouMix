using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using Midif.V3;
using Systemf;
using Uif.Extensions;
using Uif.Settables.Components;
using TouhouMix.Storage.Protos.Json.V1;

namespace TouhouMix.Levels.Gameplay {
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		void UpdateBackgroundNotes(float ticks) {
			// end note before starting new note
			for (int i = 0; i < backgroundNoteFreeStartIndex; i++) {
				var note = backgroundNotes[i];
				if (note.end <= ticks) {
					// end background note
					sf2Synth.NoteOff(note.channel, note.note, 0);
					backgroundNoteFreeStartIndex -= 1;
					backgroundNotes[i] = backgroundNotes[backgroundNoteFreeStartIndex];
					backgroundNotes[backgroundNoteFreeStartIndex] = note;
					i -= 1;
				}
			}

			for (int i = 0; i < backgroundSequences.Count; i++) {
				var seq = backgroundSequences[i];
				var track = backgroundTracks[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];
					// start background note
					sf2Synth.NoteOn(seq.channel, seqNote.note, seqNote.velocity);
					if (seqNote.end <= ticks) {
						// already overdue
						sf2Synth.NoteOn(seq.channel, seqNote.note, seqNote.velocity);
					} else {
						if (backgroundNoteFreeStartIndex < backgroundNotes.Count) {
							backgroundNotes[backgroundNoteFreeStartIndex] = seqNote;
						} else {
							backgroundNotes.Add(seqNote);
						}
						backgroundNoteFreeStartIndex += 1;
					}
				}
			}
		}
	}
}
