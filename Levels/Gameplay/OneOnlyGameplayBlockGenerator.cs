using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using System.Linq;
using Note = Midif.V3.NoteSequenceCollection.Note;
using Sequence = Midif.V3.NoteSequenceCollection.Sequence;

namespace TouhouMix.Levels.Gameplay {
	[System.Serializable]
	public sealed class OneOnlyGameplayBlockGenerator {
		sealed class VirtualTouch {
			public bool isFree = true;
			public Note holdingNote;
			public float lastTapSeconds = float.MinValue;
			public float lastTapX;
		}

		public sealed class BlockInfo {
			public Note note;
			public int lane;
			public float x;
			public int touchIndex = -1;
			public BlockType type;
		}

		public int maxTouchCount;
		public int laneCount;
		public float[] laneX;
		public float minTapIntervalSeconds;
		public float cooldownSeconds;
		public float maxTouchMoveVelocity;
		public float coalesceSeconds;

		public float instantBlockSeconds;
		public float shortBlockSeconds;

		readonly List<BlockInfo> blocks = new List<BlockInfo>();
		readonly List<VirtualTouch> touches = new List<VirtualTouch>();
		readonly List<Note> backgroundNotes = new List<Note>();
		Note[] noteLanes;

		void Reset() {
			blocks.Clear();
			touches.Clear();
			backgroundNotes.Clear();
			for (int i = 0; i < maxTouchCount; i++) {
				touches.Add(new VirtualTouch());
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
				if (a.start == b.start) {
					return a.channel.CompareTo(b.channel);
				}
				return a.start.CompareTo(b.start);
			});

			float seconds = 0;
			var coalescedNotes = new List<Note>();
			foreach (var note in notes) {
				Debug.LogFormat("seconds {0} start {1} coalesced {2}", seconds, note.startSeconds, coalescedNotes.Count);
				if (coalescedNotes.Count == 0) {
					seconds = note.startSeconds;
					coalescedNotes.Add(note);
				} else {
					if (note.startSeconds > seconds + coalesceSeconds) {
						// Cannot coalesce
						GenerateBlockBatch(coalescedNotes);
						coalescedNotes.Clear();
					}
					// Coalesce
					seconds = note.startSeconds;
					coalescedNotes.Add(note);
				}
			}
			if (coalescedNotes.Count > 0) {
				GenerateBlockBatch(coalescedNotes);
			}

			return blocks;
		}

		
		void GenerateBlockBatch(List<Note> notes) {
			var backgroundNotes = new List<Note>();
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

			var gameBlocks = new List<BlockInfo>();
			for (int i = 0; i < laneCount; i++) {
				var note = noteLanes[i];
				if (note != null) {
					gameBlocks.Add(new BlockInfo {note = note, lane=i, x = laneX[i] });
					noteLanes[i] = null;
				}
			}
			// Remove lower sounding notes
			gameBlocks.Sort((a, b) => -a.note.note.CompareTo(b.note.note));
			while (gameBlocks.Count > maxTouchCount) {
				int lastIndex = gameBlocks.Count - 1;
				backgroundNotes.Add(gameBlocks[lastIndex].note);
				gameBlocks.RemoveAt(lastIndex);
			}

			Debug.LogFormat("batch {0} game {1} bg {2}", notes.Count, gameBlocks.Count, backgroundNotes.Count);

			for (int i = 0; i < gameBlocks.Count; i++) {
				var block = gameBlocks[i];
				Debug.LogFormat("lane {0} x {1:F2} start {2:F2}", block.lane, block.x, block.note.startSeconds);
			}

			gameBlocks.Sort((a, b) => a.x.CompareTo(b.x));
			for (int i = 0; i < maxTouchCount; i++) {
				minMatchingTouchIndex[i] = -1;
			}
			minCost = float.MaxValue;
			GenerateOptimalViableBlocks(gameBlocks, gameBlocks.Count, 0, 0, 0);

			for (int i = 0; i < gameBlocks.Count; i++) {
				var touch = touches[minMatchingTouchIndex[i]];
				var block = gameBlocks[i];
				block.touchIndex = minMatchingTouchIndex[i];

				if (block.note.durationSeconds <= instantBlockSeconds) {
					block.type = BlockType.INSTANT;
				} else if (block.note.durationSeconds <= shortBlockSeconds) {
					block.type = BlockType.SHORT;
				} else {
					block.type = BlockType.LONG;
				}

				if (!touch.isFree) {
					if (touch.holdingNote == null && block.note.startSeconds > touch.lastTapSeconds + cooldownSeconds) {
						Debug.Log("Free start");
						// Now free
						touch.isFree = true;
					} else {
						Debug.Log("Not free");
						// Still not free;
						float maxOffset = maxTouchMoveVelocity * (block.note.startSeconds - touch.lastTapSeconds);
						if (Mathf.Abs(block.x - touch.lastTapX) > maxOffset) {
							if (block.x > touch.lastTapX) {
								block.x = touch.lastTapX + maxOffset;
							} else {
								block.x = touch.lastTapX - maxOffset;
							}
						}
					}
				}

				if (touch.holdingNote != null) {
					if (block.note.startSeconds < touch.holdingNote.endSeconds) {
						// Still holding
						Debug.Log("Still holding");
						block.type = BlockType.INSTANT;
					} else {
						// Holding end
						Debug.Log("Holding end");
						touch.holdingNote = null;
					}
				}

				// If the note appears too fast, generate instant
				if (block.note.startSeconds - touch.lastTapSeconds < minTapIntervalSeconds) {
					Debug.Log("Tapping too fast");
					block.type = BlockType.INSTANT;
				}

				if (block.type == BlockType.LONG) {
					touch.holdingNote = block.note;
				}

				touch.isFree = false;
				touch.lastTapSeconds = block.note.startSeconds;
				touch.lastTapX = block.x;

				Debug.LogFormat("optimal lane {0} touch {1} x {2:F2} start {3:F2} type {4}", block.lane, block.touchIndex, block.x, block.note.startSeconds, block.type);

				blocks.Add(block);
			}
		}

		float minCost;
		int[] minMatchingTouchIndex;

		void GenerateOptimalViableBlocks(List<BlockInfo> blocks, int blockCount, int blockIndex, int touchIndex, float cost) {
			if (blockIndex == blockCount) {
				// Match finishes
				for (int i = 0; i < blockCount; i++) {
					Debug.LogFormat("    block {0} ({1}) touch {2} ({3})", i, blocks[i].x, blocks[i].touchIndex, touches[blocks[i].touchIndex].lastTapX);
				}
				Debug.Log("  cost " + cost);
				if (cost < minCost) {
					minCost = cost;
					for (int i = 0; i < blockCount; i++) {
						minMatchingTouchIndex[i] = blocks[i].touchIndex;
					}
					Debug.Log("  new min cost " + minCost);
				}
				return;
			}

			for (int i = touchIndex; i < maxTouchCount; i++) {
				blocks[blockIndex].touchIndex = i;
				GenerateOptimalViableBlocks(blocks, blockCount, blockIndex + 1, i + 1, cost + Mathf.Abs(touches[i].lastTapX - blocks[blockIndex].x));
			}
		}
	}
}
