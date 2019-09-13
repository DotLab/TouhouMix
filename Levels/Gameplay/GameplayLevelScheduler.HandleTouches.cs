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
		void ProcessMouse(float ticks) {
			var position = Input.mousePosition.Vec2().Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);

			float x = position.x;
			if (position.y < 0.5f * sizeWatcher.canvasSize.y) {
				// only mark lane as touched when touch is in range
				MarkTouchedLanes(x);
			}

			if (Input.GetMouseButtonDown(0)) {
				// find the nearest note to press
				TouchDown(ticks, x, MOUSE_TOUCH_ID);
			} else if (Input.GetMouseButtonUp(0)) {
				// clean up hold and find the nearest perfect instant key to press
				TouchUp(ticks, x, MOUSE_TOUCH_ID);
			} else if (Input.GetMouseButton(0)) {
				// update hold and find the perfect instant key to press
				Hold(ticks, x, MOUSE_TOUCH_ID);
			}
		}

		void ProcessTouches(float ticks) {
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
					TouchDown(ticks, x, touch.fingerId);
				} else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
					// clean up hold and find the nearest perfect instant key to press
					TouchUp(ticks, x, touch.fingerId);
				} else {
					// update hold and find the perfect instant key to press
					Hold(ticks, x, touch.fingerId);
				}
			}
		}

		void TouchDown(float ticks, float x, int fingerId) {
			bool isInstantBlockTouched = CheckAllInstantBlocks(ticks, false);

			int bestBlockIndex = -1;
			Block bestBlock = null;
			float bestTimingDiff = -1;
			float bestOffset = -1;
			FindBestNote(ticks, x, shortBlocks, shortBlocksFreeStartIndex, 
				ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);
			FindBestNote(ticks, x, longBlocks, longBlocksFreeStartIndex, 
				ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);

			// when touched instant blocks, only touch short or long block when timing is perfect
			if (bestBlock == null || (isInstantBlockTouched && bestTimingDiff > perfectTiming)) return;
				
			if (bestBlock.type == Block.BlockType.Short) {
				TouchShortBlock(bestBlock, bestBlockIndex);
			} else {
				TouchLongBlock(bestBlock, bestBlockIndex, fingerId, x);
			}
		}

		void TouchUp(float ticks, float x, int fingerId) {
			CheckAllInstantBlocks(ticks, false);

			Block holdingBlock;
			if (holdingBlockDict.TryGetValue(fingerId, out holdingBlock)) {
				holdingBlock.holdingFingerId = -1;
				holdingBlockDict.Remove(fingerId);
				HideAndFreeTouchedBlock(holdingBlock, holdingBlock.index, longBlocks, ref longBlocksFreeStartIndex);
			}
		}

		void Hold(float ticks, float x, int fingerId) {
			CheckAllInstantBlocks(ticks, true);

			Block holdingBlock;
			if (holdingBlockDict.TryGetValue(fingerId, out holdingBlock)) {
				holdingBlock.holdingX = x + holdingBlock.holdingOffset;
			}
		}

		bool CheckAllInstantBlocks(float ticks, bool onlyCheckOverdue) {
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

		void FindBestNote(float ticks, float x, List<Block> blocks, int freeStartIndex, 
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
		}

		void TouchShortBlock(Block block, int index) {
			block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block, index, shortBlocks, ref shortBlocksFreeStartIndex);
		}

		void TouchLongBlock(Block block, int index, int fingerId, float x) {
			block.holdingFingerId = fingerId;
			block.holdingOffset = block.x - x;
			block.holdingX = block.x;
			holdingBlockDict.Add(fingerId, block);
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

		void MarkTouchedLanes(float x) {
			touchedLaneSet.Clear();
			for (int i = 0; i < laneCount; i++) {
				float laneX = laneXDict[i];
				if (laneX - blockJudgingHalfWidth <= x && x <= laneX + blockJudgingHalfWidth) {
					touchedLaneSet.Add(i);
				}
			}
		}
	}
}
