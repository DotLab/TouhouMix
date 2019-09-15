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
		void ProcessMouse() {
			var position = Input.mousePosition.Vec2().Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);

			float x = position.x;
			if (position.y < 0.5f * sizeWatcher.canvasSize.y) {
				// only mark lane as touched when touch is in range
				MarkTouchedLanes(x);
			}

			if (Input.GetMouseButtonDown(0)) {
				// find the nearest note to press
				ProcessTouchDown(x, MOUSE_TOUCH_ID);
			} else if (Input.GetMouseButtonUp(0)) {
				// clean up hold and find the nearest perfect instant key to press
				ProcessTouchUp(x, MOUSE_TOUCH_ID);
			} else if (Input.GetMouseButton(0)) {
				// update hold and find the perfect instant key to press
				ProcessHold(x, MOUSE_TOUCH_ID);
			}
		}

		void ProcessTouches() {
			var touchCount = Input.touchCount;

			for (int i = 0; i < touchCount; i++) {
				var touch = Input.GetTouch(i);
				var position = touch.position.Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);

				float x = position.x;
				if (position.y < 0.5f * sizeWatcher.canvasSize.y) {
					// only mark lane as touched when touch is in range
					MarkTouchedLanes(x);
				}

				if (touch.phase == TouchPhase.Began) {
					// find the nearest note to press
					ProcessTouchDown(x, touch.fingerId);
				} else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
					// clean up hold and find the nearest perfect instant key to press
					ProcessTouchUp(x, touch.fingerId);
				} else {
					// update hold and find the perfect instant key to press
					ProcessHold(x, touch.fingerId);
				}
			}
		}

		void MarkTouchedLanes(float x) {
			touchedLaneSet.Clear();
			for (int i = 0; i < laneCount; i++) {
				float laneX = laneXDict[i];
				if (laneX - blockJudgingHalfWidth <= x && x <= laneX + blockJudgingHalfWidth) {
					touchedLaneSet.Add(i);
				}
			}
		}

		void ProcessTouchDown(float x, int fingerId) {
//			bool isInstantBlockTouched = CheckAllInstantBlocks(false);
//			if (isInstantBlockTouched) return;

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
				
//			if (bestBlock.type == Block.BlockType.Instant) {
//				TouchInstantBlock(bestBlock, bestBlockIndex);
//			} else if (bestBlock.type == Block.BlockType.Short) {
//				TouchShortBlock(bestBlock, bestBlockIndex);
//			} else {
//				TouchLongBlock(bestBlock, bestBlockIndex, fingerId, x);
//			}
			switch (bestBlock.type) {
			case Block.BlockType.Instant: TouchInstantBlock(bestBlock, bestBlockIndex); break;
			case Block.BlockType.Short: TouchShortBlock(bestBlock, bestBlockIndex); break;
			case Block.BlockType.Long: TouchLongBlock(bestBlock, bestBlockIndex, fingerId, x); break;
			}
		}

		void ProcessTouchUp(float x, int fingerId) {
			Block holdingBlock;
			if (holdingBlockDict.TryGetValue(fingerId, out holdingBlock)) {
				holdingBlock.holdingFingerId = -1;
				holdingBlockDict.Remove(fingerId);
				HideAndFreeTouchedBlock(holdingBlock, holdingBlock.index, longBlocks, ref longBlocksFreeStartIndex);
				CountScoreForLongBlockTail(holdingBlock);

				// Only check instant block when ending long block
				CheckAllInstantBlocks(false);
			}
		}

		void ProcessHold(float x, int fingerId) {
			CheckAllInstantBlocks(true);

			Block holdingBlock;
			if (holdingBlockDict.TryGetValue(fingerId, out holdingBlock)) {
				holdingBlock.holdingX = x + holdingBlock.holdingOffset;
			}
		}

		bool CheckAllInstantBlocks(bool onlyCheckOverdue) {
			bool isInstantBlockTouched = false;
			for (int i = 0; i < instantBlocksFreeStartIndex; i++) {
				var block = instantBlocks[i];
				if (!touchedLaneSet.Contains(block.lane)) continue;

				float tickDiff = ticks - block.note.start;
				float timingDiff = midiSequencer.ToSeconds(tickDiff);
//				Debug.LogFormat("compare instant note tick {1}, time {0}", timingDiff, tickDiff);
				if ((onlyCheckOverdue && 0 <= timingDiff && timingDiff <= perfectTiming) ||
					(!onlyCheckOverdue && -perfectTiming <= timingDiff && timingDiff <= perfectTiming)) {
					// if within perfect timing, touch
					TouchInstantBlock(block, i);
					isInstantBlockTouched = true;
				}
			}
			return isInstantBlockTouched;
		}

		void FindBestNote(float x, List<Block> blocks, int freeStartIndex, 
			ref int bestBlockIndex, ref Block bestBlock, ref float bestTimingDiff, ref float bestOffset
		) {
			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				if (!touchedLaneSet.Contains(block.lane)) continue;
				float tickDiff = ticks - block.note.start;
				float timeingDiff = midiSequencer.ToSeconds(tickDiff);
				timeingDiff = timeingDiff < 0 ? -timeingDiff : timeingDiff;
				float offset = laneXDict[block.lane] - x;
				offset = offset < 0 ? -offset : offset;
//				Debug.LogFormat("compare best note tick {2}, time {0}, offset {1}", timeingDiff, offset, tickDiff);
				if (timeingDiff <= badTiming) {
					// if within bad timing, compare
					if (bestBlock == null || timeingDiff < bestTimingDiff || offset < bestOffset) {
						bestBlock = block;
						bestTimingDiff = timeingDiff;
						bestBlockIndex = i;
						bestOffset = offset;
					}
				}
			}
		}

		void TouchInstantBlock(Block block, int index) {
			block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block, index, instantBlocks, ref instantBlocksFreeStartIndex);
			CountScoreForBlock(block);
			AddAsBackgroundNotes(block, true);
		}

		void TouchShortBlock(Block block, int index) {
			block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block, index, shortBlocks, ref shortBlocksFreeStartIndex);
			CountScoreForBlock(block);
			AddAsBackgroundNotes(block, true);
		}

		void TouchLongBlock(Block block, int index, int fingerId, float x) {
			block.holdingFingerId = fingerId;
			block.holdingOffset = block.x - x;
			block.holdingX = block.x;
			holdingBlockDict.Add(fingerId, block);
			CountScoreForBlock(block);
			AddAsBackgroundNotes(block, true);
			StartLongNote(block.note);
		}

		static void HideAndFreeTouchedBlock(Block block, int index, List<Block> blocks, ref int freeStartIndex) {
			freeStartIndex -= 1;
			var lastActiveBlock = blocks[freeStartIndex];
			blocks[index] = lastActiveBlock;
			blocks[freeStartIndex] = block;
			lastActiveBlock.index = index;
			block.index = freeStartIndex;

			block.rect.gameObject.SetActive(false);
		}
	}
}
