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
	public sealed class GameplayLevelScheduler : MonoBehaviour {
		const int MOUSE_TOUCH_ID = -100;

		public TouhouMix.Prefabs.CanvasSizeWatcher sizeWatcher;

		[Space]
		public CanvasGroup readyPageGroup;
		public Text readyPageText;

		[Space]
		public RectTransform judgeRect;
		public float judgeHeight = 80;
		float cacheHeight;

		[Space]
		public int cacheEsType;
		public float cacheBeats = 2;
		public int graceEsType;
		public float graceBeats = 1;
		float cacheTicks;
		float graceTicks;
		public float perfectTiming = .05f;
		public float greatTiming = .15f;
		public float goodTiming = .2f;
		public float badTiming = .5f;
		// easy 1.6, normal 1.3, hard 1, expert .8
		public float timingMultiplier = 1;

		[Space]
		public GameObject instantBlockPrefab;
		public GameObject shortBlockPrefab;
		public GameObject longBlockPrefab;
		public RectTransform instantBlockPageRect;
		public RectTransform shortBlockPageRect;
		public RectTransform longBlockPageRect;
		public float maxInstantBlockBeats = 1f / 16;
		public float maxShortBlockBeats = 1f / 4;
		float maxInstantBlockTicks;
		float maxShortBlockTicks;

		[Space]
		public int laneCount;
		float[] laneXDict;
		public float blockWidth = 100;
		public float blockJudgingWidth = 120;
		float blockJudgingHalfWidth;

		GameScheduler game_;
		AnimationManager anim_;

		bool hasStarted;

		int sampleRate;

		string midiFileSha256Hash;
		Sf2File sf2File;
		MidiFile midiFile;
		NoteSequenceCollection sequenceCollection;

		Sf2Synth sf2Synth;
		MidiSequencer midiSequencer;

		MidiSynthConfigProto synthConfig;

		readonly List<NoteSequenceCollection.Sequence> gameSequences = new List<NoteSequenceCollection.Sequence>();
		readonly List<NoteSequenceCollection.Sequence> backgroundSequences = new List<NoteSequenceCollection.Sequence>();

		sealed class Block {
			public enum BlockType {
				Instant,
				Short,
				Long,
			}

			public BlockType type;
			public NoteSequenceCollection.Note note;
			public List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();

			public float end;
			public RectTransform rect;
			public ISettable<Color> color;

			public int lane;
			public float x;
			public int index;

			public int holdingFingerId;
			public float holdingOffset;
			public float holdingX;

			public void Reset() {
				rect.gameObject.SetActive(true);
				holdingFingerId = -1;
			}
		}

		sealed class BackgroundTrack {
			public int seqNoteIndex;
		}
		int backgroundNoteFreeStartIndex;
		readonly List<NoteSequenceCollection.Note> backgroundNotes = new List<NoteSequenceCollection.Note>();
		BackgroundTrack[] backgroundTracks;
		BackgroundTrack[] gameTracks;

		readonly List<Block> instantBlocks = new List<Block>();
		readonly List<Block> shortBlocks = new List<Block>();
		readonly List<Block> longBlocks = new List<Block>();
		int instantBlocksFreeStartIndex;
		int shortBlocksFreeStartIndex;
		int longBlocksFreeStartIndex;

		readonly Dictionary<int, Block> holdingBlockDict = new Dictionary<int, Block>();

		readonly Dictionary<int, Block> tentativeBlockDict = new Dictionary<int, Block>();
		readonly HashSet<int> touchedLaneSet = new HashSet<int>();

		void Start() {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;

			hasStarted = false;

			judgeRect.anchoredPosition = new Vector2(0, judgeHeight);
			cacheHeight = sizeWatcher.canvasSize.y - judgeHeight;

			sf2File = new Sf2File(Resources.Load<TextAsset>("sf2/GeneralUser GS v1.471").bytes);

			var audioConfig = AudioSettings.GetConfiguration();
			sampleRate = audioConfig.sampleRate;
			sf2Synth = new Sf2Synth(sf2File, new Sf2Synth.Table(sampleRate), 64);
			sf2Synth.SetVolume(-10);

			midiFile = new MidiFile(Resources.Load<TextAsset>("test").bytes);
			midiFileSha256Hash = MiscHelper.GetBase64EncodedSha256Hash(midiFile.bytes);

			sequenceCollection = new NoteSequenceCollection(midiFile);
			cacheTicks = cacheBeats * midiFile.ticksPerBeat;
			graceTicks = graceBeats * midiFile.ticksPerBeat;
			maxInstantBlockTicks = maxInstantBlockBeats * midiFile.ticksPerBeat;
			maxShortBlockTicks = maxShortBlockBeats * midiFile.ticksPerBeat;

			blockJudgingHalfWidth = blockJudgingWidth * .5f;

			float canvasWidth = sizeWatcher.canvasSize.x;
			laneXDict = new float[laneCount];
			float laneStart = blockWidth * .5f;
			float laneSpacing = (canvasWidth - blockWidth) / (laneCount - 1);
			for (int i = 0; i < laneXDict.Length; i++) {
				laneXDict[i] = laneStart + i * laneSpacing;
			}

			midiSequencer = new MidiSequencer(midiFile, sf2Synth);
			midiSequencer.isMuted = true;

			synthConfig = MidiSynthConfigProto.LoadOrCreateDefault(game_, sequenceCollection, midiFileSha256Hash);
			foreach (var seqConfig in synthConfig.sequenceStateList) {
				if (seqConfig.shouldUseInGame) {
					gameSequences.Add(sequenceCollection.sequences[seqConfig.sequenceIndex]);
				} else if (!seqConfig.isMuted) {
					backgroundSequences.Add(sequenceCollection.sequences[seqConfig.sequenceIndex]);
				}
			}

			perfectTiming *= timingMultiplier;
			greatTiming *= timingMultiplier;
			goodTiming *= timingMultiplier;
			badTiming *= timingMultiplier;

			backgroundTracks = new BackgroundTrack[backgroundSequences.Count];
			for (int i = 0; i < backgroundTracks.Length; i++) backgroundTracks[i] = new BackgroundTrack();
			gameTracks = new BackgroundTrack[gameSequences.Count];
			for (int i = 0; i < gameTracks.Length; i++) gameTracks[i] = new BackgroundTrack();

//			ShowReadyAnimation();
			StartGame();
		}

		void ShowReadyAnimation() {
			readyPageGroup.gameObject.SetActive(true);
			readyPageGroup.alpha = 0;
			anim_.New().FadeIn(readyPageGroup, .5f, 0).Then()
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "3")
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "2")
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "1")
				.FadeOutFromOne(readyPageText, 1, EsType.QuadIn).Then()
				.Set(readyPageText.GetStringSettable(), "Go")
				.Set(readyPageText.GetAlphaFloatSettable(), 1)
				.FadeOutFromOne(readyPageGroup, 1, EsType.QuadIn).Then()
				.Call(StartGame);
		}

		void StartGame() {
			readyPageGroup.gameObject.SetActive(false);
			hasStarted = true;
		}

		void Update() {
			if (!hasStarted) return;

			midiSequencer.AdvanceTime(Time.deltaTime);
			float ticks = midiSequencer.ticks;

			UpdateBackgroundNotes(ticks);

			GenerateGameNotes(ticks);

			touchedLaneSet.Clear();
			#if UNITY_EDITOR
			ProcessMouse(ticks);
			#else
			ProcessTouches(ticks);
			#endif

			UpdateBlocks(ticks);

			sf2Synth.Panic();
		}

		void UpdateBackgroundNotes(float ticks) {
			// end note before starting new note
			for (int i = 0; i < backgroundNoteFreeStartIndex; i++) {
				var note = backgroundNotes[i];
				if (note.end <= ticks) {
					// end background note
					sf2Synth.NoteOff(note.channel, note.note, 0);
					backgroundNoteFreeStartIndex -= 1;
					backgroundNotes[i] = backgroundNotes[backgroundNoteFreeStartIndex];
					backgroundNotes[backgroundNoteFreeStartIndex] = note;
					i -= 1;
				}
			}

			for (int i = 0; i < backgroundSequences.Count; i++) {
				var seq = backgroundSequences[i];
				var track = backgroundTracks[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];
					// start background note
					sf2Synth.NoteOn(seq.channel, seqNote.note, seqNote.velocity);
					if (seqNote.end <= ticks) {
						// already overdue
						sf2Synth.NoteOn(seq.channel, seqNote.note, seqNote.velocity);
					} else {
						if (backgroundNoteFreeStartIndex < backgroundNotes.Count) {
							backgroundNotes[backgroundNoteFreeStartIndex] = seqNote;
						} else {
							backgroundNotes.Add(seqNote);
						}
						backgroundNoteFreeStartIndex += 1;
					}
				}
			}
		}

		void GenerateGameNotes(float ticks) {
			tentativeBlockDict.Clear();

			for (int i = 0; i < gameSequences.Count; i++) {
				var seq = gameSequences[i];
				var track = gameTracks[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks + cacheTicks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];
					// start game block
					AddTentativeGameBlock(seqNote);
				}
			}

			foreach (var pair in tentativeBlockDict) {
				var lane = pair.Key;
				var tentativeBlock = pair.Value;

				Block block;
				if (tentativeBlock.type == Block.BlockType.Instant) {
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, instantBlocks, ref instantBlocksFreeStartIndex,
						instantBlockPrefab, instantBlockPageRect);
				} else if (tentativeBlock.type == Block.BlockType.Short) {
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, shortBlocks, ref shortBlocksFreeStartIndex,
						shortBlockPrefab, shortBlockPageRect);
				} else {  // tentativeBlock.type == Block.BlockType.Long
					block = GetOrCreateBlockFromTentativeBlock(tentativeBlock, longBlocks, ref longBlocksFreeStartIndex,
						longBlockPrefab, longBlockPageRect);
				}
				block.rect.SetAsLastSibling();
				block.lane = lane;
				block.x = laneXDict[lane];
			}
		}

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
//				var touch = Input.GetTouch(i);
//				var position = touch.position.Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);
//				if (position.y > 0.5f * sizeWatcher.canvasSize.y) continue;
//
//				float x = position.x;
//				FillTouchedLaneSet(x);
//
//				if (touch.phase == TouchPhase.Began) {
//					// find the nearest note to press
//					TouchDown(ticks, position, touch.fingerId);
//				} else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
//					// clean up hold and find the nearest perfect instant key to press
//				} else {
//					// update hold and find the perfect instant key to press
//				}
			}
		}

		void UpdateBlocks(float ticks) {
			UpdateBlocks(ticks, instantBlocks, ref instantBlocksFreeStartIndex);
			UpdateBlocks(ticks, shortBlocks, ref shortBlocksFreeStartIndex);
			UpdateLongBlocks(ticks, longBlocks, ref longBlocksFreeStartIndex);
		}

		void UpdateBlocks(float ticks, List<Block> blocks, ref int freeStartIndex) {
			for (int i = 0; i < freeStartIndex; i++) {
				var block = blocks[i];
				float start = block.note.start;
				if (start <= ticks - graceTicks) {
					// miss
					HideAndFreeTouchedBlock(block, i, blocks, ref freeStartIndex);
					i -= 1;
				} else {
					float y = GetY(ticks, start);
					block.rect.anchoredPosition = new Vector2(block.x, y);
				}
			}
		}

		void UpdateLongBlocks(float ticks, List<Block> blocks, ref int freeStartIndex) {
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
					float startY = block.holdingFingerId != -1 ? judgeHeight : GetY(ticks, start);
					float endY = GetY(ticks, end);
					block.rect.anchoredPosition = new Vector2(block.holdingFingerId != -1 ? block.holdingX : block.x, startY);
					block.rect.sizeDelta = new Vector2(blockWidth, endY - startY);
				}
			}
		}

		float GetY(float ticks, float start) {
			if (ticks < start) {
				// in cache period
				return Es.Calc(cacheEsType, (start - ticks) / cacheTicks) * cacheHeight + judgeHeight;
			} else { // start <= ticks
				// in grace period
				return judgeHeight - judgeHeight * Es.Calc(graceEsType, (ticks - start) / graceTicks);
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

		void AddTentativeGameBlock(NoteSequenceCollection.Note note) {
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

			if (note.duration <= maxInstantBlockTicks) {
				// tentative instant block
				AddTentativeInstantBlock(note);
			} else if (note.duration <= maxShortBlockTicks) {
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
			Block existingBlock;
			if (tentativeBlockDict.TryGetValue(lane, out existingBlock)) {
				// if has exising block, append as background note
				existingBlock.backgroundNotes.Add(note);
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, Block.BlockType.Instant, note);
			}
		}

		void AddTentativeShortBlock(NoteSequenceCollection.Note note) {
//			Debug.Log("tentative short");
			int lane = note.note % laneCount;
			Block existingBlock;
			if (tentativeBlockDict.TryGetValue(lane, out existingBlock)) {
				// if has exising block
				if (existingBlock.type == Block.BlockType.Instant) {
					// if has existing instant block, override the instant block
					OverrideTentativeBlock(existingBlock, Block.BlockType.Short, note);
				} else {
					// if short or long, append as background note
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, Block.BlockType.Short, note);
			}
		}

		void AddTentativeLongBlock(NoteSequenceCollection.Note note) {
//			Debug.Log("tentative long");
			int lane = note.note % laneCount;
			Block existingBlock;
			if (tentativeBlockDict.TryGetValue(lane, out existingBlock)) {
				// if has exising block
				if (existingBlock.type != Block.BlockType.Long) {
					// if has existing instant or short block, override the instant block
					OverrideTentativeBlock(existingBlock, Block.BlockType.Long, note);
				} else if (existingBlock.note.end < note.end) {
					// if long and shorter, override
					OverrideTentativeBlock(existingBlock, Block.BlockType.Long, note);
				} else {
					existingBlock.backgroundNotes.Add(note);
				}
			} else {
				// if no existing block, normal generation
				AddTentativeBlock(lane, Block.BlockType.Long, note);
			}
		}

		void AddTentativeBlock(int lane, Block.BlockType type, NoteSequenceCollection.Note note) {
			tentativeBlockDict.Add(lane, new Block{type = type, note = note});
		}

		static void OverrideTentativeBlock(Block block, Block.BlockType newType, NoteSequenceCollection.Note newNote) {
			block.type = newType;
			block.backgroundNotes.Add(block.note);
			block.note = newNote;
		}

		static Block GetOrCreateBlockFromTentativeBlock(Block tentativeBlock, List<Block> blocks, ref int freeStartIndex, 
			GameObject blockPrefab, RectTransform blockPageRect
		) {
			Block block;
			if (freeStartIndex < blocks.Count) {
				block = blocks[freeStartIndex];
				block.note = tentativeBlock.note;
				block.backgroundNotes = tentativeBlock.backgroundNotes;
				block.end = tentativeBlock.note.end;
			} else {
				var instant = Instantiate(blockPrefab, blockPageRect);
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

		void OnAudioFilterRead (float[] buffer, int channel) {
			if (sf2Synth != null) sf2Synth.Process(buffer);
		}
	}
}
