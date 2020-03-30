using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;

namespace TouhouMix.Levels.Gameplay {
	public interface IBlockGenerator {
		void AddTentativeBlock(NoteSequenceCollection.Note note);

		void ClearTentativeBlocks();
	}
}