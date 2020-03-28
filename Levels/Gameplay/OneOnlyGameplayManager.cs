using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif;
using Uif.Settables;
using Uif.Settables.Components;

namespace TouhouMix.Levels.Gameplay {
  [System.Serializable]
  public class OneOnlyGameplayManager : IGameplayManager {
		public GameObject instantBlockPrefab;
		public GameObject shortBlockPrefab;
		public GameObject longBlockPrefab;
		public RectTransform instantBlockPageRect;
		public RectTransform shortBlockPageRect;
		public RectTransform longBlockPageRect;
		public float maxInstantBlockSeconds = .2f;
		public float maxShortBlockSeconds = .8f;

		[Space]
		public int cacheEsType;
		public float cacheBeats = 2;
		public int graceEsType;
		public float graceBeats = 1;
		protected float cacheTicks;
		protected float graceTicks;

		[Space]
		public RectTransform judgeRect;
		public float judgeHeight = 80;
		public float judgeThickness = 2;
		protected float cacheHeight;

		[Space]
		public int laneCount = 12;
		public float blockWidth = 100;
		public float blockJudgingWidth = 120;
		protected float[] laneXDict;
		protected float blockJudgingHalfWidth;

		protected IGameplayHost host;
		protected ScoringManager scoringManager;
		protected MidiFile midiFile;
		protected MidiSequencer midiSequencer;

		protected readonly List<Block> instantBlocks = new List<Block>();
		protected readonly List<Block> shortBlocks = new List<Block>();
		protected readonly List<Block> longBlocks = new List<Block>();
		protected int instantBlocksFreeStartIndex;
		protected int shortBlocksFreeStartIndex;
		protected int longBlocksFreeStartIndex;

		protected readonly Dictionary<int, Block> holdingBlockDict = new Dictionary<int, Block>();
		protected readonly Dictionary<int, Block> tentativeBlockDict = new Dictionary<int, Block>();
		protected readonly HashSet<int> touchedLaneSet = new HashSet<int>();

		public virtual void Init(IGameplayHost host) {
			this.host = host;

			scoringManager = host.GetScoringManager();
			midiFile = host.GetMidiFile();
			midiSequencer = host.GetMidiSequencer();

			var canvasSize = host.GetCanvasSize();

			judgeRect.anchoredPosition = new Vector2(0, judgeHeight);
			judgeRect.sizeDelta = new Vector2(0, judgeThickness);
			cacheHeight = canvasSize.y - judgeHeight;

			float canvasWidth = canvasSize.x;
			laneXDict = new float[laneCount];
			float laneStart = blockWidth * .5f;
			float laneSpacing = (canvasWidth - blockWidth) / (laneCount - 1);
			for (int i = 0; i < laneXDict.Length; i++) {
				laneXDict[i] = laneStart + i * laneSpacing;
			}

			cacheTicks = cacheBeats * midiFile.ticksPerBeat;
			graceTicks = graceBeats * midiFile.ticksPerBeat;

			blockJudgingHalfWidth = blockJudgingWidth * .5f;

			midiSequencer.ticks = -cacheTicks;
		}

		#region Block Generation
		public virtual void AddTentativeBlock(NoteSequenceCollection.Note note) {
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

			float durationInSeconds = (float)note.duration / midiFile.ticksPerBeat / midiSequencer.beatsPerSecond;
			//Debug.Log(durationInSeconds);
			if (durationInSeconds <= maxInstantBlockSeconds) {
				// tentative instant block
				AddTentativeInstantBlock(note);
			} else if (durationInSeconds <= maxShortBlockSeconds) {
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
			if (tentativeBlockDict.TryGetValue(lane, out Block existingBlock)) {
				// if has exising block, append as background note
				existingBlock.backgroundNotes.Add(note);
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, GameplayBlock.BlockType.Instant, note);
			}
		}

