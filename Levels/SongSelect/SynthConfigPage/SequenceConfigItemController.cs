using UnityEngine;
using UnityEngine.UI;
using Uif.Extensions;

namespace TouhouMix.Levels.SongSelect.SynthConfigPage {
	public sealed class SequenceConfigItemController : MonoBehaviour {
		const int MIDI_PERCUSSION_CHANNEL = 9;

		const string FA_MUSIC = "\uf001";
		const string FA_DRUM = "\uf569";
		const string FA_VOLUME_MUTE = "\uf6a9";
		const string FA_VOLUME_UP = "\uf028";
		const string FA_MICROPHONE = "\uf130";
		const string FA_MICROPHONE_SLASH = "\uf131";

		public Text leftText;
		public Text iconText;

		public Image iconFrameImage;
		public Image itemFrameImage;

		[Space]
		public Text button1Text;
		public Text button2Text;
		public Text button3Text;

		public Text muteButtonText;
		public Text soloButtonText;

		[Space]
		public RectTransform previewRect;
		public Text previewText;

		public System.Action button1Action;
		public System.Action muteButtonAction;
		public System.Action soloButtonAction;

		public SynthConfigPageScheduler.PreviewTrack previewTrack = new SynthConfigPageScheduler.PreviewTrack();

		public SynthConfigPageScheduler.SynthConfigPageStateProto props;
		public SynthConfigPageScheduler.SynthConfigPageStateProto.SequenceStateProto state;

		public Color previewStartColor;
		public Color previewEndColor;
		public bool shouldMute;

		public void ApplyState(
			SynthConfigPageScheduler.SynthConfigPageStateProto props,
			SynthConfigPageScheduler.SynthConfigPageStateProto.SequenceStateProto state
		) {
			this.props = props;
			this.state = state;

			leftText.text = string.Format(
				"Track {0} (Tk{0})\n" +
				"Channel {1} (Ch{1})\n" +
				"{3} (Prog{2})", state.track, state.channel, state.program, 
				state.channel == MIDI_PERCUSSION_CHANNEL ? "Percussion Set " + state.program : programNames[state.program]);

			iconText.text = state.channel == MIDI_PERCUSSION_CHANNEL ? FA_DRUM : FA_MUSIC;

			var v = state.shouldUseInGame ? 1 : .2f;
			var trackColor = Color.HSVToRGB((float)state.trackGroup / props.trackGroupCount, 1, v);
			var channelColor = Color.HSVToRGB((float)state.channelGroup / props.channelGroupCount, 1, v);
			previewStartColor = trackColor;
			previewStartColor.a = 0;
			previewEndColor = channelColor;
			trackColor.a = iconFrameImage.color.a;
			iconFrameImage.color = trackColor;
			channelColor.a = itemFrameImage.color.a;
			itemFrameImage.color = channelColor;

			if (state.shouldUseInGame) {
				button1Text.text = "using in game";
			} else {
				button1Text.text = "not using in game";
			}

			muteButtonText.text = state.isMuted ? FA_VOLUME_MUTE : FA_VOLUME_UP;
			muteButtonText.SetAlpha(state.isMuted ? .2f : 1f);
			if (props.soloSequenceIndex == -1 || props.soloSequenceIndex == state.sequenceIndex) {
				soloButtonText.text = FA_MICROPHONE;
				soloButtonText.SetAlpha(1);
			} else {
				soloButtonText.text = FA_MICROPHONE_SLASH;
				soloButtonText.SetAlpha(.2f);
			}

			shouldMute = state.isMuted || (props.soloSequenceIndex != -1 && props.soloSequenceIndex != state.sequenceIndex);
		}
			
		public void OnButton1Clicked() {
			button1Action();
		}

		public void OnMuteButtonClicked() {
			muteButtonAction();
		}

		public void OnSoloButtonClicked() {
			soloButtonAction();
		}

		readonly string[] programNames = { 
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