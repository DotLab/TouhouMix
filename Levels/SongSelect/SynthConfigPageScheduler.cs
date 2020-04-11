using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using UnityEngine.UI;
using TouhouMix.Levels.SongSelect.SynthConfigPage;
using Uif.Tasks;
using TouhouMix.Storage.Protos.Json.V1;
using TouhouMix.Storage;

namespace TouhouMix.Levels.SongSelect {
	public class SynthConfigPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public sealed class PreviewNote {
			public RectTransform rect;
			public Image image;
			public float y;
			public float start;
			public float end;
			public byte note;
			public byte velocity;
			public bool isFree;
			public bool isOn;
		}

		public sealed class PreviewTrack {
			public int seqNoteIndex;
			public int noteOnCount;

			public int notesFreeStartIndex;
			public readonly List<PreviewNote> notes = new List<PreviewNote>();

			public void Reset() {
				for (int i = 0; i < notesFreeStartIndex; i++) {
					notes[i].rect.gameObject.SetActive(false);
				}
				seqNoteIndex = 0;
				notesFreeStartIndex = 0;
				noteOnCount = 0;
			}
		}

		const string FA_MUSIC = "\uf001";
		const string FA_DRUM = "\uf569";
		const string FA_VOLUME_MUTE = "\uf6a9";
		const string FA_VOLUME_UP = "\uf028";
		const string FA_MICROPHONE = "\uf130";
		const string FA_MICROPHONE_SLASH = "\uf131";
		const int MIDI_PERCUSSION_CHANNEL = 9;

		public Text audioConfigText;
		public RectTransform scrollViewContentRect;

		[Space]
		public GameObject sequenceConfigItemPrefab;
		public GameObject previewNotePrefab;

		[Space]
		public float previewBeats = 2;
		public float previewTicks;
		public float previewTrackWidth = 600;
		public float previewTrackNoteHeight = 5;

		public SequenceConfigItemController[] sequenceConfigItems;
		public MidiSynthConfigProto state;

		public int sampleRate;

		string midiFileSha256Hash;
		Sf2File sf2File;
		MidiFile midiFile;
		NoteSequenceCollection sequenceCollection;

		Sf2Synth sf2Synth;
		MidiSequencer midiSequencer;

