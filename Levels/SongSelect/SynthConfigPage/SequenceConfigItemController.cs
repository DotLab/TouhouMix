using UnityEngine;
using UnityEngine.UI;
using Uif.Extensions;
using TouhouMix.Storage.Protos.Json.V1;

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
		public System.Action<int> onProgramOverrideSelectChanged;

		public SynthConfigPageScheduler.PreviewTrack previewTrack = new SynthConfigPageScheduler.PreviewTrack();

		public MidiSynthConfigProto props;
		public MidiSynthConfigProto.SequenceConfigProto state;

		public Color previewStartColor;
		public Color previewEndColor;
		public bool shouldMute;

		bool enableCallback = false;
		public Dropdown programOverrideDropdown;
		public void OnProgramOverrideDropdownChanged(int value) {
			if (!enableCallback) {
				return;
			}
			state.programOverride = value; 
			onProgramOverrideSelectChanged(value);
		}

		public void ApplyState(
			MidiSynthConfigProto props,
			MidiSynthConfigProto.SequenceConfigProto state
		) {
			this.props = props;
			this.state = state;

			leftText.text = string.Format(
				"Track {0} (Tk{0})\n" +
				"Channel {1} (Ch{1})\n" +
				"{3} (Prog{2})", state.track, state.channel, state.program, 
				state.channel == MIDI_PERCUSSION_CHANNEL ? "Percussion Set " + state.program : MidiProgramOptionFiller.names[state.program]);
			programOverrideDropdown.GetComponent<MidiProgramOptionFiller>().Start();
			programOverrideDropdown.value = state.programOverride;
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
				button1Text.Translate("using in game");
			} else {
				button1Text.Translate("not using in game");
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

			enableCallback = true;
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
	}
}