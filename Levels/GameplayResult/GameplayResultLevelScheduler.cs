﻿using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;

namespace TouhouMix.Levels {
	public sealed class GameplayResultLevelScheduler : MonoBehaviour {
		public Text titleText;
		public Text subtitleText;

		public Text scoreText;
		public Text newBestText;

		public Text perfectCountText;
		public Text greatCountText;
		public Text goodCountText;
		public Text badCountText;
		public Text missCountText;
		public Text maxComboCountText;
		public Text fullComboText;
		public Text accuracyText;

		public Text gradeText;

		[Space]
		public Cutoff[] gradeCutoffs;

		[System.Serializable]
		public sealed class Cutoff {
			public string grade;
			public float cutoff;
		}

		void Start() {
			var game = GameScheduler.instance;

			titleText.text = game.title;
			subtitleText.text = game.subtitle;

			scoreText.text = game.score.ToString("N0");
			newBestText.text = "";

			perfectCountText.text = game.perfectCount.ToString("N0");
			greatCountText.text = game.greatCount.ToString("N0");
			goodCountText.text = game.goodCount.ToString("N0");
			badCountText.text = game.badCount.ToString("N0");
			missCountText.text = game.missCount.ToString("N0");
			maxComboCountText.text = game.maxComboCount.ToString("N0") + "x";
			fullComboText.gameObject.SetActive(game.missCount == 0 && game.badCount == 0);
			AnimationManager.instance.New()
				.ScaleTo(fullComboText.transform, new Vector3(1.2f, 1.2f, 1.2f), .1f, 0).Then()
				.ScaleTo(fullComboText.transform, Vector3.one, .2f, 0).Then().Repeat();

			accuracyText.text = game.accuracy.ToString("P2");

			for (int i = 0; i < gradeCutoffs.Length; i++) {
				var cutoff = gradeCutoffs[i];
				if (game.accuracy >= cutoff.cutoff) {
					gradeText.text = cutoff.grade;
					break;
				}
			}
		}

		public void OnMenuButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.SONG_SELECT_LEVEL_BUILD_INDEX);
		}

		public void OnAgainButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
		}
	}
}

