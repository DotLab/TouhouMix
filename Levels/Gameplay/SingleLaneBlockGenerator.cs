using System.Collections.Generic;
using UnityEngine;
using Note = Midif.V3.NoteSequenceCollection.Note;
using Sequence = Midif.V3.NoteSequenceCollection.Sequence;

namespace TouhouMix.Levels.Gameplay {
	[System.Serializable]
	public sealed class SingleLaneBlockGenerator {
		sealed class VirtualTouch {
			public int index;
			public bool isFree = true;
			public Note holdingNote;
			public float lastPressSeconds = float.MinValue;
			public float lastPressX;
			public BlockInfo lastPressBlock;
		}

		public sealed class BlockInfo {
			public Note note;
			public int batch;
			public int lane;
			public float x;
			public int touchIndex = -1;
			public BlockType type;

			public BlockInfo prev;
		}

		public int maxTouchCount = 2;
		public int laneCount;
		public float[] laneX;
		public float minTapInterval = 0;
		public float cooldownSeconds = 2;
		public float maxTouchMoveVelocity = 400;
		public float blockCoalesceSeconds = .1f;

		public float instantBlockSeconds;
		public float shortBlockSeconds;

		public readonly List<BlockInfo> blocks = new List<BlockInfo>();
		public readonly List<Note> backgroundNotes = new List<Note>();
		readonly List<BlockInfo> batchBlocks = new List<BlockInfo>();
		VirtualTouch[] touches;
		Note[] noteLanes;

		void Reset() {
			blocks.Clear();
			touches = new VirtualTouch[maxTouchCount];
			backgroundNotes.Clear();
			for (int i = 0; i < maxTouchCount; i++) {
				touches[i] = new VirtualTouch { index = i };
			}
			noteLanes = new Note[laneCount];
			minMatchingTouchIndex = new int[maxTouchCount];
		}

		public List<BlockInfo> GenerateBlocks(List<Sequence> sequences) {
			Reset();

			var notes = new List<Note>();
			foreach (var seq in sequences) {
				notes.AddRange(seq.notes);
			}
			// Sort notes by time and channel
			notes.Sort((a, b) => {
				if (a.start == b.start) return a.channel.CompareTo(b.channel);
				return a.start.CompareTo(b.start);
			});

			float seconds = 0;
			int batch = 0;
			var coalescedNotes = new List<Note>();
			foreach (var note in notes) {
				//Debug.LogFormat("seconds {0} start {1} coalesced {2}", seconds, note.startSeconds, coalescedNotes.Count);
				if (coalescedNotes.Count == 0) {
					seconds = note.startSeconds;
					coalescedNotes.Add(note);
				} else {
					if (note.startSeconds > seconds + blockCoalesceSeconds) {
						// Cannot coalesce
						GenerateBlockBatch(batch, coalescedNotes);
						batch += 1;
						coalescedNotes.Clear();
						seconds = note.startSeconds;
					}
					// Coalesce
					coalescedNotes.Add(note);
				}
			}
			if (coalescedNotes.Count > 0) {
				GenerateBlockBatch(batch, coalescedNotes);
			}

			backgroundNotes.Sort((a, b) => { 
				if (a.start == b.start) {
					return a.note.CompareTo(b.note);
				}
				return a.start.CompareTo(b.start);
			});

			return blocks;
		}

