using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TouhouMix.Prefabs;
using Uif;
using Uif.Settables;
using Block = TouhouMix.Levels.Gameplay.GameplayLevelScheduler.Block;
using TouhouMix.Storage.Protos.Json.V1;

namespace TouhouMix.Levels.Gameplay {
	public sealed class ScoringManager : MonoBehaviour {
		public float timingOffset = 0;
		public float perfectTiming = .05f;
		public float greatTiming = .15f;
		public float goodTiming = .2f;
		public float badTiming = .5f;
		// easy 1.6, normal 1.3, hard 1, expert .8

		[Space]
		public ProgressBarPageScheduler progressBar;
		public Text scoreText;
		public Text scoreAdditionText;
		public Text comboText;
		public Text judgmentText;
		public Text accuracyText;
		public float scoreBase = 125;
		public Color perfectColor;
		public Color greatColor;
		public Color goodColor;
		public Color badColor;
		public Color missColor;
		int score;
		int combo;
		float accuracyFactor;
		float perfectAccuracyFactor;
		float accuracy;

		int perfectCount = 0;
		int greatCount = 0;
		int goodCount = 0;
		int badCount = 0;
		int missCount = 0;
		int maxCombo;

		[Space]
		public Transform sparkPage;
		public ParticleBurstEmitter perfectEmitter;
		public ParticleBurstEmitter greatEmitter;
		public ParticleBurstEmitter goodEmitter;
		public ParticleBurstEmitter badEmitter;

		public RectTransform flashPageRect;
		public GameObject flashPrefab;
		FlashController[] flashControllers;

		public RectTransform backgroundRect;

		enum Judgment {
			Perfect,
			Great,
			Good,
			Bad,
			Miss,
		}

		GameScheduler game_;
		AnimationManager anim_;

		public void Init(GameplayLevelScheduler level) {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;

			scoreText.text = "";
			scoreAdditionText.text = "";
			comboText.text = "";
			judgmentText.text = "";
			accuracyText.text = "";

			flashControllers = new FlashController[level.laneCount];
			for (int i = 0; i < level.laneCount; i++) {
				var flashController = Instantiate(flashPrefab, flashPageRect).GetComponent<FlashController>();
				flashController.rect.anchoredPosition = new Vector2(level.laneXDict[i], level.judgeHeight);
				flashController.rect.sizeDelta = new Vector2(level.blockWidth, 0);
				flashControllers[i] = flashController;
			}
		}

		public void LoadGameplayConfig(GameplayConfigProto config) {
			try {
				timingOffset = config.judgeTimeOffset;
				perfectTiming = config.perfectTime;
				greatTiming = config.greatTime;
				goodTiming = config.goodTime;
				badTiming = config.badTime;

				perfectEmitter = LoadSparkPreset(config.perfectSparkPreset, config.perfectSparkScaling);
				greatEmitter = LoadSparkPreset(config.greatSparkPreset, config.greatSparkScaling);
				goodEmitter = LoadSparkPreset(config.goodSparkPreset, config.goodSparkScaling);
				badEmitter = LoadSparkPreset(config.badSparkPreset, config.badSparkScaling);
			} catch(System.Exception e) {
				Debug.Log(e);
			}
		}

		ParticleBurstEmitter LoadSparkPreset(string path, float scaling) {
			var prefab = Resources.Load<GameObject>("Sparks/" + path);
			var instance = Instantiate(prefab, sparkPage, false);
			instance.transform.localScale = new Vector3(scaling, scaling, scaling);
			return instance.GetComponent<ParticleBurstEmitter>();
		}

		public void SetProgress(float progress) {
			progressBar.SetProgress(progress);
		}

		public void ReportScores() {
			game_.perfectCount = perfectCount;
			game_.greatCount = greatCount;
			game_.goodCount = goodCount;
			game_.badCount = badCount;
			game_.missCount = missCount;
			game_.score = score;
			game_.accuracy = accuracy;
			game_.maxComboCount = maxCombo;
		}


