using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif;
using Systemf;

namespace TouhouMix.Levels.Gameplay {
	[System.Serializable]
	public class OneOnlyGameplayManagerV2 : IGameplayManager {
		const string BLOCK_SHORT = "Short";
		const string BLOCK_SHORT_CONNECT = "ShortConnect";
		const string BLOCK_INSTANT = "Instant";
		const string BLOCK_INSTANT_INNER = "InstantInner";
		const string BLOCK_INSTANT_CONNECT = "InstantConnect";
		const string BLOCK_LONG_START = "LongStart";
		const string BLOCK_LONG_FILL = "LongFill";
		const string BLOCK_LONG_INNER = "LongInner";
		const string BLOCK_LONG_END = "LongEnd";

		public string skinPrefabPath = "Blocks/V2/Squre45/";
		public readonly Dictionary<string, GameObject> skinPrefabDict = new Dictionary<string, GameObject>();
		protected readonly Dictionary<string, ActiveFreeContainer<BlockSkinController>> freeSkinContainerDict = new Dictionary<string, ActiveFreeContainer<BlockSkinController>>();

		public RectTransform activeBlockRect;

		protected readonly ActiveFreeContainer<Block> blockContainer = new ActiveFreeContainer<Block>(() => new Block());
		protected readonly ActiveFreeContainer<Connect> connectContainer = new ActiveFreeContainer<Connect>(() => new Connect());
		protected readonly Dictionary<int, Block> holdingBlockDict = new Dictionary<int, Block>();

		public bool generateShortConnect = true;
		public bool generateInstantConnect = true;
		public float maxInstantConnectSeconds = .5f;

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
		public float judgeTimeOffset = 0;
		public int laneCount = 12;
		public float blockWidth = 100;
		public float blockJudgingWidth = 120;
		protected float blockScaling;
		protected float blockJudgingHalfWidth;

		protected GameplayLevelScheduler level;

		public readonly SingleLaneBlockGenerator generator = new SingleLaneBlockGenerator();
		protected List<SingleLaneBlockGenerator.BlockInfo> blockInfos;
		protected int blockInfoIndex;

		public virtual void Init(GameplayLevelScheduler level) {
			this.level = level;

			string[] skinNames = new string[] {
				BLOCK_SHORT,
				BLOCK_SHORT_CONNECT,
				BLOCK_INSTANT,
				BLOCK_INSTANT_INNER,
				BLOCK_INSTANT_CONNECT,
				BLOCK_LONG_START,
				BLOCK_LONG_END,
				BLOCK_LONG_FILL,
				BLOCK_LONG_INNER,
			};
			foreach (var name in skinNames) {
				var prefab = Resources.Load<GameObject>(skinPrefabPath + name);
				skinPrefabDict[name] = prefab;
				freeSkinContainerDict[name] = new ActiveFreeContainer<BlockSkinController>(() => {
					var instance = Object.Instantiate(prefab, activeBlockRect, false);
					return instance.GetComponent<BlockSkinController>();
				});
			}

			var canvasSize = level.sizeWatcher.canvasSize;
			judgeRect.anchoredPosition = new Vector2(0, judgeHeight);
			judgeRect.sizeDelta = new Vector2(0, judgeThickness);
			cacheHeight = canvasSize.y - judgeHeight;

			blockScaling = blockWidth / 100;
			float canvasWidth = canvasSize.x;
			float[] laneX = new float[laneCount];
			float laneStart = blockWidth * .5f;
			float laneSpacing = (canvasWidth - blockWidth) / (laneCount - 1);
			for (int i = 0; i < laneX.Length; i++) {
				laneX[i] = laneStart + i * laneSpacing;
			}
			generator.laneCount = laneCount;
			generator.laneX = laneX;

			cacheTicks = cacheBeats * level.midiFile.ticksPerBeat;
			graceTicks = graceBeats * level.midiFile.ticksPerBeat;

			blockJudgingHalfWidth = blockJudgingWidth * .5f;

			level.midiSequencer.ticks = -cacheTicks;

			blockInfos = generator.GenerateBlocks(level.gameSequences);
			level.backgroundSequences.Add(new NoteSequenceCollection.Sequence { notes = generator.backgroundNotes });
			blockInfoIndex = 0;
		}

		#region Skin Management