		void GenerateBlockBatch(int batch, List<Note> notes) {
			// Remove overlapped notes
			foreach (var note in notes) {
				int lane = note.note % laneCount;
				if (noteLanes[lane] == null) {
					noteLanes[lane] = note;
				} else {
					if (noteLanes[lane].note < note.note) {
						// Replace
						backgroundNotes.Add(noteLanes[lane]);
						noteLanes[lane] = note;
					} else {
						backgroundNotes.Add(note);
					}
				}
			}

			batchBlocks.Clear();
			for (int i = 0; i < laneCount; i++) {
				var note = noteLanes[i];
				if (note != null) {
					batchBlocks.Add(new BlockInfo { note = note, lane = i, x = laneX[i], batch = batch });
					noteLanes[i] = null;
				}
			}
			// Remove lower sounding notes
			batchBlocks.Sort((a, b) => -a.note.note.CompareTo(b.note.note));
			while (batchBlocks.Count > maxTouchCount) {
				int lastIndex = batchBlocks.Count - 1;
				backgroundNotes.Add(batchBlocks[lastIndex].note);
				batchBlocks.RemoveAt(lastIndex);
			}

			//Debug.LogFormat("batch {0} game {1} bg {2}", notes.Count, batchBlocks.Count, backgroundNotes.Count);
			//for (int i = 0; i < batchBlocks.Count; i++) {
			//	var block = batchBlocks[i];
			//	Debug.LogFormat("lane {0} x {1:F2} start {2:F2}", block.lane, block.x, block.note.startSeconds);
			//}

			batchBlocks.Sort((a, b) => a.x.CompareTo(b.x));
			for (int i = 0; i < maxTouchCount; i++) {
				minMatchingTouchIndex[i] = -1;
			}
			minCost = float.MaxValue;
			FindOptimalMatchings(batchBlocks, batchBlocks.Count, 0, 0, 0);

			batchBlocks.Sort((a, b) => a.note.start.CompareTo(b.note.start));
			for (int i = 0; i < batchBlocks.Count; i++) {
				var block = batchBlocks[i];
				block.touchIndex = minMatchingTouchIndex[i];
				var touch = touches[block.touchIndex];

				if (block.note.durationSeconds <= instantBlockSeconds) {
					block.type = BlockType.INSTANT;
				} else if (block.note.durationSeconds <= shortBlockSeconds) {
					block.type = BlockType.SHORT;
				} else {
					block.type = BlockType.LONG;
				}

				if (!touch.isFree) {
					// Check if still not free
					if (touch.holdingNote == null && block.note.startSeconds > touch.lastPressSeconds + cooldownSeconds) {
						//Debug.Log("Free start");
						// Now free
						touch.isFree = true;
						touch.lastPressBlock = null;
					} else {
						//Debug.Log("Not free");
						// Still not free;
						block.prev = touch.lastPressBlock;
						float maxOffset = maxTouchMoveVelocity * (block.note.startSeconds - touch.lastPressSeconds);
						if (Mathf.Abs(block.x - touch.lastPressX) > maxOffset) {
							if (block.x > touch.lastPressX) {
								block.x = touch.lastPressX + maxOffset;
							} else {
								block.x = touch.lastPressX - maxOffset;
							}
						}
					}
				}

				if (touch.holdingNote != null) {
					if (block.note.startSeconds < touch.holdingNote.endSeconds) {
						// Still holding
						//Debug.Log("Still holding");
						block.type = BlockType.INSTANT;
					} else {
						// Holding end
						//Debug.Log("Holding end");
						touch.holdingNote = null;
					}
				}

				// If the note appears too fast, generate instant
				if (block.note.startSeconds - touch.lastPressSeconds < minTapInterval) {
					//Debug.Log("Tapping too fast");
					block.type = BlockType.INSTANT;
				}

				if (block.type == BlockType.LONG) {
					touch.holdingNote = block.note;
				}
				touch.isFree = false;
				touch.lastPressSeconds = block.note.startSeconds;
				touch.lastPressX = block.x;
				touch.lastPressBlock = block;

				//Debug.LogFormat("optimal lane {0} touch {1} x {2:F2} start {3:F2} type {4} du {5:F2}", block.lane, block.touchIndex, block.x, block.note.startSeconds, block.type, block.note.durationSeconds);

				blocks.Add(block);
			}
		}

		float minCost;
		int[] minMatchingTouchIndex;

		void FindOptimalMatchings(List<BlockInfo> blocks, int blockCount, int blockIndex, int touchIndex, float cost) {
			if (blockIndex == blockCount) {
				// Match finishes
				// for (int i = 0; i < blockCount; i++) {
				// Debug.LogFormat("    block {0} ({1}) touch {2} ({3})", i, blocks[i].x, blocks[i].touchIndex, touches[blocks[i].touchIndex].lastTapX);
				// }
				// Debug.Log("  cost " + cost);
				if (cost < minCost) {
					minCost = cost;
					for (int i = 0; i < blockCount; i++) {
						minMatchingTouchIndex[i] = blocks[i].touchIndex;
					}
					// Debug.Log("  new min cost " + minCost);
				}
				return;
			}

			for (int i = touchIndex; i < maxTouchCount; i++) {
				blocks[blockIndex].touchIndex = i;
				FindOptimalMatchings(blocks, blockCount, blockIndex + 1, i + 1, cost + Mathf.Abs(touches[i].lastPressX - blocks[blockIndex].x));
			}
		}
	}
}
