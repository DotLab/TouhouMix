using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif;
using Uif.Settables.Components;

namespace TouhouMix.Levels.Gameplay {
	[System.Serializable]
	public sealed class ScanningLineGameplayManagerV2 : OneOnlyGameplayManagerV2 {
		[Space]
		public int scanningEsType;
		public float scanningBeats = 3;
		float scanningTicks;
		float scanningTicks2;

		[Space]
		public float scannerThickness = 2;
		float scanningSpace;
		Vector2 canvasSize;

		[Space]
		public int blockShowEsType;
		public int blockHideEsType;

		float ScannerY {
			set { judgeRect.anchoredPosition = new Vector2(0, value); }
		}

		public override void Init(IGameplayHost host) {
			base.Init(host);

			scanningTicks = scanningBeats * midiSequencer.file.ticksPerBeat;
			scanningTicks2 = scanningTicks + scanningTicks;
			
			canvasSize = host.GetCanvasSize();

			scanningSpace = host.GetCanvasSize().y - judgeHeight - judgeHeight;
		}

		public override void GenerateBlocks() {
			base.GenerateBlocks();

			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				if (!block.isTentative) {
					continue;
				}

				if (block.type == BlockType.LONG) {
					float progressStart = block.note.start / scanningTicks2;
					progressStart -= (int)progressStart;
					float progressEnd = block.note.end / scanningTicks2;
					progressEnd -= (int)progressEnd;
				} else {
					float progress = block.note.start / scanningTicks2;
					progress -= (int)progress;
					if (progress < .5f) {  // Up
						block.skin.color.Set(Color.red);
					} else {
						block.skin.color.Set(Color.cyan);
					}

					block.skin.rect.anchoredPosition = new Vector2(block.x, GetY(block.note.start));
				}

			}

			for (int i = 0; i < connectContainer.firstFreeItemIndex; i++) {
				var connect = connectContainer.itemList[i];
				if (!connect.isTentative) {
					continue;
				}

				float progress = connect.startTick / scanningTicks2;
				progress -= (int)progress;
				if (progress < .5f) {  // Up
					connect.skin.color.Set(Color.red);
				} else {
					connect.skin.color.Set(Color.cyan);
				}

				float startY = GetY(connect.startTick);
				connect.skin.rect.anchoredPosition = new Vector2(connect.startX, startY);
				float endY = GetY(connect.endTick);
				float length = Vector2.Distance(new Vector2(connect.startX, startY), new Vector2(connect.endX, endY));
				connect.skin.rect.sizeDelta = new Vector2(connect.skin.rect.sizeDelta.x, length);
				connect.skin.rect.eulerAngles = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(connect.startX - connect.endX, endY - startY));
			}
		}

		protected override float GetY(float ticks) {
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

		public override void UpdateBlocks() {
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				block.isTentative = false;
				float start = block.note.start;
				if (start <= ticks - graceTicks) {
					// miss
					scoringManager.CountMiss(block);
					HideAndFreeTouchedBlock(block);
					i -= 1;
					continue;
				}

				if (ticks < start) {
					// in cache period
					float t = Mathf.Clamp01(Es.Calc(blockShowEsType, 1 - (start - ticks) / cacheTicks));
					block.skin.rect.eulerAngles = new Vector3(0, 0, 10f * (1f - t));
					block.skin.rect.localScale = Vector3.one * blockScaling * t;
				} else { // in grace period
					float t = Mathf.Clamp01(Es.Calc(blockHideEsType, 1 - (ticks - start) / graceTicks));
					block.skin.rect.eulerAngles = new Vector3(0, 0, -30f * (1f - t));
					block.skin.rect.localScale = Vector3.one * blockScaling * t;
				}
			}

			for (int i = 0; i < connectContainer.firstFreeItemIndex; i++) {
				var connect = connectContainer.itemList[i];

				if (connect.startTick <= ticks && connect.endTick <= ticks) {
					FreeSkin(connect.skinName, connect.skin);
					connectContainer.FreeItem(connect);
					i -= 1;
					continue;
				}

				if (ticks < connect.startTick) {
					// in cache period
					float t = Mathf.Clamp01(Es.Calc(blockShowEsType, 1 - (connect.startTick - ticks) / cacheTicks));
					//connect.skin.rect.eulerAngles = new Vector3(0, 0, 10f * (1f - t));
					connect.skin.rect.localScale = Vector3.one * blockScaling * t;
				} else { // in grace period
					float t = Mathf.Clamp01(Es.Calc(blockHideEsType, 1 - (ticks - connect.startTick) / graceTicks));
					//connect.skin.rect.eulerAngles = new Vector3(0, 0, -30f * (1f - t));
					connect.skin.rect.localScale = Vector3.one * blockScaling * t;
				}
			}

			ScannerY = GetY(midiSequencer.ticks);
		}

		//protected override void UpdateBlocks(List<Block> blocks, ref int freeStartIndex) {
		//	float ticks = midiSequencer.ticks;

		//	for (int i = 0; i < freeStartIndex; i++) {
		//		var block = blocks[i];
		//		float start = block.note.start;
		//		if (start <= ticks - graceTicks) {
		//			// miss
		//			scoringManager.CountMiss(block);
		//			HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
		//			i -= 1;
		//		} else {
		//			if (ticks < start) {
		//				// in cache period
		//				float t = Mathf.Clamp01(Es.Calc(blockShowEsType, 1 - (start - ticks) / cacheTicks));
		//				block.rect.eulerAngles = new Vector3(0, 0, 10f * (1f - t));
		//				block.rect.localScale = Vector3.one * blockScaling * t;
		//			} else { // in grace period
		//				float t = Mathf.Clamp01(Es.Calc(blockHideEsType, 1 - (ticks - start) / graceTicks));
		//				block.rect.eulerAngles = new Vector3(0, 0, -30f * (1f - t));
		//				block.rect.localScale = Vector3.one * blockScaling * t;
		//			}
		//		}
		//	}
		//}
	}
}
