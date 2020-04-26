using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Midif.V3;
using Uif;
using Systemf;

namespace TouhouMix.Levels.Gameplay {
	[System.Serializable]
	public class OneOnlyGameplayManagerV2 : IGameplayManager {
		const string BLOCK_INSTANT = "Instant";
		const string BLOCK_INSTANT_INNER = "InstantInner";
		const string BLOCK_INSTANT_CONNECT = "InstantConnect";
		const string BLOCK_SHORT = "Short";
		const string BLOCK_SHORT_CONNECT = "ShortConnect";
		const string BLOCK_LONG_START = "LongStart";
		const string BLOCK_LONG_FILL = "LongFill";
		const string BLOCK_LONG_END = "LongEnd";

		const string BLOCK_INSTANT_FILENAME = "instant.png";
		const string BLOCK_INSTANT_INNER_FILENAME = "instant-i.png";
		const string BLOCK_INSTANT_CONNECT_FILENAME = "instant-c.png";
		const string BLOCK_SHORT_FILENAME = "short.png";
		const string BLOCK_SHORT_CONNECT_FILENAME = "short-c.png";
		const string BLOCK_LONG_START_FILENAME = "long-b.png";
		const string BLOCK_LONG_FILL_FILENAME = "long-f.png";
		const string BLOCK_LONG_END_FILENAME = "long-t.png";

		const string CUSTOM_SKIN_ROOT_PATH = "Skins";
		const string BUILTIN_SKIN_ROOT_PATH = "Blocks";

		public bool loadCustomSkin;
		public string customSkinPath;
		public GameObject customSkinTemplatePrefab;

		public string skinPrefabPath = "Squre45";
		public readonly Dictionary<string, GameObject> skinPrefabDict = new Dictionary<string, GameObject>();
		protected readonly Dictionary<string, ActiveFreeContainer<BlockSkinController>> freeSkinContainerDict = new Dictionary<string, ActiveFreeContainer<BlockSkinController>>();

		public RectTransform activeBlockRect;
		public GameObject emptyBlock;

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

		[Space]
		public Color instantColor = Color.red;
		public Color shortColor = Color.red;
		public Color longColor = Color.red;

		protected GameplayLevelScheduler level;

		public readonly SingleLaneBlockGenerator generator = new SingleLaneBlockGenerator();
		protected List<SingleLaneBlockGenerator.BlockInfo> blockInfos;
		protected int blockInfoIndex;