		protected BlockSkinController CreateOrReuseSkin(string skinName) {
			var skin = freeSkinContainerDict[skinName].CreateOrReuseItem();
			skin.group.alpha = 1;
			return skin;
		}

		protected void FreeSkin(string skinName, BlockSkinController skin) {
			freeSkinContainerDict[skinName].FreeItem(skin);
			skin.group.alpha = 0;
		}

		#endregion

		#region Block Generation
		Block AddTentativeBlock(int lane, BlockType type, NoteSequenceCollection.Note note) {
			var block = blockContainer.CreateOrReuseItem();
			block.lane = lane;
			block.isTentative = true;
			block.active = true;
			block.type = type;
			block.note = note;
			block.end = note.end;
			block.holdingFingerId = -1;
			return block;
		}

		public virtual void GenerateBlocks() {
			if (blockInfoIndex >= blockInfos.Count || level.midiSequencer.ticks + cacheTicks < blockInfos[blockInfoIndex].note.start) {
				return;
			}

			Block minXBlock = null;
			Block maxXBlock = null;
			// Generate one batch at a time
			for (int batch = blockInfos[blockInfoIndex].batch; blockInfoIndex < blockInfos.Count && blockInfos[blockInfoIndex].batch == batch; blockInfoIndex += 1) {
				var blockInfo = blockInfos[blockInfoIndex];
				var block = AddTentativeBlock(blockInfo.lane, blockInfo.type, blockInfo.note);
				block.batch = blockInfo.batch;
				block.x = blockInfo.x;

				// Record minX and maxX for possibl ShortConnect
				if (minXBlock == null || block.x < minXBlock.x) {
					minXBlock = block;
				}
				if (maxXBlock == null || block.x > maxXBlock.x) {
					maxXBlock = block;
				}

				if (block.type == BlockType.INSTANT) {
					bool isInner = false;
					var prevInfo = blockInfo.prev;
					if (prevInfo != null && generateInstantConnect) {
						// Need to generate InstantConnect
						for (int i = blockContainer.firstFreeItemIndex - 1; i >= 0; i--) {
							var activeBlock = blockContainer.itemList[i];
							if (activeBlock.batch == prevInfo.batch && activeBlock.lane == prevInfo.lane) {
								if (activeBlock.type == BlockType.INSTANT && block.note.startSeconds - activeBlock.note.startSeconds <= maxInstantConnectSeconds) {
									GenerateConnect(BLOCK_INSTANT_CONNECT, activeBlock, block, false);
									isInner = true;
								}
								break;
							}
						}
					}
					block.skinName = isInner ? BLOCK_INSTANT_INNER : BLOCK_INSTANT;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.skin.rect.SetAsLastSibling();
				} else if (block.type == BlockType.SHORT) {
					block.skinName = BLOCK_SHORT;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.skin.rect.SetAsLastSibling();
				} else {  // tentativeBlock.type == GameplayBlockType.Long
					block.skinName = BLOCK_LONG_START;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.longFillSkin = CreateOrReuseSkin(BLOCK_LONG_FILL);
					block.longEndSkin = CreateOrReuseSkin(BLOCK_LONG_END);
					block.longFillSkin.rect.localScale = new Vector3(blockScaling, 1, 1);
					block.longEndSkin.rect.localScale = Vector3.one * blockScaling;

					block.longFillSkin.rect.SetAsLastSibling();
					block.longEndSkin.rect.SetAsLastSibling();
					block.skin.rect.SetAsLastSibling();
				}

				block.skin.rect.localScale = Vector3.one * blockScaling;
			}

			if (generateShortConnect && minXBlock != maxXBlock) {
				// Need to generate ShortConnect
				GenerateConnect(BLOCK_SHORT_CONNECT, minXBlock, maxXBlock, true);
			}
		}