		public override void Back() {
			game.midiSynthConfigs.synthConfigDict[midiFileSha256Hash] = state;
			base.Back();
		}

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			sf2File = new Sf2File(Resources.Load<TextAsset>("sf2/GeneralUser GS v1.471").bytes);
		}

		public override void Enable() {
			base.Enable();
			var audioConfig = AudioSettings.GetConfiguration();
			sampleRate = audioConfig.sampleRate;
			audioConfigText.text = string.Format(
				"Sample Rate: {0:N0} Hz\n" +
				"DSP Buffer Size: {1:N0}\n" +
				"DSP Theoratical Delay: {2:N1} ms ({4:N1} Hz)\n" +
				"{3:N0} Channels\n\n", 
				sampleRate, audioConfig.dspBufferSize, 
				(float)audioConfig.dspBufferSize / sampleRate * 1000, 
				(int)audioConfig.speakerMode, sampleRate / (float)audioConfig.dspBufferSize) +
				string.Format(
					"Model: {0}\n" +
					"Name: {1}\n" +
					"OS: {2}\n" +
					"CPU: {3}\n" +
					"GPU: {4}\n",
					SystemInfo.deviceModel, SystemInfo.deviceName,
					SystemInfo.operatingSystem, SystemInfo.processorType, SystemInfo.graphicsDeviceType);

			sf2Synth = new Sf2Synth(sf2File, new Sf2Synth.Table(sampleRate), 64);
			sf2Synth.SetVolume(-10);

			if (midiFile != null && level.midiDetailPage.midiFile == midiFile) return;
			sf2Synth.Reset();

			midiFile = level.midiDetailPage.midiFile ?? new MidiFile(Resources.Load<TextAsset>("test").bytes);
			sequenceCollection = level.midiDetailPage.sequenceCollection ?? new NoteSequenceCollection(midiFile);
			midiFileSha256Hash = MiscHelper.GetBase64EncodedSha256Hash(midiFile.bytes);

			previewTicks = previewBeats * midiFile.ticksPerBeat;

			midiSequencer = new MidiSequencer(midiFile, sf2Synth);
			midiSequencer.isMuted = true;

			state = MidiSynthConfigProto.LoadOrCreateDefault(game, sequenceCollection, midiFileSha256Hash);

			sequenceConfigItems = new SequenceConfigItemController[sequenceCollection.sequences.Count];
			int childCount = scrollViewContentRect.childCount;
			for (int i = 0; i < sequenceCollection.sequences.Count; i++) {
				var instance = i < childCount ? scrollViewContentRect.GetChild(i).gameObject : Instantiate(sequenceConfigItemPrefab, scrollViewContentRect);
				instance.SetActive(true);
				var item = instance.GetComponent<SequenceConfigItemController>();
				sequenceConfigItems[i] = item;
				item.previewTrack.Reset();

				item.button1Action = () => {
					item.state.shouldUseInGame = !item.state.shouldUseInGame;
					item.ApplyState(item.props, item.state);
				};

				item.muteButtonAction = () => {
					item.state.isMuted = !item.state.isMuted;
					item.ApplyState(item.props, item.state);
				};

				item.soloButtonAction = () => {
					if (state.soloSequenceIndex == item.state.sequenceIndex) {
						state.soloSequenceIndex = -1;
					} else {
						state.soloSequenceIndex = item.state.sequenceIndex;
					}
					ApplyState();
				};

				item.onProgramOverrideSelectChanged = value => {
					sf2Synth.ignoreProgramChange = false;
					sf2Synth.ProgramChange(item.state.channel, (byte)value);
					sf2Synth.ignoreProgramChange = true;
				};
			}

			for (int i = sequenceCollection.sequences.Count; i < childCount; i++) {
				scrollViewContentRect.GetChild(i).gameObject.SetActive(false);
			}

			ApplyState();
		}

		void ApplyState() {
			for (int i = 0; i < sequenceConfigItems.Length; i++) {
				sf2Synth.ignoreProgramChange = false;
				sf2Synth.ProgramChange(state.sequenceStateList[i].channel, (byte)state.sequenceStateList[i].programOverride);
				sf2Synth.ignoreProgramChange = true;
				sequenceConfigItems[i].ApplyState(state, state.sequenceStateList[i]);
			}
		}

		void Update() {
			if (midiSequencer == null) return;

			midiSequencer.AdvanceTime(Time.deltaTime);
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < sequenceConfigItems.Length; i++) {
				var item = sequenceConfigItems[i];
				var track = item.previewTrack;

				var seq = sequenceCollection.sequences[i];

				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks + previewTicks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];

					// get or create free preview note
					PreviewNote note;
					if (track.notesFreeStartIndex < track.notes.Count) {
						note = track.notes[track.notesFreeStartIndex];
						note.rect.gameObject.SetActive(true);
					} else {
						var instance = Instantiate(previewNotePrefab, item.previewRect);
						note = new PreviewNote { rect = instance.GetComponent<RectTransform>(), image = instance.GetComponent<Image>() };
						track.notes.Add(note);
					}
					track.notesFreeStartIndex += 1;

					// start preview note
					note.isFree = false;
					note.isOn = false;
					note.start = seqNote.start;
					note.end = seqNote.end;
					note.note = seqNote.note;
					note.velocity = seqNote.velocity;
					note.rect.anchoredPosition = new Vector2(0, note.y = -(seqNote.note % 12 * previewTrackNoteHeight));
					note.rect.sizeDelta = new Vector2((float)seqNote.duration / previewTicks * previewTrackWidth, previewTrackNoteHeight);
				}

				for (int j = 0; j < track.notesFreeStartIndex; j++) {
					var note = track.notes[j];
					if (note.end <= ticks) {  // free preview note
						if (!note.isOn) {  // the note is overdue, on before off
							track.noteOnCount += 1;
							if (!item.shouldMute) sf2Synth.NoteOn(seq.channel, note.note, note.velocity);
						}
						sf2Synth.NoteOff(seq.channel, note.note, 0);
						note.isFree = true;
						note.rect.gameObject.SetActive(false);
						track.notesFreeStartIndex -= 1;
						track.notes[j] = track.notes[track.notesFreeStartIndex];
						track.notes[track.notesFreeStartIndex] = note;
						j -= 1;
					}
				}

				for (int j = 0; j < track.notesFreeStartIndex; j++) {
					var note = track.notes[j];
					// update preview note
					if (!note.isOn && note.start <= ticks) {  // should be on
						note.isOn = true;
						track.noteOnCount += 1;
						if (!item.shouldMute) sf2Synth.NoteOn(seq.channel, note.note, note.velocity);
					}
					float scaledStart = (note.start - ticks) / previewTicks;
					note.rect.anchoredPosition = new Vector2(-scaledStart * previewTrackWidth, note.y);
					note.image.color = Color.Lerp(item.previewStartColor, item.previewEndColor, 1 - scaledStart);
				}

				item.previewText.text = string.Format("{0:N0}/{1:N0}", track.noteOnCount, seq.notes.Count);
			}

			sf2Synth.Panic();
		}

		void OnAudioFilterRead(float[] buffer, int channel) {
			if (sf2Synth != null) sf2Synth.Process(buffer);
		}
	}
}