		public virtual void Init(GameplayLevelScheduler level) {
			this.level = level;

			if (loadCustomSkin) {
				string skinPath = System.IO.Path.Combine(Application.persistentDataPath, CUSTOM_SKIN_ROOT_PATH, customSkinPath);
				float pixelsPerUnit = LoadCustomSkin(skinPath, BLOCK_SHORT_FILENAME, BLOCK_SHORT);
				LoadCustomSkin(skinPath, BLOCK_SHORT_CONNECT_FILENAME, BLOCK_SHORT_CONNECT, pixelsPerUnit, true);
				LoadCustomSkin(skinPath, BLOCK_INSTANT_FILENAME, BLOCK_INSTANT, pixelsPerUnit);
				LoadCustomSkin(skinPath, BLOCK_INSTANT_INNER_FILENAME, BLOCK_INSTANT_INNER, pixelsPerUnit);
				LoadCustomSkin(skinPath, BLOCK_INSTANT_CONNECT_FILENAME, BLOCK_INSTANT_CONNECT, pixelsPerUnit, true);
				LoadCustomSkin(skinPath, BLOCK_LONG_START_FILENAME, BLOCK_LONG_START, pixelsPerUnit);
				LoadCustomSkin(skinPath, BLOCK_LONG_FILL_FILENAME, BLOCK_LONG_FILL, pixelsPerUnit, true);
				LoadCustomSkin(skinPath, BLOCK_LONG_END_FILENAME, BLOCK_LONG_END, pixelsPerUnit);
			} else {
				string[] skinNames = new string[] {
					BLOCK_SHORT,
					BLOCK_SHORT_CONNECT,
					BLOCK_INSTANT,
					BLOCK_INSTANT_INNER,
					BLOCK_INSTANT_CONNECT,
					BLOCK_LONG_START,
					BLOCK_LONG_FILL,
					BLOCK_LONG_END,
				};
				foreach (var name in skinNames) {
					var prefab = Resources.Load<GameObject>(BUILTIN_SKIN_ROOT_PATH + "/" + skinPrefabPath + "/" + name);
					LoadSkin(name, prefab ?? emptyBlock);
				}
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

		protected float LoadCustomSkin(string root, string fileName, string skinName, float pixelsPerUnit = -1, bool canStrech = false) {
			string path = System.IO.Path.Combine(root, fileName);
			if (System.IO.File.Exists(path)) {
				byte[] bytes = System.IO.File.ReadAllBytes(path);
				var texture = new Texture2D(4, 4);
				texture.filterMode = FilterMode.Point;
				texture.LoadImage(bytes);
				if (pixelsPerUnit < 0) {
					pixelsPerUnit = 100f / texture.width;
				}

				var instance = Object.Instantiate(customSkinTemplatePrefab, activeBlockRect, false);
				instance.name = fileName;
				instance.GetComponent<CanvasGroup>().alpha = 0;

				var image = instance.GetComponentInChildren<RawImage>();
				image.texture = texture;
				var imageRect = image.GetComponent<RectTransform>();
				imageRect.sizeDelta = new Vector2(texture.width * pixelsPerUnit, texture.height * pixelsPerUnit);

				if (canStrech) {
					instance.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
					imageRect.anchorMax = new Vector2(.5f, 1);
					imageRect.anchorMin = new Vector2(.5f, 0);
					imageRect.offsetMax = Vector2.zero;
					imageRect.offsetMin = Vector2.zero;
					imageRect.sizeDelta = new Vector2(texture.width * pixelsPerUnit, 0);
					var skin = instance.GetComponent<BlockSkinController>();
					skin.rawImage = image;
					skin.pixelsPerUnit = pixelsPerUnit;
					texture.wrapMode = TextureWrapMode.Repeat;
				} else {
					texture.wrapMode = TextureWrapMode.Mirror;
				}

				LoadSkin(skinName, instance);
			} else {
				LoadSkin(skinName, emptyBlock);
			}
			return pixelsPerUnit;
		}

		protected void LoadSkin(string skinName, GameObject prefab) {
			skinPrefabDict[skinName] = prefab;
			freeSkinContainerDict[skinName] = new ActiveFreeContainer<BlockSkinController>(() => {
				var instance = Object.Instantiate(prefab, activeBlockRect, false);
				return instance.GetComponent<BlockSkinController>();
			});
		}

		protected bool HasSkin(string skinName) {
			return skinPrefabDict.ContainsKey(skinName) && skinPrefabDict[skinName] != emptyBlock;
		}

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
		Block AddBlock(int lane, BlockType type, NoteSequenceCollection.Note note) {
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
				var block = AddBlock(blockInfo.lane, blockInfo.type, blockInfo.note);
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
									GenerateConnect(BLOCK_INSTANT_CONNECT, activeBlock, block, instantColor);
									isInner = true;
								}
								break;
							}
						}
					}
					block.skinName = isInner && HasSkin(BLOCK_INSTANT_INNER) ? BLOCK_INSTANT_INNER : BLOCK_INSTANT;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.skin.MoveToFront();
					block.skin.color.Set(instantColor);
				} else if (block.type == BlockType.SHORT) {
					block.skinName = BLOCK_SHORT;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.skin.MoveToFront();
					block.skin.color.Set(shortColor);
				} else {  // tentativeBlock.type == GameplayBlockType.Long
					block.longFillSkin = CreateOrReuseSkin(BLOCK_LONG_FILL);
					block.longFillSkin.Scale = new Vector3(blockScaling, 1, 1);
					block.longFillSkin.MoveToFront();
					block.longFillSkin.color.Set(longColor);

					block.longEndSkin = CreateOrReuseSkin(BLOCK_LONG_END);
					block.longEndSkin.Scale = Vector3.one * blockScaling;
					block.longEndSkin.MoveToFront();
					block.longEndSkin.color.Set(longColor);
					
					block.skinName = BLOCK_LONG_START;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.skin.MoveToFront();
					block.skin.color.Set(longColor);
				}
				block.skin.Scale = Vector3.one * blockScaling;
			}

			if (generateShortConnect && minXBlock != maxXBlock) {
				// Need to generate ShortConnect
				GenerateConnect(BLOCK_SHORT_CONNECT, minXBlock, maxXBlock, shortColor);
			}
		}

		protected virtual void GenerateConnect(string skinName, Block from, Block to, Color color) {
			if (!HasSkin(skinName)) {
				return;
			}

			var connect = connectContainer.CreateOrReuseItem();
			connect.isTentative = true;
			connect.skinName = skinName;
			connect.skin = CreateOrReuseSkin(skinName);
			connect.skin.Scale = new Vector3(blockScaling, 1, 1);
			connect.skin.color.Set(color);
			connect.startX = from.x;
			connect.startTick = from.note.start;
			connect.endX = to.x;
			connect.endTick = to.note.start;
			// Push to back
			connect.skin.MoveToBack();
		}

		#endregion

		#region Block Update

		public virtual void UpdateBlocks() {
			float ticks = level.midiSequencer.ticks;

			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				block.isTentative = false;

				int start = block.note.start;
				if ((block.type != BlockType.LONG && block.note.start < ticks - graceTicks) || 
						(block.type == BlockType.LONG && block.end < ticks - graceTicks)) {
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
						level.scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.end), block, true, true);
						level.StopNote(block.note);
						HideAndFreeTouchedBlock(block);
					} else {
						float startY = block.holdingFingerId != -1 ? judgeHeight : GetY(start);
						float endY = GetY(end);
						float blockX = block.holdingFingerId != -1 ? block.holdingX : block.x;
						block.skin.Pos = new Vector2(blockX, startY);
						block.longFillSkin.Pos = new Vector2(blockX, startY);
						block.longFillSkin.Size = new Vector2(100, endY - startY);
						block.longEndSkin.Pos = new Vector2(blockX, endY);
					}
				} else {
					block.skin.Pos = new Vector2(block.x, GetY(block.note.start));
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
				connect.skin.Pos = new Vector2(connect.startX, startY);
				float endY = GetY(connect.endTick);
				float length = Vector2.Distance(new Vector2(connect.startX, startY), new Vector2(connect.endX, endY));
				connect.skin.Size = new Vector2(connect.skin.Size.x, length);
				connect.skin.Rot = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(connect.startX - connect.endX, endY - startY));
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
			Block holdingBlock;
			if (!holdingBlockDict.TryGetValue(id, out holdingBlock)) {
				return;
			}
			holdingBlock.holdingFingerId = -1;
			holdingBlockDict.Remove(id);
			HideAndFreeTouchedBlock(holdingBlock);
			//Debug.Log("Up " + holdingBlock.end + " " + GetOffsetInSeconds(holdingBlock.end));
			level.scoringManager.CountScoreForBlock(GetOffsetInSeconds(holdingBlock.end), holdingBlock, false, true);

			// Only check instant block when ending long block
			//CheckAllInstantBlocks(false);
		}

		public void ProcessTouchHold(int id, float x, float y) {
			CheckAllInstantBlocks(x, true);

			Block holdingBlock;
			if (!holdingBlockDict.TryGetValue(id, out holdingBlock)) {
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
			Block holdingBlock;
			if (holdingBlockDict.TryGetValue(fingerId, out holdingBlock)) {
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

		float GetOffsetInSeconds(float targetTicks) {
			float timing = targetTicks - level.midiSequencer.ticks;
			float seconds = level.midiSequencer.ToSeconds(timing) - judgeTimeOffset;
			return seconds;
		}

		#endregion

		public sealed class Connect : IIndexable {
			public bool isTentative;

			public int index;

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