		protected virtual void GenerateConnect(string skinName, Block from, Block to, bool isFixed) {
			var connect = connectContainer.CreateOrReuseItem();
			connect.isTentative = true;
			//connect.isFixed = isFixed;
			connect.isFixed = false;
			connect.skinName = skinName;
			connect.skin = CreateOrReuseSkin(skinName);
			connect.skin.rect.localScale = new Vector3(blockScaling, 1, 1);
			connect.startX = from.x;
			connect.startTick = from.note.start;
			connect.endX = to.x;
			connect.endTick = to.note.start;
			//if (isFixed) {
			//	// Calc the length and angle once
			//	float startY = GetY(connect.startTick);
			//	float endY = GetY(connect.endTick);
			//	float length = Vector2.Distance(new Vector2(connect.startX, startY), new Vector2(connect.endX, endY));
			//	connect.skin.rect.sizeDelta = new Vector2(connect.skin.rect.sizeDelta.x, length);
			//	connect.skin.rect.eulerAngles = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(connect.startX - connect.endX, endY - startY));
			//}
			// Push to back
			connect.skin.rect.SetAsFirstSibling();
		}

		#endregion

		#region Block Update

		public virtual void UpdateBlocks() {
			float ticks = level.midiSequencer.ticks;

			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				block.isTentative = false;

				int start = block.note.start;
				if (block.end < ticks - graceTicks) {
					// miss
					level.scoringManager.CountMiss();
					HideAndFreeTouchedBlock(block);
					i -= 1;
					continue;
				}

				if (block.type == BlockType.LONG) {
					float end = block.end;
					if (block.holdingFingerId != -1 && end <= ticks) {
						// hold finish
						holdingBlockDict.Remove(block.holdingFingerId);
						block.holdingFingerId = -1;
						level.scoringManager.CountScoreForLongBlockTail(GetOffsetInSeconds(block.end), block, true);
						level.StopNote(block.note);
						HideAndFreeTouchedBlock(block);
					} else {
						float startY = block.holdingFingerId != -1 ? judgeHeight : GetY(start);
						float endY = GetY(end);
						float blockX = block.holdingFingerId != -1 ? block.holdingX : block.x;
						block.skin.rect.anchoredPosition = new Vector2(blockX, startY);
						block.longFillSkin.rect.anchoredPosition = new Vector2(blockX, startY);
						block.longFillSkin.rect.sizeDelta = new Vector2(0, endY - startY);
						block.longEndSkin.rect.anchoredPosition = new Vector2(blockX, endY);
					}
				} else {
					block.skin.rect.anchoredPosition = new Vector2(block.x, GetY(block.note.start));
				}
			}

