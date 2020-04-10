using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Uif.Binding;
using UnityEngine.UI;
using Uif.Tasks;

namespace TouhouMix.Levels.SongSelect {
	public sealed class AppConfigPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public Text sampleRateText;
		public Text audioBufferText;
		public Text audioDelayText;

		public int displayLanguage {
			get { return GameScheduler.instance.GetDisplayLanguageIndex(); }
			set { GameScheduler.instance.SetDisplayLanguageByIndex(value); }
		}

		public int translateUserGeneratedContent {
			get { return BindingHelper.BoolToInt(GameScheduler.instance.appConfig.translateUserGeneratedContent); }
			set { GameScheduler.instance.appConfig.translateUserGeneratedContent = BindingHelper.IntToBool(value); }
		}

		public int sampleRateDownscale {
			get { return GameScheduler.instance.appConfig.sampleRateDownscale; }
			set { GameScheduler.instance.appConfig.sampleRateDownscale = value; GameScheduler.instance.ApplyAppAudioConfig(); RefreshAudioInfo();}
		}

		public int audioBufferUpscale {
			get { return GameScheduler.instance.appConfig.audioBufferUpscale; }
			set { GameScheduler.instance.appConfig.audioBufferUpscale = value; GameScheduler.instance.ApplyAppAudioConfig(); RefreshAudioInfo();}
		}

		public int networkEndpoint {
			get { return GameScheduler.instance.appConfig.networkEndpoint; }
			set { GameScheduler.instance.appConfig.networkEndpoint = value; GameScheduler.instance.ApplyAppAudioConfig(); RefreshAudioInfo();}
		}

		public override void Enable() {
			base.Enable();
			RefreshAudioInfo();
		}

		void RefreshAudioInfo() {
			var audioConfig = AudioSettings.GetConfiguration();
			sampleRateText.text = string.Format("{0:N0} Hz", audioConfig.sampleRate);
			audioBufferText.text = string.Format("{0:N0} Samples", audioConfig.dspBufferSize);
			audioDelayText.text = string.Format("{0:N1} ms ({1:N1} Hz)", 
				(float)audioConfig.dspBufferSize / audioConfig.sampleRate * 1000, audioConfig.sampleRate / (float)audioConfig.dspBufferSize);
		}
	}
}
