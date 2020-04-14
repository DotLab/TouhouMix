using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Systemf;

namespace TouhouMix.Levels.Gameplay {
	public enum BlockType {
		INSTANT,
		SHORT,
		LONG,
	}

	public class Block : IActiveCheckable, IIndexable {
		public bool active;
		public bool isTentative;

		public BlockType type;
		public NoteSequenceCollection.Note note;
		public List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();

		// Long block can override note end
		public float end;

		// Display and touch
		public int batch;
		public int lane;
		public float x;
		public int index;

		// Holding block
		public int holdingFingerId;
		public float holdingOffset;
		public float holdingX;

		// Skins
		public string skinName;
		public BlockSkinController skin;
		// Long block skins
		public BlockSkinController longFillSkin;
		public BlockSkinController longEndSkin;

		public bool Active {
			get { return active; }
		}

		public int Index {
			get { return index; }
			set { index = value; }
		}

		public virtual Vector2 Position {
			get { return skin.rect.anchoredPosition; }
		}
	}
}
