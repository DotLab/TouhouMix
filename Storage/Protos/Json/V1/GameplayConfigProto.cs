using System.Collections.Generic;

namespace TouhouMix.Storage.Protos.Json.V1 {
	[System.Serializable]
	public sealed class GameplayConfigProto {
		public enum DifficaultyPresetEnum {
			BEGINNER = 0,
			EASY = 1,
			NORMAL = 2,
			HARD = 3,
			LUNATIC = 4,
			CUSTOM = 5,
		}
		public int difficultyPreset;

		public enum LayoutPresetEnum {
			ONE_ONLY = 0,
			SCANNING_LINE = 6,
		}
		public int layoutPreset;

		public bool useRandomColor;
		public bool useOneColor;
		public string instantBlockColor;
		public string shortBlockColor;
		public string longBlockColor;

		public bool useCustomBlockSkin;
		public enum CustomBlockSkinFilterModeEnum {
			NEAREST = 0,
			BILINEAR = 1,
		}
		public int customBlockSkinFilterMode;
		public string blockSkinPreset;

		public bool keyboardMode;
		public string keyboardModeKeys;

		public int laneCount;
		public float blockSize;
		public float blockJudgingWidth;

		public float judgeLinePosition;
		public float judgeLineThickness;

		public float playbackSpeed;
		public float cacheTime;
		public int cacheEasingType;
		public float graceTime;
		public int graceEasingType;

		public float instantBlockMaxTime;
		public float shortBlockMaxTime;

		public int maxSimultaneousBlocks;
		public float minTapInterval;
		public float minCooldownTime;
		public float maxTouchMoveSpeed;
		public float maxBlockCoalesceTime;

		public bool generateShortConnect;
		public bool generateInstantConnect;
		public float instantConnectMaxTime;

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
				difficultyPreset = (int)DifficaultyPresetEnum.BEGINNER,
				layoutPreset = (int)LayoutPresetEnum.ONE_ONLY,

				useRandomColor = true,
				useOneColor = false,
				instantBlockColor = "#ff0000",
				shortBlockColor = "#ff0000",
				longBlockColor = "#ff0000",

				useCustomBlockSkin = false,
				customBlockSkinFilterMode = (int)CustomBlockSkinFilterModeEnum.NEAREST,
				blockSkinPreset = "Squre45",

#if UNITY_STANDALONE
				keyboardMode = true,
#else
				keyboardMode = false,
#endif
				keyboardModeKeys = "S,D,F,J,K,L",

				laneCount = 12,
				blockSize = 120,
				blockJudgingWidth = 200,

				judgeLinePosition = 60,
				judgeLineThickness = 2,

				playbackSpeed = 1,
				cacheTime = 2,
				cacheEasingType = 10,
				graceTime = 1,
				graceEasingType = 9,

				instantBlockMaxTime = .2f,
				shortBlockMaxTime = 1,

				maxSimultaneousBlocks = 2,
				minTapInterval = .2f,
				minCooldownTime = 1.5f,
				maxTouchMoveSpeed = 400,
				// 2 1 .5f .05f
				maxBlockCoalesceTime = 2,

				generateShortConnect = true,
				generateInstantConnect = true,
				instantConnectMaxTime = 1f,

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