		void AddTentativeShortBlock(NoteSequenceCollection.Note note) {
			//			Debug.Log("tentative short");
			int lane = note.note % laneCount;
			if (tentativeBlockDict.TryGetValue(lane, out Block existingBlock)) {
				// if has exising block
				if (existingBlock.type == GameplayBlock.BlockType.Instant) {
					// if has existing instant block, override the instant block
					OverrideTentativeBlock(existingBlock, GameplayBlock.BlockType.Short, note);
				} else {
					// if short or long, append as background note
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, GameplayBlock.BlockType.Short, note);
			}
		}

		void AddTentativeLongBlock(NoteSequenceCollection.Note note) {
			//			Debug.Log("tentative long");
			int lane = note.note % laneCount;
			if (tentativeBlockDict.TryGetValue(lane, out Block existingBlock)) {
				// if has exising block
				if (existingBlock.type != GameplayBlock.BlockType.Long) {
					// if has existing instant or short block, override the instant block
					OverrideTentativeBlock(existingBlock, GameplayBlock.BlockType.Long, note);
				} else if (existingBlock.note.end < note.end) {
					// if long and shorter, override
					OverrideTentativeBlock(existingBlock, GameplayBlock.BlockType.Long, note);
				} else {
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, GameplayBlock.BlockType.Long, note);
			}
		}

		void AddTentativeBlock(int lane, GameplayBlock.BlockType type, NoteSequenceCollection.Note note) {
			tentativeBlockDict.Add(lane, new Block { type = type, note = note });
		}

		void OverrideTentativeBlock(Block block, GameplayBlock.BlockType newType, NoteSequenceCollection.Note newNote) {
			block.type = newType;
			block.backgroundNotes.Add(block.note);
			block.note = newNote;
		}

