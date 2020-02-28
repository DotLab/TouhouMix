using UnityEngine;
using Uif.Binding;

namespace TouhouMix.Levels.SongSelect {
	public sealed class GameConfigPageScheduler : TwoWayBindable {
		float cacheTime_;
		public float cacheTime {
			get {return cacheTime_;}
			set {cacheTime_ = value; BackPropagateValue("cacheTime", value);}
		}

		[ContextMenu("Reset Cache Time")]
		public void dsf() {
			cacheTime = 3;
		}
	}
}