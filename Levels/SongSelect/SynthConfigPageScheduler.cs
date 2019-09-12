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
			public int freeStartIndex;
			public int noteOnCount;

			public readonly List<PreviewNote> notes = new List<PreviewNote>();

			public void Reset() {
				for (int i = 0; i < freeStartIndex; i++) {
					notes[i].rect.gameObject.SetActive(false);
				}
				seqNoteIndex = 0;
				freeStartIndex = 0;
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

		public override Uif.AnimationSequence Show(Uif.AnimationSequence seq) {
			return seq.Call(Init).Append(base.Show);
		}

		public override void Back() {
			game_.midiSynthConfigs.synthConfigDict[midiFileSha256Hash] = state;

			level_.Pop();
		}

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			sf2File = new Sf2File(Resources.Load<TextAsset>("sf2/GeneralUser GS v1.471").bytes);

			var audioConfig = AudioSettings.GetConfiguration();
			sampleRate = audioConfig.sampleRate;
			audioConfigText.text = string.Format(
				"Sample Rate: {0:N0} Hz\n" +
				"DSP Buffer Size: {1:N0}\n" +
				"DSP Theoratical Delay: {2:N1} ms\n" +
				"{3:N0} Channels", sampleRate, audioConfig.dspBufferSize, (float)audioConfig.dspBufferSize / sampleRate * 1000, (int)audioConfig.speakerMode);

			sf2Synth = new Sf2Synth(sf2File, new Sf2Synth.Table(sampleRate), 64);
			sf2Synth.SetVolume(-10);
		}

		public void Init() {
			if (midiFile != null && level_.midiDetailPage.midiFile == midiFile) return; 
			sf2Synth.Reset();

//			midiFile = level_.midiDetailPage.midiFile ?? new MidiFile(Resources.Load<TextAsset>("test").bytes);
//			sequenceCollection = level_.midiDetailPage.sequenceCollection ?? new NoteSequenceCollection(midiFile);
			midiFile = new MidiFile(Resources.Load<TextAsset>("test").bytes);
			midiFileSha256Hash = MiscHelper.GetBase64EncodedSha256Hash(midiFile.bytes);

			sequenceCollection = new NoteSequenceCollection(midiFile);
			previewTicks = previewBeats * midiFile.ticksPerBeat;

			midiSequencer = new MidiSequencer(midiFile, sf2Synth);
			midiSequencer.isMuted = true;

			LoadOrCreateState();

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
			}

			for (int i = sequenceCollection.sequences.Count; i < childCount; i++) {
				scrollViewContentRect.GetChild(i).gameObject.SetActive(false);
			}

			ApplyState();
		}

		void LoadOrCreateState() {
			try {
				state = game_.midiSynthConfigs.synthConfigDict[midiFileSha256Hash];

				Systemf.Assert.Equal(sequenceCollection.sequences.Count, state.sequenceStateList.Count);
			} catch (System.Exception e) {
				Debug.LogError(e);

				state = new MidiSynthConfigProto{
					trackGroupCount = sequenceCollection.trackGroups.Length,
					channelGroupCount = sequenceCollection.channelGroups.Length,
					soloSequenceIndex = -1,
					sequenceStateList = new List<MidiSynthConfigProto.SequenceConfigProto>(),
				};
				
				for (int i = 0; i < sequenceCollection.sequences.Count; i++) {
					var seq = sequenceCollection.sequences[i];
					state.sequenceStateList.Add(new MidiSynthConfigProto.SequenceConfigProto{
						sequenceIndex = i,
						track = seq.track,
						trackGroup = seq.trackGroup,
						channel = seq.channel,
						channelGroup = seq.channelGroup,
						program = seq.program,
						shouldUseInGame = false,
						isMuted = false,
					});
				}
			}
		}

		void ApplyState() {
			for (int i = 0; i < sequenceConfigItems.Length; i++) {
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
					if (track.freeStartIndex < track.notes.Count) {
						note = track.notes[track.freeStartIndex];
						note.rect.gameObject.SetActive(true);
					} else {
						var instance = Instantiate(previewNotePrefab, item.previewRect);
						note = new PreviewNote{rect = instance.GetComponent<RectTransform>(), image = instance.GetComponent<Image>()};
						track.notes.Add(note);
					}

					// start preview note
					track.freeStartIndex += 1;
					note.isFree = false;
					note.isOn = false;
					note.start = seqNote.start;
					note.end = seqNote.end;
					note.note = seqNote.note;
					note.velocity = seqNote.velocity;
					note.rect.anchoredPosition = new Vector2(0, note.y = -(seqNote.note % 12 * previewTrackNoteHeight));
					note.rect.sizeDelta = new Vector2((float)seqNote.duration / previewTicks * previewTrackWidth, previewTrackNoteHeight);
				}

				for (int j = 0; j < track.freeStartIndex; j++) {
					var note = track.notes[j];
					if (note.end <= ticks) {  // free preview note
						if (!note.isOn) {  // the note is overdue, on before off
							track.noteOnCount += 1;
							if (!item.shouldMute) sf2Synth.NoteOn(seq.channel, note.note, note.velocity);
						}
						sf2Synth.NoteOff(seq.channel, note.note, 0);
						note.isFree = true;
						note.rect.gameObject.SetActive(false);
						track.freeStartIndex -= 1;
						track.notes[j] = track.notes[track.freeStartIndex];
						track.notes[track.freeStartIndex] = note;
						j -= 1;
					}
				}

				for (int j = 0; j < track.freeStartIndex; j++) {
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

		void OnAudioFilterRead (float[] buffer, int channel) {
			if (sf2Synth != null) sf2Synth.Process(buffer);
		}

		readonly string[] names = { 
			"Acoustic Grand Piano",
			"Bright Acoustic Piano",
			"Electric Grand Piano",
			"Honky-tonk Piano",
			"Electric Piano 1",
			"Electric Piano 2",
			"Harpsichord",
			"Clavi",
			"Celesta",
			"Glockenspiel",
			"Music Box",
			"Vibraphone",
			"Marimba",
			"Xylophone",
			"Tubular Bells",
			"Dulcimer",
			"Drawbar Organ",
			"Percussive Organ",
			"Rock Organ",
			"Church Organ",
			"Reed Organ",
			"Accordion",
			"Harmonica",
			"Tango Accordion",
			"Acoustic Guitar (nylon)",
			"Acoustic Guitar (steel)",
			"Electric Guitar (jazz)",
			"Electric Guitar (clean)",
			"Electric Guitar (muted)",
			"Overdriven Guitar",
			"Distortion Guitar",
			"Guitar harmonics",
			"Acoustic Bass",
			"Electric Bass (finger)",
			"Electric Bass (pick)",
			"Fretless Bass",
			"Slap Bass 1",
			"Slap Bass 2",
			"Synth Bass 1",
			"Synth Bass 2",
			"Violin",
			"Viola",
			"Cello",
			"Contrabass",
			"Tremolo Strings",
			"Pizzicato Strings",
			"Orchestral Harp",
			"Timpani",
			"String Ensemble 1",
			"String Ensemble 2",
			"SynthStrings 1",
			"SynthStrings 2",
			"Choir Aahs",
			"Voice Oohs",
			"Synth Voice",
			"Orchestra Hit",
			"Trumpet",
			"Trombone",
			"Tuba",
			"Muted Trumpet",
			"French Horn",
			"Brass Section",
			"SynthBrass 1",
			"SynthBrass 2",
			"Soprano Sax",
			"Alto Sax",
			"Tenor Sax",
			"Baritone Sax",
			"Oboe",
			"English Horn",
			"Bassoon",
			"Clarinet",
			"Piccolo",
			"Flute",
			"Recorder",
			"Pan Flute",
			"Blown Bottle",
			"Shakuhachi",
			"Whistle",
			"Ocarina",
			"Lead 1 (square)",
			"Lead 2 (sawtooth)",
			"Lead 3 (calliope)",
			"Lead 4 (chiff)",
			"Lead 5 (charang)",
			"Lead 6 (voice)",
			"Lead 7 (fifths)",
			"Lead 8 (bass + lead)",
			"Pad 1 (new age)",
			"Pad 2 (warm)",
			"Pad 3 (polysynth)",
			"Pad 4 (choir)",
			"Pad 5 (bowed)",
			"Pad 6 (metallic)",
			"Pad 7 (halo)",
			"Pad 8 (sweep)",
			"FX 1 (rain)",
			"FX 2 (soundtrack)",
			"FX 3 (crystal)",
			"FX 4 (atmosphere)",
			"FX 5 (brightness)",
			"FX 6 (goblins)",
			"FX 7 (echoes)",
			"FX 8 (sci-fi)",
			"Sitar",
			"Banjo",
			"Shamisen",
			"Koto",
			"Kalimba",
			"Bag pipe",
			"Fiddle",
			"Shanai",
			"Tinkle Bell",
			"Agogo",
			"Steel Drums",
			"Woodblock",
			"Taiko Drum",
			"Melodic Tom",
			"Synth Drum",
			"Reverse Cymbal",
			"Guitar Fret Noise",
			"Breath Noise",
			"Seashore",
			"Bird Tweet",
			"Telephone Ring",
			"Helicopter",
			"Applause",
			"Gunshot",
		};
	}
}