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
		void GenerateGameNotes() {
			for (int i = 0; i < gameSequences.Count; i++) {
				var seq = gameSequences[i];
				var track = gameTracks[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks + cacheTicks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];
					// start game block
					gameplayManager.AddTentativeNote(seqNote);
				}
			}

			gameplayManager.GenerateBlocks();
		}
	}
}
