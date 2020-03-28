using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif;
using Uif.Settables;
using Uif.Settables.Components;
using Sirenix.OdinInspector;
using Systemf;
using System.Linq;

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
		public RectTransform freeBlockRect;

		protected readonly ActiveFreeContainer<Block> blockContainer = new ActiveFreeContainer<Block>(() => new Block());
		// For instant connect
		protected readonly OrderedActiveQueue<Block> instantBlockQueue = new OrderedActiveQueue<Block>();

		protected readonly ActiveFreeContainer<Connect> connectContainer = new ActiveFreeContainer<Connect>(() => new Connect());
		protected readonly Dictionary<int, Block> tentativeBlockDict = new Dictionary<int, Block>();
		protected readonly Dictionary<int, Block> holdingBlockDict = new Dictionary<int, Block>();
		protected readonly HashSet<int> touchedLaneSet = new HashSet<int>();

		public float maxInstantBlockSeconds = .2f;
		public float maxShortBlockSeconds = .8f;
		public float maxInstantConnectSeconds = .5f;
		public float maxInstantConnectX = 200;

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
		protected float blockScaling;
		protected float[] laneXDict;
		protected float blockJudgingHalfWidth;

		protected IGameplayHost host;
		protected ScoringManager scoringManager;
		protected MidiFile midiFile;
		protected MidiSequencer midiSequencer;

		public virtual void Init(IGameplayHost host) {
			this.host = host;

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

			scoringManager = host.GetScoringManager();
			midiFile = host.GetMidiFile();
			midiSequencer = host.GetMidiSequencer();

			var canvasSize = host.GetCanvasSize();

			judgeRect.anchoredPosition = new Vector2(0, judgeHeight);
			judgeRect.sizeDelta = new Vector2(0, judgeThickness);
			cacheHeight = canvasSize.y - judgeHeight;

			blockScaling = blockWidth / 100;
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

		#region Skin Management

		BlockSkinController CreateOrReuseSkin(string skinName) {
			var skin = freeSkinContainerDict[skinName].CreateOrReuseItem();
			//skin.rect.SetParent(activeBlockRect, false);
			skin.group.alpha = 1;
			// Push to front
			skin.rect.SetAsLastSibling();
			return skin;
		}

		void FreeSkin(string skinName, BlockSkinController skin) {
			freeSkinContainerDict[skinName].FreeItem(skin);
			//skin.rect.SetParent(freeBlockRect, false);
			skin.group.alpha = 0;
		}

		#endregion

		#region Block Generation
		public virtual void AddTentativeBlock(NoteSequenceCollection.Note note) {
			//			Debug.LogFormat("tentative ch{0} n{1} {2} {3}", note.channel, note.note, note.start, note.duration);
			Block overlappingLongBlock = null;
			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				if (block.type == GameplayBlock.BlockType.Long && note.start <= block.end) {
					//					Debug.Log("Overlap!");
					overlappingLongBlock = block;
					break;
				}
			}

			//float durationInSeconds = (float)note.duration / midiFile.ticksPerBeat / midiSequencer.beatsPerSecond;
			float durationInSeconds = midiSequencer.ToSeconds(note.duration);
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
			var block = blockContainer.CreateOrReuseItem();
			block.isTentative = true;
			block.active = true;
			block.type = type;
			block.note = note;
			block.end = note.end;
			block.backgroundNotes.Clear();
			block.holdingFingerId = -1;
			tentativeBlockDict.Add(lane, block);
		}

		void OverrideTentativeBlock(Block block, GameplayBlock.BlockType newType, NoteSequenceCollection.Note newNote) {
			block.type = newType;
			block.backgroundNotes.Add(block.note);
			block.note = newNote;
		}

		public virtual void GenerateBlocks() {
			Block minXBlock = null;
			Block maxXBlock = null;
			foreach (var pair in tentativeBlockDict) {
				int lane = pair.Key;
				var block = pair.Value;
				block.lane = lane;
				block.x = laneXDict[lane];

				// Record minX and maxX for possibl ShortConnect
				if (minXBlock == null || block.x < minXBlock.x) {
					minXBlock = block;
				}
				if (maxXBlock == null || block.x > maxXBlock.x) {
					maxXBlock = block;
				}

				if (block.type == GameplayBlock.BlockType.Instant) {
					// Need to generate InstantConnect
					bool isInner = false;
					foreach (var activeBlock in instantBlockQueue.itemQueue.Reverse()) {
						if (!activeBlock.isTentative) {
							if (midiSequencer.ToSeconds(block.note.start - activeBlock.note.start) < maxInstantConnectSeconds && Mathf.Abs(activeBlock.x - block.x) < maxInstantConnectX) {
								isInner = true;
								GenerateConnect(BLOCK_INSTANT_CONNECT, activeBlock, block, false);
								break;
							}
						}
					}

					instantBlockQueue.Push(block);

					block.skinName = isInner ? BLOCK_INSTANT_INNER : BLOCK_INSTANT;
					block.skin = CreateOrReuseSkin(block.skinName);
				} else if (block.type == GameplayBlock.BlockType.Short) {
					block.skinName = BLOCK_SHORT;
					block.skin = CreateOrReuseSkin(block.skinName);
				} else {  // tentativeBlock.type == GameplayBlock.BlockType.Long
					block.skinName = BLOCK_LONG_START;
					block.skin = CreateOrReuseSkin(block.skinName);
					block.longFillSkin = CreateOrReuseSkin(BLOCK_LONG_FILL);
					block.longEndSkin = CreateOrReuseSkin(BLOCK_LONG_END);
					block.longFillSkin.rect.localScale = new Vector3(blockScaling, 1, 1);
					block.longEndSkin.rect.localScale = Vector3.one * blockScaling;
				}

				block.skin.rect.localScale = Vector3.one * blockScaling;
				block.rect = block.skin.rect;
			}

			if (tentativeBlockDict.Count > 1) {
				// Need to generate ShortConnect
				GenerateConnect(BLOCK_SHORT_CONNECT, minXBlock, maxXBlock, true);
			}

			tentativeBlockDict.Clear();
		}

		void GenerateConnect(string skinName, Block from, Block to, bool isFixed) {
			var connect = connectContainer.CreateOrReuseItem();
			connect.isFixed = isFixed;
			connect.skinName = skinName;
			connect.skin = CreateOrReuseSkin(skinName);
			connect.skin.rect.localScale = new Vector3(blockScaling, 1, 1);
			connect.startX = from.x;
			connect.startTick = from.note.start;
			connect.endX = to.x;
			connect.endTick = to.note.start;
			if (isFixed) {
				// Calc the length and angle once
				float startY = GetY(connect.startTick);
				float endY = GetY(connect.endTick);
				float length = Vector2.Distance(new Vector2(connect.startX, startY), new Vector2(connect.endX, endY));
				connect.skin.rect.sizeDelta = new Vector2(connect.skin.rect.sizeDelta.x, length);
				connect.skin.rect.eulerAngles = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(connect.startX - connect.endX, endY - startY));
			}
			// Push to back
			connect.skin.rect.SetAsFirstSibling();
		}

		#endregion

		#region Touch

		public void ProcessTouchDown(int id, float x, float y) {
			MarkTouchedLanes(x, y);

			int bestBlockIndex = -1;
			Block bestBlock = null;
			float bestTimingDiff = -1;
			float bestOffset = -1;
			FindBestNote(x, ref bestBlockIndex, ref bestBlock, ref bestTimingDiff, ref bestOffset);

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
			HideAndFreeTouchedBlock(holdingBlock);
			Debug.Log("Up " + holdingBlock.end + " " + GetOffsetInSeconds(holdingBlock.end));
			scoringManager.CountScoreForLongBlockTail(GetOffsetInSeconds(holdingBlock.end), holdingBlock);

			// Only check instant block when ending long block
			//CheckAllInstantBlocks(false);
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
			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				if (block.type != GameplayBlock.BlockType.Instant) {
					continue;
				}
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

		void FindBestNote(float x, ref int bestBlockIndex, ref Block bestBlock, ref float bestTimingDiff, ref float bestOffset) {
			float badTiming = scoringManager.badTiming;
			//Debug.Log("check " + blockContainer.firstFreeItemIndex);
			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				if (!touchedLaneSet.Contains(block.lane)) continue;
				float tickDiff = midiSequencer.ticks - block.note.start;
				float timeingDiff = midiSequencer.ToSeconds(tickDiff);
				timeingDiff = timeingDiff < 0 ? -timeingDiff : timeingDiff;
				float offset = laneXDict[block.lane] - x;
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
			scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block, isHolding);
			host.AddBackgroundNotes(block);
		}

		void TouchShortBlock(Block block, int index) {
			//block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block);
			scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block);
			host.AddBackgroundNotes(block);
		}

		void TouchLongBlock(Block block, int _, int fingerId, float x) {
			block.holdingFingerId = fingerId;
			block.holdingOffset = block.x - x;
			block.holdingX = block.x;
			if (holdingBlockDict.TryGetValue(fingerId, out Block holdingBlock)) {
				scoringManager.CountMiss(holdingBlock);
				HideAndFreeTouchedBlock(holdingBlock);
			}
			holdingBlockDict[fingerId] = block;
			scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block);
			host.AddBackgroundNotes(block);
			host.StartNote(block.note);
		}

		protected void HideAndFreeTouchedBlock(Block block) {
			FreeSkin(block.skinName, block.skin);
			if (block.type == GameplayBlock.BlockType.Long) {
				FreeSkin(BLOCK_LONG_FILL, block.longFillSkin);
				FreeSkin(BLOCK_LONG_END, block.longEndSkin);
			}
			if (block.type == GameplayBlock.BlockType.Instant) {
				instantBlockQueue.ForceFree(block);
			}
			blockContainer.FreeItem(block);
		}

		float GetOffsetInSeconds(float timingTicks) {
			float timing = midiSequencer.ticks - timingTicks;
			if (timing < 0) timing = -timing;
			return midiSequencer.ToSeconds(timing);
		}

		#endregion

		#region Block Update

		public virtual void UpdateBlocks() {
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				block.isTentative = false;
				int start = block.note.start;
				if (block.note.end < ticks - graceTicks) {
					// miss
					scoringManager.CountMiss(block);
					HideAndFreeTouchedBlock(block);
					i -= 1;
					continue;
				}

				if (block.type == GameplayBlock.BlockType.Long) {
					int end = block.note.end;
					if (block.holdingFingerId != -1 && end <= ticks) {
						// hold finish
						holdingBlockDict.Remove(block.holdingFingerId);
						block.holdingFingerId = -1;
						scoringManager.CountScoreForLongBlockTail(GetOffsetInSeconds(block.end), block, true);
						host.StopNote(block.note);
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
				if (connect.startTick <= ticks - graceTicks && connect.endTick <= ticks - graceTicks) {
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

		float GetY(float start) {
			float ticks = midiSequencer.ticks;
			if (ticks < start) {
				// in cache period
				return Es.Calc(cacheEsType, Mathf.Clamp01((start - ticks) / cacheTicks)) * cacheHeight + judgeHeight;
			} else { // start <= ticks
							 // in grace period
				return judgeHeight - judgeHeight * Es.Calc(graceEsType, Mathf.Clamp01((ticks - start) / graceTicks));
			}
		}

		#endregion

		public sealed class Block : GameplayBlock, IActiveCheckable, IIndexable {
			public bool active;
			public bool isTentative;

			new public int lane;
			new public float x;
			new public int index;
			public string skinName;
			public BlockSkinController skin;
			// For long block
			public BlockSkinController longFillSkin;
			public BlockSkinController longEndSkin;

			public bool Active {
				get { return active; }
			}

			public int Index {
				get { return index; }
				set { index = value; }
			}
		}

		public sealed class Connect : IIndexable {
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
