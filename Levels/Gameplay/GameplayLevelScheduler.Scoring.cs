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
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		void CountScoreForBlock(Block block) {

		}

		void CountMiss() {

		}

		float GetTimingScoreMultiplier(float timing) {
			if (timing < perfectTiming) return perfectTimingScoreMultiplier;
			if (timing < greatTiming) return greatTimingScoreMultiplier;
			if (timing < goodTiming) return goodTimingScoreMultiplier;
			if (timing < badTiming) return badTimingScoreMultiplier;
			return missTimingScoreMultiplier;
		}

		float GetComboScoreMultiplier(int count) {
			if (count <= 50) return 1;
			if (count <= 100) return 1.1f;
			if (count <= 200) return 1.15f;
			if (count <= 400) return 1.2f;
			if (count <= 600) return 1.25f;
			if (count <= 800) return 1.3f;
			return 1.35f;
		}
	}
}
