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
			tentativeBlockDict.Clear();

			for (int i = 0; i < gameSequences.Count; i++) {
				var seq = gameSequences[i];
				var track = gameTracks[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks + cacheTicks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];
					// start game block
					AddTentativeGameBlock(seqNote);
				}
			}

			foreach (var pair in tentativeBlockDict) {
				var lane = pair.Key;
				var tentativeBlock = pair.Value;

				Block block;
				if (tentativeBlock.type == Block.BlockType.Instant) {
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, instantBlocks, ref instantBlocksFreeStartIndex,
						instantBlockPrefab, instantBlockPageRect);
				} else if (tentativeBlock.type == Block.BlockType.Short) {
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, shortBlocks, ref shortBlocksFreeStartIndex,
						shortBlockPrefab, shortBlockPageRect);
				} else {  // tentativeBlock.type == Block.BlockType.Long
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, longBlocks, ref longBlocksFreeStartIndex,
						longBlockPrefab, longBlockPageRect);
				}
				block.rect.SetAsLastSibling();
				block.lane = lane;
				block.x = laneXDict[lane];
			}
		}

		void AddTentativeGameBlock(NoteSequenceCollection.Note note) {
//			Debug.LogFormat("tentative ch{0} n{1} {2} {3}", note.channel, note.note, note.start, note.duration);
			Block overlappingLongBlock = null;
			for (int i = 0; i < longBlocksFreeStartIndex; i++) {
				var block = longBlocks[i];
				if (note.start <= block.end) {
//					Debug.Log("Overlap!");
					overlappingLongBlock = block;
					break;
				}
			}

			if (note.duration <= maxInstantBlockTicks) {
				// tentative instant block
				AddTentativeInstantBlock(note);
			} else if (note.duration <= maxShortBlockTicks) {
				// tentative short block
				if (overlappingLongBlock != null) {
					// if has overlapping long block, change to instant block
					AddTentativeInstantBlock(note);
				} else {
					AddTentativeShortBlock(note);
				}
			} else {
				// tentative long block
				if (overlappingLongBlock != null) {
					overlappingLongBlock.end = note.start;
				}
				AddTentativeLongBlock(note);
			}
		}

		void AddTentativeInstantBlock(NoteSequenceCollection.Note note) {
//			Debug.Log("tentative instant");
			int lane = note.note % laneCount;
			Block existingBlock;
			if (tentativeBlockDict.TryGetValue(lane, out existingBlock)) {
				// if has exising block, append as background note
				existingBlock.backgroundNotes.Add(note);
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, Block.BlockType.Instant, note);
			}
		}

		void AddTentativeShortBlock(NoteSequenceCollection.Note note) {
//			Debug.Log("tentative short");
			int lane = note.note % laneCount;
			Block existingBlock;
			if (tentativeBlockDict.TryGetValue(lane, out existingBlock)) {
				// if has exising block
				if (existingBlock.type == Block.BlockType.Instant) {
					// if has existing instant block, override the instant block
					OverrideTentativeBlock(existingBlock, Block.BlockType.Short, note);
				} else {
					// if short or long, append as background note
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, Block.BlockType.Short, note);
			}
		}

		void AddTentativeLongBlock(NoteSequenceCollection.Note note) {
//			Debug.Log("tentative long");
			int lane = note.note % laneCount;
			Block existingBlock;
			if (tentativeBlockDict.TryGetValue(lane, out existingBlock)) {
				// if has exising block
				if (existingBlock.type != Block.BlockType.Long) {
					// if has existing instant or short block, override the instant block
					OverrideTentativeBlock(existingBlock, Block.BlockType.Long, note);
				} else if (existingBlock.note.end < note.end) {
					// if long and shorter, override
					OverrideTentativeBlock(existingBlock, Block.BlockType.Long, note);
				} else {
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, Block.BlockType.Long, note);
			}
		}

		void AddTentativeBlock(int lane, Block.BlockType type, NoteSequenceCollection.Note note) {
			tentativeBlockDict.Add(lane, new Block{type = type, note = note});
		}

		static void OverrideTentativeBlock(Block block, Block.BlockType newType, NoteSequenceCollection.Note newNote) {
			block.type = newType;
			block.backgroundNotes.Add(block.note);
			block.note = newNote;
		}

		static Block GetOrCreateBlockFromTentativeBlock(Block tentativeBlock, List<Block> blocks, ref int freeStartIndex, 
			GameObject blockPrefab, RectTransform blockPageRect
		) {
			Block block;
			if (freeStartIndex < blocks.Count) {
				block = blocks[freeStartIndex];
				block.note = tentativeBlock.note;
				block.backgroundNotes = tentativeBlock.backgroundNotes;
				block.end = tentativeBlock.note.end;
			} else {
				var instant = Instantiate(blockPrefab, blockPageRect);
				tentativeBlock.rect = instant.GetComponent<RectTransform>();
				tentativeBlock.color = instant.GetComponent<MultiGraphicColorSettable>();
				tentativeBlock.end = tentativeBlock.note.end;
				tentativeBlock.index = blocks.Count;
				blocks.Add(block = tentativeBlock);
			}
			freeStartIndex += 1;

			block.Reset();
			return block;
		}
	}
}
