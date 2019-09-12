using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Tasks;

namespace TouhouMix.Levels.Gameplay {
	public sealed class GameplayLevelScheduler : MonoBehaviour {
		public CanvasGroup readyPageGroup;
		public Text readyPageText;

		GameScheduler game_;
		AnimationManager anim_;

		void Start() {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;
		}
	}
}
