using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TouhouMix.Levels.SongSelect {
	public sealed class AppConfigPageScheduler : PageScheduler<SongSelectLevelScheduler> { 
		public int displayLanguage {
			get { return GameScheduler.instance.GetDisplayLanguageIndex(); }
			set { GameScheduler.instance.SetDisplayLanguageByIndex(value); }
		}

		public override void Back() {
			level_.Pop();
		}
	}
}
