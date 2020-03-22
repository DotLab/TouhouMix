using UnityEngine;
using Midif.V3;

namespace TouhouMix.Levels.Gameplay {
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		public void StartNote(NoteSequenceCollection.Note seqNote) {
			sf2Synth.NoteOn(seqNote.channel, seqNote.note, seqNote.velocity);
		}

		public void StopNote(NoteSequenceCollection.Note seqNote) {
			sf2Synth.NoteOff(seqNote.channel, seqNote.note, 0);
		}

		public void AddBackgroundNotes(GameplayBlock block) {
			AddBackgroundNote(block.note);
			foreach (var note in block.backgroundNotes) {
				AddBackgroundNote(note);
			}
		}

		void AddBackgroundNote(NoteSequenceCollection.Note seqNote) {
			// start background note
			sf2Synth.NoteOn(seqNote.channel, seqNote.note, seqNote.velocity);
			if (seqNote.end <= ticks) {
				// already overdue
				sf2Synth.NoteOff(seqNote.channel, seqNote.note, 0);
			} else {
				if (backgroundNoteFreeStartIndex < backgroundNotes.Count) {
					backgroundNotes[backgroundNoteFreeStartIndex] = seqNote;
				} else {
					backgroundNotes.Add(seqNote);
				}
				backgroundNoteFreeStartIndex += 1;
			}
		}

		void UpdateBackgroundNotes() {
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
					AddBackgroundNote(seq.notes[track.seqNoteIndex]);
				}
			}
		}
	}
}