			for (int i = 0; i < connectContainer.firstFreeItemIndex; i++) {
				var connect = connectContainer.itemList[i];
				connect.isTentative = false;

				if (connect.startTick <= ticks && connect.endTick <= ticks) {
					FreeSkin(connect.skinName, connect.skin);
					connectContainer.FreeItem(connect);
					i -= 1;
					continue;
				}

				float startY = GetY(connect.startTick);
				connect.skin.rect.anchoredPosition = new Vector2(connect.startX, startY);
				if (!connect.isFixed) {
					float endY = GetY(connect.endTick);
					float length = Vector2.Distance(new Vector2(connect.startX, startY), new Vector2(connect.endX, endY));
					connect.skin.rect.sizeDelta = new Vector2(connect.skin.rect.sizeDelta.x, length);
					connect.skin.rect.eulerAngles = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(connect.startX - connect.endX, endY - startY));
				}
			}
		}

		protected virtual float GetY(float start) {
			float ticks = level.midiSequencer.ticks;
			if (ticks < start) {
				// in cache period
				return Es.Calc(cacheEsType, Mathf.Clamp01((start - ticks) / cacheTicks)) * cacheHeight + judgeHeight;
			} else { // start <= ticks
							 // in grace period
				return judgeHeight - judgeHeight * Es.Calc(graceEsType, Mathf.Clamp01((ticks - start) / graceTicks));
			}
		}

		#endregion

		#region Touch

		public void ProcessTouchDown(int id, float x, float y) {
			int bestBlockIndex = -1;
			Block bestBlock = null;
			float bestTimingDiff = -1;
			float bestOffset = -1;
			FindBestNote(x, ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);

			if (bestBlock == null) return;

			switch (bestBlock.type) {
				case BlockType.INSTANT: TouchInstantBlock(bestBlock, bestBlockIndex); break;
				case BlockType.SHORT: TouchShortBlock(bestBlock, bestBlockIndex); break;
				case BlockType.LONG: TouchLongBlock(bestBlock, bestBlockIndex, id, x); break;
			}
		}

		public void ProcessTouchUp(int id, float x, float y) {
			if (!holdingBlockDict.TryGetValue(id, out Block holdingBlock)) {
				return;
			}
			holdingBlock.holdingFingerId = -1;
			holdingBlockDict.Remove(id);
			HideAndFreeTouchedBlock(holdingBlock);
			//Debug.Log("Up " + holdingBlock.end + " " + GetOffsetInSeconds(holdingBlock.end));
			level.scoringManager.CountScoreForLongBlockTail(GetOffsetInSeconds(holdingBlock.end), holdingBlock);

			// Only check instant block when ending long block
			//CheckAllInstantBlocks(false);
		}

		public void ProcessTouchHold(int id, float x, float y) {
			CheckAllInstantBlocks(x, true);

			if (!holdingBlockDict.TryGetValue(id, out Block holdingBlock)) {
				return;
			}
			holdingBlock.holdingX = x + holdingBlock.holdingOffset;
		}

		bool CheckAllInstantBlocks(float x, bool onlyCheckOverdue) {
			bool isInstantBlockTouched = false;
			float perfectTiming = level.scoringManager.perfectTiming;
			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				if (block.type != BlockType.INSTANT) {
					continue;
				}
				//if (!touchedLaneSet.Contains(block.lane)) continue;
				if (!(block.x - blockJudgingHalfWidth <= x && x <= block.x + blockJudgingHalfWidth)) {
					continue;
				}

				float tickDiff = level.midiSequencer.ticks - block.note.start;
				float timingDiff = level.midiSequencer.ToSeconds(tickDiff);
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

		void FindBestNote(float x, ref int bestBlockIndex, ref Block bestBlock, ref float bestTimingDiff, ref float bestOffset) {
			float badTiming = level.scoringManager.badTiming;
			//Debug.Log("check " + blockContainer.firstFreeItemIndex);
			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				//if (!touchedLaneSet.Contains(block.lane)) continue;
				if (!(block.x - blockJudgingHalfWidth <= x && x <= block.x + blockJudgingHalfWidth)) {
					continue;
				}
				float tickDiff = level.midiSequencer.ticks - block.note.start;
				float timeingDiff = level.midiSequencer.ToSeconds(tickDiff);
				timeingDiff = timeingDiff < 0 ? -timeingDiff : timeingDiff;
				float offset = block.x - x;
				offset = offset < 0 ? -offset : offset;
				//Debug.LogFormat("compare best note tick {2}, time {0}, offset {1}", timeingDiff, offset, tickDiff);
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
			//block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block);
			level.scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block, isHolding);
			level.PlayBackgroundNote(block.note);
		}

		void TouchShortBlock(Block block, int index) {
			//block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block);
			level.scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block);
			level.PlayBackgroundNote(block.note);
		}

		void TouchLongBlock(Block block, int _, int fingerId, float x) {
			block.holdingFingerId = fingerId;
			block.holdingOffset = block.x - x;
			block.holdingX = block.x;
			if (holdingBlockDict.TryGetValue(fingerId, out Block holdingBlock)) {
				level.scoringManager.CountMiss();
				HideAndFreeTouchedBlock(holdingBlock);
			}
			holdingBlockDict[fingerId] = block;
			level.scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block);
			level.PlayBackgroundNote(block.note);
			level.StartNote(block.note);
		}

		protected void HideAndFreeTouchedBlock(Block block) {
			FreeSkin(block.skinName, block.skin);
			if (block.type == BlockType.LONG) {
				FreeSkin(BLOCK_LONG_FILL, block.longFillSkin);
				FreeSkin(BLOCK_LONG_END, block.longEndSkin);
			}
			blockContainer.FreeItem(block);
		}

		float GetOffsetInSeconds(float timingTicks) {
			float timing = level.midiSequencer.ticks - timingTicks;
			float seconds = level.midiSequencer.ToSeconds(timing) + judgeTimeOffset;
			if (seconds < 0) seconds = -seconds;
			return seconds;
		}

		#endregion

		public sealed class Connect : IIndexable {
			public bool isTentative;

			public int index;

			public bool isFixed;
			public string skinName;
			public BlockSkinController skin;
			public float startX;
			public float startTick;
			public float endX;
			public float endTick;

			public int Index {
				get { return index; }
				set { index = value; }
			}
		}
	}
}
