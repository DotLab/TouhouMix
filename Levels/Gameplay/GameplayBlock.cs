using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif.Settables;

namespace TouhouMix.Levels.Gameplay {
	public class GameplayBlock {
		public enum BlockType {
			Instant,
			Short,
			Long,
		}

		public BlockType type;
		public NoteSequenceCollection.Note note;
		public List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();

		public float end;
		public RectTransform rect;
		public ISettable<Color> color;

		public int lane;
		public float x;
		public int index;

		public int holdingFingerId;
		public float holdingOffset;
		public float holdingX;

		public void Reset() {
			rect.gameObject.SetActive(true);
			holdingFingerId = -1;
		}
	}
}
