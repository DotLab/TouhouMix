using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif;
using Uif.Settables.Components;

namespace TouhouMix.Levels.Gameplay {
  [System.Serializable]
  public sealed class ScanningLineGameplayManager : OneOnlyGameplayManager {
		[Space]
		public int scanningEsType;
		public float scanningBeats = 3;
		float scanningTicks;
		float scanningTicks2;

		[Space]
		public float scannerThickness = 2;
		float scanningSpace;

		[Space]
		public int blockShowEsType;
		public int blockHideEsType;

		float lastTicks = 0;

		float ScannerY {
			set { judgeRect.anchoredPosition = new Vector2(0, value); }
		}

		float blockScaling = 1;

		public override void Init(IGameplayHost host) {
			base.Init(host);

			scanningTicks = scanningBeats * midiFile.ticksPerBeat;
			scanningTicks2 = scanningTicks + scanningTicks;

			blockScaling = blockWidth / 100;

			//lastTicks = midiSequencer.ticks;

			scanningSpace = host.GetCanvasSize().y - judgeHeight - judgeHeight;
		}

		public override void AddTentativeNote(NoteSequenceCollection.Note note) {
			int lane = note.note % laneCount;

			Block block;
			//float durationInSeconds = note.duration / midiFile.ticksPerBeat / host.GetBeatsPerSecond();
			float durationInSeconds = midiSequencer.ToSeconds(note.duration);
			//Debug.Log(durationInSeconds);
			if (durationInSeconds <= maxInstantBlockSeconds) {
				// tentative instant block
				var tentativeBlock = new Block { type = BlockType.INSTANT, note = note };
				block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, instantBlocks, ref instantBlocksFreeStartIndex, instantBlockPrefab, instantBlockPageRect);
			} else {
				// tentative short block
				var tentativeBlock = new Block { type = BlockType.SHORT, note = note };
				block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, shortBlocks, ref shortBlocksFreeStartIndex, shortBlockPrefab, instantBlockPageRect);
			}

			block.rect.SetAsLastSibling();
			block.rect.sizeDelta = new Vector2(blockWidth, 0);
			block.lane = lane;
			block.x = laneXDict[lane];

			float progress = (midiSequencer.ticks - lastTicks) / scanningTicks2;
			progress -= (int)progress;
			if (progress < .5f) {  // Up
				block.color.Set(Color.red);
			} else {
				block.color.Set(Color.cyan);
			}

			block.rect.anchoredPosition = new Vector2(block.x, GetY(note.start));
		}

		float GetY(float ticks) {
			float progress = ticks / scanningTicks2;
			progress -= (int)progress;
			if (progress < .5f) {  // Up
				return judgeHeight + scanningSpace * Mathf.Clamp01(Es.Calc(scanningEsType, progress * 2));
			} else {
				return judgeHeight + scanningSpace * (1f - Mathf.Clamp01(Es.Calc(scanningEsType, (progress - .5f) * 2)));
			}
		}

		protected override void MarkTouchedLanes(float x, float y) {
			touchedLaneSet.Clear();

			if (y > host.GetCanvasSize().y - judgeHeight * .5f) {
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

		public override void GenerateBlocks() {
		}

		public override void UpdateBlocks() {
			base.UpdateBlocks();

			ScannerY = GetY(midiSequencer.ticks);
		}

		protected override void UpdateBlocks(List<Block> blocks, ref int freeStartIndex) {
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
					if (ticks < start) {
						// in cache period
						float t = Mathf.Clamp01(Es.Calc(blockShowEsType, 1 - (start - ticks) / cacheTicks));
						block.rect.eulerAngles = new Vector3(0, 0, 10f * (1f - t));
						block.rect.localScale = Vector3.one * blockScaling * t;
					} else { // in grace period
						float t = Mathf.Clamp01(Es.Calc(blockHideEsType, 1 - (ticks - start) / graceTicks));
						block.rect.eulerAngles = new Vector3(0, 0, -30f * (1f - t));
						block.rect.localScale = Vector3.one * blockScaling * t;
					}
				}
			}
		}
	}
}
