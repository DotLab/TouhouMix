using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using Uif;
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

		protected readonly ActiveFreeContainer<Block> blockContainer = new ActiveFreeContainer<Block>(() => new Block());

		// For fast generate instant connect
		protected readonly OrderedActiveQueue<Block> instantBlockQueue = new OrderedActiveQueue<Block>();
		protected readonly ActiveFreeContainer<Connect> connectContainer = new ActiveFreeContainer<Connect>(() => new Connect());

		protected readonly Dictionary<int, Block> holdingBlockDict = new Dictionary<int, Block>();
		protected readonly HashSet<int> touchedLaneSet = new HashSet<int>();

		public int maxSimultaneousBlocks = 2;
		public bool generateShortConnect = true;
		public bool generateInstantConnect = true;
		public bool generateInstantConnectMesh = false;

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

			cacheTicks = cacheBeats * midiSequencer.file.ticksPerBeat;
			graceTicks = graceBeats * midiSequencer.file.ticksPerBeat;

			blockJudgingHalfWidth = blockJudgingWidth * .5f;

			midiSequencer.ticks = -cacheTicks;
		}

		#region Skin Management

		protected BlockSkinController CreateOrReuseSkin(string skinName) {
			var skin = freeSkinContainerDict[skinName].CreateOrReuseItem();
			//skin.rect.SetParent(activeBlockRect, false);
			skin.group.alpha = 1;
			return skin;
		}

		protected void FreeSkin(string skinName, BlockSkinController skin) {
			freeSkinContainerDict[skinName].FreeItem(skin);
			//skin.rect.SetParent(freeBlockRect, false);
			skin.group.alpha = 0;
		}

		#endregion

		#region Block Generation
		protected readonly Dictionary<int, Block> tentativeBlockDict = new Dictionary<int, Block>();
		protected readonly List<NoteSequenceCollection.Note> tentativeNotes = new List<NoteSequenceCollection.Note>();

		public virtual void AddTentativeNote(NoteSequenceCollection.Note note) {
			tentativeNotes.Add(note);
		}

		void AddTentativeInstantBlock(NoteSequenceCollection.Note note) {
			//			Debug.Log("tentative instant");
			int lane = note.note % laneCount;
			if (tentativeBlockDict.TryGetValue(lane, out Block existingBlock)) {
				// if has exising block, append as background note
				existingBlock.backgroundNotes.Add(note);
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, BlockType.INSTANT, note);
			}
		}

		void AddTentativeShortBlock(NoteSequenceCollection.Note note) {
			//			Debug.Log("tentative short");
			int lane = note.note % laneCount;
			if (tentativeBlockDict.TryGetValue(lane, out Block existingBlock)) {
				if (existingBlock.type == BlockType.INSTANT) {
					// if has existing instant block, override the instant block
					OverrideTentativeBlock(existingBlock, BlockType.SHORT, note);
				} else {
					// if short or long, append as background note
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, BlockType.SHORT, note);
			}
		}

		void AddTentativeLongBlock(NoteSequenceCollection.Note note) {
			//			Debug.Log("tentative long");
			int lane = note.note % laneCount;
			if (tentativeBlockDict.TryGetValue(lane, out Block existingBlock)) {
				// if has exising block
				if (existingBlock.type != BlockType.LONG) {
					// if has existing instant or short block, override the instant block
					OverrideTentativeBlock(existingBlock, BlockType.LONG, note);
				} else if (existingBlock.note.end < note.end) {
					// if long and shorter, override
					OverrideTentativeBlock(existingBlock, BlockType.LONG, note);
				} else {
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, BlockType.LONG, note);
			}
		}

		void OverrideTentativeBlock(Block block, BlockType newType, NoteSequenceCollection.Note newNote) {
			block.type = newType;
			block.backgroundNotes.Add(block.note);
			block.note = newNote;
			block.end = newNote.end;
		}

		void AddTentativeBlock(int lane, BlockType type, NoteSequenceCollection.Note note) {
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

		sealed class NoteComparer : IComparer<NoteSequenceCollection.Note> {
			public int Compare(NoteSequenceCollection.Note a, NoteSequenceCollection.Note b) {
				if (a.note < b.note) {
					return 1;
				}
				return -1;
			}
		}
		readonly NoteComparer noteComparer = new NoteComparer();

		public virtual void GenerateBlocks() {
			tentativeNotes.Sort(noteComparer);
			for (int i = 0; i < tentativeNotes.Count; i++) {
				//Debug.Log(i + " " + tentativeNotes[i].note + " " + tentativeNotes[i].velocity);
				if (tentativeBlockDict.Count >= maxSimultaneousBlocks) {
					host.AddBackgroundNote(tentativeNotes[i]);
					continue;
				}

				var note = tentativeNotes[i];
				Block overlappingLongBlock = null;
				for (int j = 0; j < blockContainer.firstFreeItemIndex; j++) {
					var block = blockContainer.itemList[j];
					if (!block.isTentative && block.type == BlockType.LONG && note.start <= block.end) {
						//					Debug.Log("Overlap!");
						overlappingLongBlock = block;
						break;
					}
				}

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
			tentativeNotes.Clear();

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

				if (block.type == BlockType.INSTANT) {
					// Need to generate InstantConnect
					bool isInner = false;
					if (generateInstantConnect) {
						for (int i = blockContainer.firstFreeItemIndex - 1; i >= 0; i--) {
							var activeBlock = blockContainer.itemList[i];
						//for (var activeBlock in instantBlockQueue.itemQueue.Reverse()) {
							if (!activeBlock.isTentative) {
								if (midiSequencer.ToSeconds(block.note.start - activeBlock.note.start) < maxInstantConnectSeconds && Mathf.Abs(activeBlock.x - block.x) < maxInstantConnectX) {
									isInner = true;
									GenerateConnect(BLOCK_INSTANT_CONNECT, activeBlock, block, false);
									if (!generateInstantConnectMesh) {
										break;
									}
								}
							}
						}
						instantBlockQueue.Push(block);
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

			if (generateShortConnect && tentativeBlockDict.Count > 1) {
				// Need to generate ShortConnect
				GenerateConnect(BLOCK_SHORT_CONNECT, minXBlock, maxXBlock, true);
			}

			tentativeBlockDict.Clear();
		}

		protected virtual void GenerateConnect(string skinName, Block from, Block to, bool isFixed) {
			var connect = connectContainer.CreateOrReuseItem();
			connect.isTentative = true;
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

		#region Block Update

		public virtual void UpdateBlocks() {
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < blockContainer.firstFreeItemIndex; i++) {
				var block = blockContainer.itemList[i];
				block.isTentative = false;

				int start = block.note.start;
				if (block.end < ticks - graceTicks) {
					// miss
					scoringManager.CountMiss(block);
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
				case BlockType.INSTANT: TouchInstantBlock(bestBlock, bestBlockIndex); break;
				case BlockType.SHORT: TouchShortBlock(bestBlock, bestBlockIndex); break;
				case BlockType.LONG: TouchLongBlock(bestBlock, bestBlockIndex, id, x); break;
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
			//Debug.Log("Up " + holdingBlock.end + " " + GetOffsetInSeconds(holdingBlock.end));
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
				if (block.type != BlockType.INSTANT) {
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
			host.PlayBackgroundNotes(block);
		}

		void TouchShortBlock(Block block, int index) {
			//block.rect.gameObject.SetActive(false);
			HideAndFreeTouchedBlock(block);
			scoringManager.CountScoreForBlock(GetOffsetInSeconds(block.note.start), block);
			host.PlayBackgroundNotes(block);
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
			host.PlayBackgroundNotes(block);
			host.StartNote(block.note);
		}

		protected void HideAndFreeTouchedBlock(Block block) {
			FreeSkin(block.skinName, block.skin);
			if (block.type == BlockType.LONG) {
				FreeSkin(BLOCK_LONG_FILL, block.longFillSkin);
				FreeSkin(BLOCK_LONG_END, block.longEndSkin);
			}
			if (block.type == BlockType.INSTANT) {
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
