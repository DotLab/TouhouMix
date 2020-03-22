using UnityEngine;
using Midif.V3;

namespace TouhouMix.Levels.Gameplay {
	public interface IGameplayManager {
		void Init(IGameplayHost level);

		void AddTentativeBlock(NoteSequenceCollection.Note note);
		void GenerateBlocks();

		void UpdateBlocks();

		void ProcessTouchDown(int id, float x, float y);
		void ProcessTouchUp(int id, float x, float y);
		void ProcessTouchHold(int id, float x, float y);
	}

	public interface IGameplayHost {
		ScoringManager GetScoringManager();
		MidiSequencer GetMidiSequencer();
		MidiFile GetMidiFile();

		Vector2 GetCanvasSize();

		float GetBeatsPerSecond();

		void StartNote(NoteSequenceCollection.Note note);
		void StopNote(NoteSequenceCollection.Note note);
		void AddBackgroundNotes(GameplayBlock block);
	}
}