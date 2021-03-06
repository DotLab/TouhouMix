﻿using UnityEngine;
using Midif.V3;

namespace TouhouMix.Levels.Gameplay {
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		public void StartNote(NoteSequenceCollection.Note seqNote) {
			sf2Synth.NoteOn(seqNote.channel, seqNote.note, seqNote.velocity);
		}

		public void StopNote(NoteSequenceCollection.Note seqNote) {
			sf2Synth.NoteOff(seqNote.channel, seqNote.note, 0);
		}

		public void PlayBackgroundNote(NoteSequenceCollection.Note seqNote) {
			//Debug.LogFormat("on {0} @ {1} ch {2}", seqNote.note, seqNote.velocity, seqNote.channel);
			// start background note
			sf2Synth.NoteOn(seqNote.channel, seqNote.note, seqNote.velocity);
			if (seqNote.end <= ticks) {
				// already overdue
				//Debug.LogFormat("  overdue");
				sf2Synth.NoteOff(seqNote.channel, seqNote.note, 0);
			} else {
				activeBackgroundNoteSet.AddItem(seqNote);
			}
		}

		public void AddBackgroundNote(NoteSequenceCollection.Note seqNote) {
			pendingBackgroundNoteSet.AddItem(seqNote);
		}

		void UpdateBackgroundNotes() {
			// Play pending game background notes
			for (int i = 0; i < pendingBackgroundNoteSet.firstFreeItemIndex; i++) {
				var note = pendingBackgroundNoteSet.itemList[i];
				if (note.start <= ticks) {
					// end background note
					PlayBackgroundNote(note);
					pendingBackgroundNoteSet.FreeItemAt(i);
					i -= 1;
				}
			}

			// End notes
			for (int i = 0; i < activeBackgroundNoteSet.firstFreeItemIndex; i++) {
				var note = activeBackgroundNoteSet.itemList[i];
				if (note.end <= ticks) {
					// end background note
					//Debug.LogFormat("off {0} ch {1}", note.note, note.channel);
					sf2Synth.NoteOff(note.channel, note.note, 0);
					activeBackgroundNoteSet.FreeItemAt(i);
					i -= 1;
				}
			}

			// Play background track notes
			for (int i = 0; i < backgroundSequences.Count; i++) {
				var seq = backgroundSequences[i];
				var track = backgroundTracks[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks; track.seqNoteIndex++) {
					PlayBackgroundNote(seq.notes[track.seqNoteIndex]);
				}
			}
		}
	}
}
