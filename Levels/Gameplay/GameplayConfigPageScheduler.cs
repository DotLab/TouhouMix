﻿using Uif;
using Uif.Binding;

namespace TouhouMix.Levels.Gameplay {
	public sealed class GameplayConfigPageScheduler : TwoWayBindable {
		public string instantBlockPreset { 
			get { return GameScheduler.instance.gameplayConfig.instantBlockPreset; }
			set { GameScheduler.instance.gameplayConfig.instantBlockPreset = value; }
		}
		public string shortBlockPreset { 
			get { return GameScheduler.instance.gameplayConfig.shortBlockPreset; }
			set { GameScheduler.instance.gameplayConfig.shortBlockPreset = value; }
		}
		public string longBlockPreset { 
			get { return GameScheduler.instance.gameplayConfig.longBlockPreset; }
			set { GameScheduler.instance.gameplayConfig.longBlockPreset = value; }
		}

		public int laneCount { 
			get { return GameScheduler.instance.gameplayConfig.laneCount; }
			set { GameScheduler.instance.gameplayConfig.laneCount = value; }
		}
		public float blockSize { 
			get { return GameScheduler.instance.gameplayConfig.blockSize; }
			set { GameScheduler.instance.gameplayConfig.blockSize = value; }
		}
		public float blockJudgingWidth { 
			get { return GameScheduler.instance.gameplayConfig.blockJudgingWidth; }
			set { GameScheduler.instance.gameplayConfig.blockJudgingWidth = value; }
		}

		public float judgeLinePosition { 
			get { return GameScheduler.instance.gameplayConfig.judgeLinePosition; }
			set { GameScheduler.instance.gameplayConfig.judgeLinePosition = value; }
		}
		public float judgeLineThickness { 
			get { return GameScheduler.instance.gameplayConfig.judgeLineThickness; }
			set { GameScheduler.instance.gameplayConfig.judgeLineThickness = value; }
		}

		public float cacheTime { 
			get { return GameScheduler.instance.gameplayConfig.cacheTime; }
			set { GameScheduler.instance.gameplayConfig.cacheTime = value; }
		}
		public int cacheEasingType { 
			get { return GameScheduler.instance.gameplayConfig.cacheEasingType; }
			set { GameScheduler.instance.gameplayConfig.cacheEasingType = value; }
		}
		public int cacheEasingTypeFunc {
			get { UnityEngine.Debug.Log("read " + cacheEasingType);  return EsType.GetFunc(cacheEasingType) >> 2; }
			set { UnityEngine.Debug.Log("write" + value); cacheEasingType = EsType.SetFunc(cacheEasingType, value << 2); }
		}
		public int cacheEasingTypePhase {
			get { return EsType.GetPhase(cacheEasingType); }
			set { cacheEasingType = EsType.SetPhase(cacheEasingType, value); }
		}
		public float graceTime { 
			get { return GameScheduler.instance.gameplayConfig.graceTime; }
			set { GameScheduler.instance.gameplayConfig.graceTime = value; }
		}
		public int graceEasingType { 
			get { return GameScheduler.instance.gameplayConfig.graceEasingType; }
			set { GameScheduler.instance.gameplayConfig.graceEasingType = value; }
		}
		public int graceEasingTypeFunc {
			get { return EsType.GetFunc(graceEasingType) >> 2; }
			set { graceEasingType = EsType.SetFunc(graceEasingType, value << 2); }
		}
		public int graceEasingTypePhase {
			get { return EsType.GetPhase(graceEasingType); }
			set { graceEasingType = EsType.SetPhase(graceEasingType, value); }
		}

		public float instantBlockMaxTime { 
			get { return GameScheduler.instance.gameplayConfig.instantBlockMaxTime; }
			set { GameScheduler.instance.gameplayConfig.instantBlockMaxTime = value; }
		}
		public float shortBlockMaxTime { 
			get { return GameScheduler.instance.gameplayConfig.shortBlockMaxTime; }
			set { GameScheduler.instance.gameplayConfig.shortBlockMaxTime = value; }
		}

		public float judgeTimeOffset { 
			get { return GameScheduler.instance.gameplayConfig.judgeTimeOffset; }
			set { GameScheduler.instance.gameplayConfig.judgeTimeOffset = value; }
		}
		public float perfectTime { 
			get { return GameScheduler.instance.gameplayConfig.perfectTime; }
			set { GameScheduler.instance.gameplayConfig.perfectTime = value; }
		}
		public float greatTime { 
			get { return GameScheduler.instance.gameplayConfig.greatTime; }
			set { GameScheduler.instance.gameplayConfig.greatTime = value; }
		}
		public float goodTime { 
			get { return GameScheduler.instance.gameplayConfig.goodTime; }
			set { GameScheduler.instance.gameplayConfig.goodTime = value; }
		}
		public float badTime { 
			get { return GameScheduler.instance.gameplayConfig.badTime; }
			set { GameScheduler.instance.gameplayConfig.badTime = value; }
		}

		public string perfectSparkPreset { 
			get { return GameScheduler.instance.gameplayConfig.perfectSparkPreset; }
			set { GameScheduler.instance.gameplayConfig.perfectSparkPreset = value; }
		}
		public float perfectSparkScaling { 
			get { return GameScheduler.instance.gameplayConfig.perfectSparkScaling; }
			set { GameScheduler.instance.gameplayConfig.perfectSparkScaling = value; }
		}
		public string greatSparkPreset { 
			get { return GameScheduler.instance.gameplayConfig.greatSparkPreset; }
			set { GameScheduler.instance.gameplayConfig.greatSparkPreset = value; }
		}
		public float greatSparkScaling { 
			get { return GameScheduler.instance.gameplayConfig.greatSparkScaling; }
			set { GameScheduler.instance.gameplayConfig.greatSparkScaling = value; }
		}
		public string goodSparkPreset { 
			get { return GameScheduler.instance.gameplayConfig.goodSparkPreset; }
			set { GameScheduler.instance.gameplayConfig.goodSparkPreset = value; }
		}
		public float goodSparkScaling { 
			get { return GameScheduler.instance.gameplayConfig.goodSparkScaling; }
			set { GameScheduler.instance.gameplayConfig.goodSparkScaling = value; }
		}
		public string badSparkPreset { 
			get { return GameScheduler.instance.gameplayConfig.badSparkPreset; }
			set { GameScheduler.instance.gameplayConfig.badSparkPreset = value; }
		}
		public float badSparkScaling { 
			get { return GameScheduler.instance.gameplayConfig.badSparkScaling; }
			set { GameScheduler.instance.gameplayConfig.badSparkScaling = value; }
		}
	}
}