		public void CountScoreForBlock(float timing, Block block) {
			var judgment = GetTimingJudgment(timing + timingOffset);
			FlashJudgment(judgment);

			switch (judgment) {
				case Judgment.Perfect: perfectCount += 1; break;
				case Judgment.Great: greatCount += 1; break;
				case Judgment.Good: goodCount += 1; break;
				case Judgment.Bad: badCount += 1; break;
				case Judgment.Miss: missCount += 1; break;
			}

			var emitter = GetParticleEmitterFromJudgment(judgment);
			emitter.Emit(block.rect.anchoredPosition);

			bool shouldKeepCombo = CheckShouldKeepComboFromJudgment(judgment);
			if (shouldKeepCombo) {
				combo += 1;
				FlashCombo();
			} else {
				combo = 0;
				ClearCombo();
			}

			int noteScore = (int)(scoreBase * GetBlockTypeScoreMultipler(block.type) * GetJudgmentScoreMultiplier(judgment) * GetComboScoreMultiplier(combo));
			score += noteScore;
			FlashScore(noteScore);

			accuracyFactor += GetJudgmentAccuracyContribution(judgment);
			perfectAccuracyFactor += GetJudgmentAccuracyContribution(Judgment.Perfect);
			accuracy = accuracyFactor / perfectAccuracyFactor;
			FlashAccuracy();

			flashControllers[block.lane].Dim(1);
			anim_.New(backgroundRect).ScaleFromTo(backgroundRect, new Vector3(1.2f, 1.2f, 1), Vector3.one, .4f, EsType.Linear)
				.RotateFromTo(backgroundRect, -2, 0, .2f, EsType.Linear);
		}

		public void CountScoreForLongBlockTail(float timing, Block block) {
			var judgment = GetTimingJudgment(timing + timingOffset);
			FlashJudgment(judgment);

			switch (judgment) {
				case Judgment.Perfect: perfectCount += 1; break;
				case Judgment.Great: greatCount += 1; break;
				case Judgment.Good: goodCount += 1; break;
				case Judgment.Bad: badCount += 1; break;
				case Judgment.Miss: missCount += 1; break;
			}

			var emitter = GetParticleEmitterFromJudgment(judgment);
			emitter.Emit(block.rect.anchoredPosition);

			bool shouldKeepCombo = CheckShouldKeepComboFromJudgment(judgment);
			if (shouldKeepCombo) {
				combo += 1;
				FlashCombo();
			} else {
				combo = 0;
				ClearCombo();
			}

			int noteScore = (int)(scoreBase * GetBlockTypeScoreMultipler(Block.BlockType.Instant) * GetJudgmentScoreMultiplier(judgment) * GetComboScoreMultiplier(combo));
			score += noteScore;
			FlashScore(noteScore);

			accuracyFactor += GetJudgmentAccuracyContribution(judgment);
			perfectAccuracyFactor += GetJudgmentAccuracyContribution(Judgment.Perfect);
			accuracy = accuracyFactor / perfectAccuracyFactor;
			FlashAccuracy();
		}

		public void CountMiss(Block block) {
			//			Debug.Log("Miss!");
			FlashJudgment(Judgment.Miss);
			combo = 0;
			ClearCombo();
			missCount += 1;

			perfectAccuracyFactor += GetJudgmentAccuracyContribution(Judgment.Perfect);
			accuracy = accuracyFactor / perfectAccuracyFactor;
			FlashAccuracy();
		}