		public virtual void GenerateBlocks() {
			foreach (var pair in tentativeBlockDict) {
				var lane = pair.Key;
				var tentativeBlock = pair.Value;

				Block block;
				if (tentativeBlock.type == GameplayBlock.BlockType.Instant) {
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, instantBlocks, ref instantBlocksFreeStartIndex,
						instantBlockPrefab, instantBlockPageRect);
				} else if (tentativeBlock.type == GameplayBlock.BlockType.Short) {
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, shortBlocks, ref shortBlocksFreeStartIndex,
						shortBlockPrefab, shortBlockPageRect);
				} else {  // tentativeBlock.type == GameplayBlock.BlockType.Long
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, longBlocks, ref longBlocksFreeStartIndex,
						longBlockPrefab, longBlockPageRect);
				}
				block.rect.SetAsLastSibling();
				block.rect.sizeDelta = new Vector2(blockWidth, 0);
				block.lane = lane;
				block.x = laneXDict[lane];
			}

			tentativeBlockDict.Clear();
		}

		protected virtual Block GetOrCreateBlockFromTentativeBlock(Block tentativeBlock, List<Block> blocks, ref int freeStartIndex, 
			GameObject blockPrefab, RectTransform blockPageRect) {
			Block block;
			if (freeStartIndex < blocks.Count) {
				block = blocks[freeStartIndex];
				block.note = tentativeBlock.note;
				block.backgroundNotes = tentativeBlock.backgroundNotes;
				block.end = tentativeBlock.note.end;
			} else {
				var instant = GameObject.Instantiate(blockPrefab, blockPageRect);
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
		#endregion

		#region Touch

		public void ProcessTouchDown(int id, float x, float y) {
			MarkTouchedLanes(x, y);

			int bestBlockIndex = -1;
			Block bestBlock = null;
			float bestTimingDiff = -1;
			float bestOffset = -1;
			FindBestNote(x, instantBlocks, instantBlocksFreeStartIndex,
				ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);
			FindBestNote(x, shortBlocks, shortBlocksFreeStartIndex,
				ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);
			FindBestNote(x, longBlocks, longBlocksFreeStartIndex,
				ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);

			if (bestBlock == null) return;

			switch (bestBlock.type) {
				case GameplayBlock.BlockType.Instant: TouchInstantBlock(bestBlock, bestBlockIndex); break;
				case GameplayBlock.BlockType.Short: TouchShortBlock(bestBlock, bestBlockIndex); break;
				case GameplayBlock.BlockType.Long: TouchLongBlock(bestBlock, bestBlockIndex, id, x); break;
			}
		}

		public void ProcessTouchUp(int id, float x, float y) {
			MarkTouchedLanes(x, y);

			if (!holdingBlockDict.TryGetValue(id, out Block holdingBlock)) {
				return;
			}
			holdingBlock.holdingFingerId = -1;
			holdingBlockDict.Remove(id);
			HideAndFreeTouchedBlock(holdingBlock, holdingBlock.index, longBlocks, ref longBlocksFreeStartIndex);
			scoringManager.CountScoreForLongBlockTail(GetTiming(holdingBlock.end), holdingBlock);

			// Only check instant block when ending long block
			CheckAllInstantBlocks(false);
		}

		public void ProcessTouchHold(int id, float x, float y) {
			MarkTouchedLanes(x, y);

			CheckAllInstantBlocks(true);

			if (!holdingBlockDict.TryGetValue(id, out Block holdingBlock)) {
				return;
			}
			holdingBlock.holdingX = x + holdingBlock.holdingOffset;
		}

		protected virtual void MarkTouchedLanes(float x, float y) {
			touchedLaneSet.Clear();
			if (y > 0.5f * host.GetCanvasSize().y) {
				// only mark lane as touched when touch is in range
				return;
			}

			for (int i = 0; i < laneCount; i++) {
				float laneX = laneXDict[i];
				if (laneX - blockJudgingHalfWidth <= x && x <= laneX + blockJudgingHalfWidth) {
					touchedLaneSet.Add(i);
				}
			}
		}

		bool CheckAllInstantBlocks(bool onlyCheckOverdue) {
			bool isInstantBlockTouched = false;
			float perfectTiming = scoringManager.perfectTiming;
			for (int i = 0; i < instantBlocksFreeStartIndex; i++) {
				var block = instantBlocks[i];
				if (!touchedLaneSet.Contains(block.lane)) continue;

				float tickDiff = midiSequencer.ticks - block.note.start;
				float timingDiff = midiSequencer.ToSeconds(tickDiff);
				//				Debug.LogFormat("compare instant note tick {1}, time {0}", timingDiff, tickDiff);
				if ((onlyCheckOverdue && 0 <= timingDiff && timingDiff <= perfectTiming) ||
					(!onlyCheckOverdue && -perfectTiming <= timingDiff && timingDiff <= perfectTiming)) {
					// if within perfect timing, touch
					TouchInstantBlock(block, i, true);
					isInstantBlockTouched = true;
				}
			}
			return isInstantBlockTouched;
		}

		void FindBestNote(float x, List<Block> blocks, int freeStartIndex,
			ref int bestBlockIndex, ref Block bestBlock, ref float bestTimingDiff, ref float bestOffset
		) {
			float badTiming = scoringManager.badTiming;
			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				if (!touchedLaneSet.Contains(block.lane)) continue;
				float tickDiff = midiSequencer.ticks - block.note.start;
				float timeingDiff = midiSequencer.ToSeconds(tickDiff);
				timeingDiff = timeingDiff < 0 ? -timeingDiff : timeingDiff;
				float offset = laneXDict[block.lane] - x;
				offset = offset < 0 ? -offset : offset;
				//				Debug.LogFormat("compare best note tick {2}, time {0}, offset {1}", timeingDiff, offset, tickDiff);
				if (timeingDiff <= badTiming) {
					// if within bad timing, compare
					if (bestBlock == null || timeingDiff < bestTimingDiff || (timeingDiff == bestTimingDiff && offset < bestOffset)) {
						bestBlock = block;
						bestTimingDiff = timeingDiff;
						bestBlockIndex = i;
						bestOffset = offset;
					}
				}
			}
		}

		void TouchInstantBlock(Block block, int index, bool isHolding = false) {
			block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block, index, instantBlocks, ref instantBlocksFreeStartIndex);
			scoringManager.CountScoreForBlock(GetTiming(block.note.start), block, isHolding);
			host.AddBackgroundNotes(block);
		}

		void TouchShortBlock(Block block, int index) {
			block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block, index, shortBlocks, ref shortBlocksFreeStartIndex);
			scoringManager.CountScoreForBlock(GetTiming(block.note.start), block);
			host.AddBackgroundNotes(block);
		}

		void TouchLongBlock(Block block, int _, int fingerId, float x) {
			block.holdingFingerId = fingerId;
			block.holdingOffset = block.x - x;
			block.holdingX = block.x;
			if (holdingBlockDict.TryGetValue(fingerId, out Block holdingBlock)) {
				scoringManager.CountMiss(holdingBlock);
				HideAndFreeTouchedBlock(holdingBlock, holdingBlock.index, longBlocks, ref longBlocksFreeStartIndex);
			}
			holdingBlockDict[fingerId] = block;
			scoringManager.CountScoreForBlock(GetTiming(block.note.start), block);
			host.AddBackgroundNotes(block);
			host.StartNote(block.note);
		}

		protected void HideAndFreeTouchedBlock(Block block, int index, List<Block> blocks, ref int freeStartIndex) {
			freeStartIndex -= 1;
			var lastActiveBlock = blocks[freeStartIndex];
			blocks[index] = lastActiveBlock;
			blocks[freeStartIndex] = block;
			lastActiveBlock.index = index;
			block.index = freeStartIndex;

			block.rect.gameObject.SetActive(false);
		}

		#endregion

		#region Block Update

		public virtual void UpdateBlocks() {
			UpdateBlocks(instantBlocks, ref instantBlocksFreeStartIndex);
			UpdateBlocks(shortBlocks, ref shortBlocksFreeStartIndex);
			UpdateLongBlocks(longBlocks, ref longBlocksFreeStartIndex);
		}

		protected virtual void UpdateBlocks(List<Block> blocks, ref int freeStartIndex) {
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				float start = block.note.start;
				if (start <= ticks - graceTicks) {
					// miss
					scoringManager.CountMiss(block);
					HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
					i -= 1;
				} else {
					float y = GetY(start);
					block.rect.anchoredPosition = new Vector2(block.x, y);
				}
			}
		}

		void UpdateLongBlocks(List<Block> blocks, ref int freeStartIndex) {
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				float start = block.note.start;
				float end = block.end;
				if (end <= ticks - graceTicks) {
					// miss
					scoringManager.CountMiss(block);
					HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
					i -= 1;
				} else if (block.holdingFingerId != -1 && end <= ticks) {
					// hold finish
					holdingBlockDict.Remove(block.holdingFingerId);
					block.holdingFingerId = -1;
					scoringManager.CountScoreForLongBlockTail(GetTiming(block.end), block, true);
					host.StopNote(block.note);
					HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
					i -= 1;
				} else {
					float startY = block.holdingFingerId != -1 ? judgeHeight : GetY(start);
					float endY = GetY(end);
					block.rect.anchoredPosition = new Vector2(block.holdingFingerId != -1 ? block.holdingX : block.x, startY);
					block.rect.sizeDelta = new Vector2(blockWidth, endY - startY);
				}
			}
		}

		float GetY(float start) {
			float ticks = midiSequencer.ticks;
			if (ticks < start) {
				// in cache period
				return Es.Calc(cacheEsType, Clamp01((start - ticks) / cacheTicks)) * cacheHeight + judgeHeight;
			} else { // start <= ticks
							 // in grace period
				return judgeHeight - judgeHeight * Es.Calc(graceEsType, Clamp01((ticks - start) / graceTicks));
			}
		}

		float Clamp01(float t) {
			return t < 0 ? 0 : t > 1 ? 1 : t;
		}

		#endregion

		float GetTiming(float timingTicks) {
			float timing = midiSequencer.ticks - timingTicks;
			if (timing < 0) timing = -timing;
			return midiSequencer.ToSeconds(timing);
		}

		public sealed class Block : GameplayBlock {
		}
	}
}
