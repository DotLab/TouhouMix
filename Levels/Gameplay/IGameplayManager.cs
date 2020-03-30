using UnityEngine;
using Midif.V3;

namespace TouhouMix.Levels.Gameplay {
	public interface IGameplayManager {
		void Init(IGameplayHost host);

		void AddTentativeNote(NoteSequenceCollection.Note note);
		void GenerateBlocks();

		void UpdateBlocks();

		void ProcessTouchDown(int id, float x, float y);
		void ProcessTouchUp(int id, float x, float y);
		void ProcessTouchHold(int id, float x, float y);
	}

	public interface IGameplayHost {
		ScoringManager GetScoringManager();
		MidiSequencer GetMidiSequencer();

		Vector2 GetCanvasSize();

		void StartNote(NoteSequenceCollection.Note note);
		void StopNote(NoteSequenceCollection.Note note);
		void PlayBackgroundNotes(Block block);
		void PlayBackgroundNote(NoteSequenceCollection.Note note);
		void AddBackgroundNote(NoteSequenceCollection.Note note);
	}
}