		float GetBlockTypeScoreMultipler(Block.BlockType type) {
			switch (type) {
				case Block.BlockType.Instant: return .5f;
				case Block.BlockType.Short: return 1f;
				case Block.BlockType.Long: return 1.25f;
				default: throw new System.NotImplementedException();
			}
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

		Judgment GetTimingJudgment(float timing) {
			if (timing < perfectTiming) return Judgment.Perfect;
			if (timing < greatTiming) return Judgment.Great;
			if (timing < goodTiming) return Judgment.Good;
			if (timing < badTiming) return Judgment.Bad;
			return Judgment.Miss;
		}

		float GetJudgmentScoreMultiplier(Judgment judgment) {
			switch (judgment) {
				case Judgment.Perfect: return 1f;
				case Judgment.Great: return .88f;
				case Judgment.Good: return .8f;
				case Judgment.Bad: return .4f;
				default: return 0;
			}
		}

		float GetJudgmentAccuracyContribution(Judgment judgment) {
			switch (judgment) {
				case Judgment.Perfect: return 300;
				case Judgment.Great: return 200;
				case Judgment.Good: return 100;
				case Judgment.Bad: return 50;
				default: return 0;
			}
		}

		ParticleBurstEmitter GetParticleEmitterFromJudgment(Judgment judgment) {
			switch (judgment) {
				case Judgment.Perfect: return perfectEmitter;
				case Judgment.Great: return greatEmitter;
				case Judgment.Good: return goodEmitter;
				default: return badEmitter;
			}
		}

		bool CheckShouldKeepComboFromJudgment(Judgment judgment) {
			switch (judgment) {
				case Judgment.Perfect:
				case Judgment.Great:
				case Judgment.Good: return true;
				default: return false;
			}
		}

		string GetJudgmentText(Judgment judgment) {
			switch (judgment) {
				case Judgment.Perfect: return "PERFECT";
				case Judgment.Great: return "GREAT";
				case Judgment.Good: return "GOOD";
				case Judgment.Bad: return "BAD";
				default: return "MISS";
			}
		}

		Color GetJudgmentColor(Judgment judgment) {
			switch (judgment) {
				case Judgment.Perfect: return perfectColor;
				case Judgment.Great: return greatColor;
				case Judgment.Good: return goodColor;
				case Judgment.Bad: return badColor;
				default: return missColor;
			}
		}

		void FlashJudgment(Judgment judgment) {
			string str = GetJudgmentText(judgment);
			judgmentText.text = str;
			var color = GetJudgmentColor(judgment);
			judgmentText.color = color;
			anim_.New(judgmentText)
				.ScaleFromTo(judgmentText.transform, new Vector3(1.1f, 1.1f, 1.1f), Vector3.one, .2f, 0)
				.FadeFromTo(judgmentText, 1, .5f, .2f, 0);
			progressBar.SetStrock(color);
		}

		void FlashCombo() {
			if (combo > maxCombo) maxCombo = combo;
			if (combo > 5) {
				comboText.text = string.Format("{0:N0} COMBO", combo);
				anim_.New(comboText)
					.ScaleFromTo(comboText.transform, new Vector3(1.2f, 1.2f, 1.2f), Vector3.one, .2f, 0)
					.FadeFromTo(comboText, 1, .5f, .2f, 0);
			}
		}

		void ClearCombo() {
			anim_.New(comboText)
				//				.ScaleFromTo(comboText.transform, new Vector3(1.2f, 1.2f, 1.2f), Vector3.one, .2f, 0)
				.FadeTo(comboText, 0, .2f, 0);
		}

		void FlashScore(int addition) {
			scoreText.text = string.Format("{0:N0}", score);
			scoreAdditionText.text = string.Format("+{0:N0}", addition);
			anim_.New(scoreText)
				.ScaleFromTo(scoreText.transform, new Vector3(1.3f, 1.3f, 1.3f), Vector3.one, .2f, 0);
			//				.FadeFromTo(scoreText, 1, .5f, .2f, 0);
			anim_.New(scoreAdditionText)
				.FadeOutFromOne(scoreAdditionText, .2f, 0)
				.ScaleFromTo(scoreAdditionText.transform, new Vector3(1.2f, 1.2f, 1.2f), Vector3.one, .2f, 0)
				.MoveFromTo(scoreAdditionText.rectTransform, new Vector2(100, -20), new Vector2(0, -20), .2f, EsType.CubicIn);
		}

		void FlashAccuracy() {
			accuracyText.text = string.Format("{0:P}", accuracy);
			anim_.New(accuracyText)
				.ScaleFromTo(accuracyText.transform, new Vector3(1.1f, 1.1f, 1.1f), Vector3.one, .2f, 0);
		}
	}
}
