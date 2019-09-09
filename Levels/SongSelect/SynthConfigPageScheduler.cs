using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midif.V3;
using UnityEngine.UI;
using TouhouMix.Levels.SongSelect.SynthConfigPage;

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
			public RectTransform contentRect;
			public int seqNoteIndex;
			public int freeStartIndex;
			public int noteOnCount;
			public Color startColor;
			public Color endColor;
			public bool isMuted;
			public bool isSoloing;
			public readonly List<PreviewNote> notes = new List<PreviewNote>();
		}

		const string FA_MUSIC = "\uf001";
		const string FA_DRUM = "\uf569";
		const string FA_VOLUME_MUTE = "\uf6a9";
		const string FA_VOLUME_UP = "\uf028";
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
		public PreviewTrack[] previewTracks;

		Sf2File sf2File;
		MidiFile midiFile;
		NoteSequenceCollection sequenceCollection;

		Sf2Synth sf2Synth;
		MidiSequencer midiSequencer;

		void Start() {
			sf2File = new Sf2File(Resources.Load<TextAsset>("sf2/GeneralUser GS v1.471").bytes);
			midiFile = new MidiFile(Resources.Load<TextAsset>("test").bytes);
//			midiFile = new MidiFile(Resources.Load<TextAsset>("dmbn_old/100fes_easy").bytes);
			previewTicks = previewBeats * midiFile.ticksPerBeat;
			sequenceCollection = new NoteSequenceCollection(midiFile);

			var audioConfig = AudioSettings.GetConfiguration();
			var sampleRate = audioConfig.sampleRate;
			audioConfigText.text = string.Format("Sample Rate: {0:N0} Hz\n" +
				"DSP Buffer Size: {1:N0}\n" +
				"DSP Theoratical Delay: {2:N1} ms\n" +
				"{3:N0} Channels", sampleRate, audioConfig.dspBufferSize, (float)audioConfig.dspBufferSize / sampleRate * 1000, (int)audioConfig.speakerMode);

			sf2Synth = new Sf2Synth(sf2File, new Sf2Synth.Table(sampleRate), 64);
			sf2Synth.SetVolume(-10);

			midiSequencer = new MidiSequencer(midiFile, sf2Synth);
			midiSequencer.isMuted = true;

			var trackColorDict = new Dictionary<int, Color>();
			var channelColorDict = new Dictionary<int, Color>();
			for (int i = 0; i < sequenceCollection.tracks.Length; i++) {
				float t = (float)i / sequenceCollection.tracks.Length;
				trackColorDict.Add(sequenceCollection.tracks[i], Color.HSVToRGB(t, 1, 1));
			}
			for (int i = 0; i < sequenceCollection.channels.Length; i++) {
				float t = (float)i / sequenceCollection.channels.Length;
				channelColorDict.Add(sequenceCollection.channels[i], Color.HSVToRGB(t, 1, 1));
			}

			sequenceConfigItems = new SequenceConfigItemController[sequenceCollection.sequences.Count];
			previewTracks = new PreviewTrack[sequenceConfigItems.Length];
			for (int i = 0; i < sequenceCollection.sequences.Count; i++) {
				var seq = sequenceCollection.sequences[i];
				var instance = Instantiate(sequenceConfigItemPrefab, scrollViewContentRect);
				var item = instance.GetComponent<SequenceConfigItemController>();
				sequenceConfigItems[i] = item;

				previewTracks[i] = new PreviewTrack();
				var track = previewTracks[i];
				track.contentRect = item.previewRect;
				track.startColor = channelColorDict[seq.channel];
				track.startColor.a = 0;
				track.endColor = trackColorDict[seq.track];

				item.leftText.text = string.Format("Track {0} (Tk{0})\n" +
					"Channel {1} (Ch{1})\n" +
					"{3} (Prog{2})", seq.track, seq.channel, seq.program, seq.channel == MIDI_PERCUSSION_CHANNEL ? "Percussion Set " + seq.program : names[seq.program]);
				item.iconText.text = seq.channel == MIDI_PERCUSSION_CHANNEL ? FA_DRUM : FA_MUSIC;
				item.iconFrameImage.color = channelColorDict[seq.channel] * new Color(1, 1, 1, item.iconFrameImage.color.a);
				item.itemFrameImage.color = trackColorDict[seq.track] * new Color(1, 1, 1, item.itemFrameImage.color.a);

				item.button1Action = () => {
					item.isUsingInGame = !item.isUsingInGame;
					item.button1Text.text = item.isUsingInGame ? "using in game" : "use in game";
				};
				item.muteButtonAction = () => {
					for (int j = 0; j < sequenceConfigItems.Length; j++) {
						previewTracks[j].isSoloing = false;
					}
					track.isMuted = !track.isMuted;
					item.muteButtonText.text = track.isMuted ? FA_VOLUME_MUTE : FA_VOLUME_UP;
				};
				item.soloButtonAction = () => {
					if (!track.isSoloing) {  // solo
						for (int j = 0; j < sequenceConfigItems.Length; j++) {
							previewTracks[j].isSoloing = false;
							previewTracks[j].isMuted = true;
							sequenceConfigItems[j].muteButtonText.text = FA_VOLUME_MUTE;
						}
						track.isSoloing = true;
						track.isMuted = false;
						item.muteButtonText.text = FA_VOLUME_UP;
					} else {  // inverse solo
						for (int j = 0; j < sequenceConfigItems.Length; j++) {
							previewTracks[j].isSoloing = false;
							previewTracks[j].isMuted = false;
							sequenceConfigItems[j].muteButtonText.text = FA_VOLUME_UP;
						}
						track.isSoloing = false;
						track.isMuted = true;
						item.muteButtonText.text = FA_VOLUME_MUTE;
					}
				};
			}
		}

		void Update() {
			midiSequencer.AdvanceTime(Time.deltaTime);
			float ticks = midiSequencer.ticks;

			for (int i = 0; i < previewTracks.Length; i++) {
				var track = previewTracks[i];
				var seq = sequenceCollection.sequences[i];
				var item = sequenceConfigItems[i];
				for (; track.seqNoteIndex < seq.notes.Count && seq.notes[track.seqNoteIndex].start <= ticks + previewTicks; track.seqNoteIndex++) {
					var seqNote = seq.notes[track.seqNoteIndex];
					// get or create free preview note
					PreviewNote note;
					if (track.freeStartIndex < track.notes.Count) {
						note = track.notes[track.freeStartIndex];
						note.rect.gameObject.SetActive(true);
					} else {
						var instance = Instantiate(previewNotePrefab, track.contentRect);
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
						sf2Synth.NoteOff(seq.channel, note.note, 0);
						note.isFree = true;
						note.rect.gameObject.SetActive(false);
						track.freeStartIndex -= 1;
						track.notes[j] = track.notes[track.freeStartIndex];
						track.notes[track.freeStartIndex] = note;
						j -= 1;
					} else {  // update preview note
						if (!note.isOn && note.start <= ticks) {  // should be on
							note.isOn = true;
							track.noteOnCount += 1;
							if (!track.isMuted) sf2Synth.NoteOn(seq.channel, note.note, note.velocity);
						}
						float scaledStart = (note.start - ticks) / previewTicks;
						note.rect.anchoredPosition = new Vector2(-scaledStart * previewTrackWidth, note.y);
						note.image.color = Color.Lerp(track.startColor, track.endColor, 1 - scaledStart);
					}
				}

				item.previewText.text = string.Format("{0:N0}/{1:N0}", track.noteOnCount, seq.notes.Count);
			}
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