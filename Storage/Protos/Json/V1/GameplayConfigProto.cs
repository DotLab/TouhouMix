using System.Collections.Generic;

namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class GameplayConfigProto {
		public string instantBlockPreset;
		public string shortBlockPreset;
		public string longBlockPreset;

		public int laneCount;
		public float blockSize;
		public float blockJudgingWidth;

		public float judgeLinePosition;
		public float judgeLineThickness;

		public float cacheTime;
		public int cacheEasingType;
		public float graceTime;
		public int graceEasingType;

		public float instantBlockMaxTime;
		public float shortBlockMaxTime;

		public float judgeTimeOffset;
		public float perfectTime;
		public float greatTime;
		public float goodTime;
		public float badTime;

		public string perfectSparkPreset;
		public float perfectSparkScaling;
		public string greatSparkPreset;
		public float greatSparkScaling;
		public string goodSparkPreset;
		public float goodSparkScaling;
		public string badSparkPreset;
		public float badSparkScaling;
		
		public static GameplayConfigProto CreateDefault() {
			return new GameplayConfigProto {
				instantBlockPreset = "Voez/VoezInstantBlock",
				shortBlockPreset = "Voez/VoezShortBlock",
				longBlockPreset = "Voez/VoezLongBlock",

				laneCount = 12,
				blockSize = 150,
				blockJudgingWidth = 200,

				judgeLinePosition = 60,
				judgeLineThickness = 2,

				cacheTime = 2,
				cacheEasingType = 10,
				graceTime = 1,
				graceEasingType = 9,

				instantBlockMaxTime = .2f,
				shortBlockMaxTime = 1,

				judgeTimeOffset = 0,
				perfectTime = .05f,
				greatTime = .15f,
				goodTime = .2f,
				badTime = .5f,

				perfectSparkPreset = "StylizedExplosionPack1/explosion_stylized_large_darkFire",
				perfectSparkScaling = 50,
				greatSparkPreset = "StylizedExplosionPack1/explosion_stylized_medium_originalFire",
				greatSparkScaling = 50,
				goodSparkPreset = "StylizedExplosionPack1/explosion_stylized_medium_wildFire",
				goodSparkScaling = 50,
				badSparkPreset = "StylizedExplosionPack1/explosion_stylized_small_demonFire",
				badSparkScaling = 50,
			};
		}
	}
}
