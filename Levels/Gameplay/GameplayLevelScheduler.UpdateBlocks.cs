﻿using System.Collections;
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
		void UpdateBlocks() {
			UpdateBlocks(instantBlocks, ref instantBlocksFreeStartIndex);
			UpdateBlocks(shortBlocks, ref shortBlocksFreeStartIndex);
			UpdateLongBlocks(longBlocks, ref longBlocksFreeStartIndex);
		}

		void UpdateBlocks(List<Block> blocks, ref int freeStartIndex) {
			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				float start = block.note.start;
				if (start <= ticks - graceTicks) {
					// miss
					HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
					i -= 1;
				} else {
					float y = GetY(start);
					block.rect.anchoredPosition = new Vector2(block.x, y);
				}
			}
		}

		void UpdateLongBlocks(List<Block> blocks, ref int freeStartIndex) {
			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				float start = block.note.start;
				float end = block.end;
				if (end <= ticks - graceTicks) {
					// miss
					HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
					i -= 1;
				} else if (block.holdingFingerId != -1 && end <= ticks) {
					// hold finish
					holdingBlockDict.Remove(block.holdingFingerId);
					block.holdingFingerId = -1;
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
			if (ticks < start) {
				// in cache period
				return Es.Calc(cacheEsType, (start - ticks) / cacheTicks) * cacheHeight + judgeHeight;
			} else { // start <= ticks
				// in grace period
				return judgeHeight - judgeHeight * Es.Calc(graceEsType, (ticks - start) / graceTicks);
			}
		}
	}
}