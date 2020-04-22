using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Settables.Components;

namespace TouhouMix.Levels {
	public sealed class GameplayResultLevelScheduler : MonoBehaviour {
		public RawImage backgroundImage;

		public Text difficultyText;
		public Text midiNameText;
		public Text scoreText;
		public Text accuracyComboText;
		public Text countText;
		public Text statsText;

		public Text songNameText;

		public Text gradeText;

		public int love;
		public int vote;
		public MultiGraphicColorSettable loveButtonColor;
		public MultiGraphicColorSettable upButtonColor;
		public MultiGraphicColorSettable downButtonColor;
		public Color loveColor;
		public Color upColor;
		public Color downColor;

		[Space]
		public Cutoff[] gradeCutoffs;

		[System.Serializable]
		public sealed class Cutoff {
			public string grade;
			public float cutoff;
		}

		void Start() {
			var game = GameScheduler.instance;

			if (game.backgroundTexture != null) {
				backgroundImage.texture = game.backgroundTexture;
			}

			difficultyText.text = ((Storage.Protos.Json.V1.GameplayConfigProto.DifficaultyPresetEnum)(game.gameplayConfig.difficultyPreset)).ToString();
			midiNameText.text = game.title;
			scoreText.text = game.score.ToString("N0");
			accuracyComboText.text = string.Format("<size=18><b>{0}</b></size>  {1:F2}% accuracy  {2:N0} max combo",
					GetGrade(game.accuracy), game.accuracy * 100, game.maxComboCount);
			countText.text = string.Format("Perfect {0:N0}     Great {1:N0}     Good {2:N0}    Bad {3:N0}    Miss {4:N0}",
					game.perfectCount, game.greatCount, game.goodCount, game.badCount, game.missCount);
			statsText.text = string.Format("Early {0:N0}     Late {1:N0}     ATE  {2:F3}s     STE {3:F3}s",
					game.earlyCount, game.lateCount, game.offsetAverage, game.offsetStd);

			songNameText.text = game.subtitle;

			gradeText.text = GetGrade(game.accuracy);
		}

		public string GetGrade(float accuracy) {
			for (int i = 0; i < gradeCutoffs.Length; i++) {
				var cutoff = gradeCutoffs[i];
				if (accuracy >= cutoff.cutoff) {
					return cutoff.grade;
				}
			}
			return "?";
		}

		public void OnMenuButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.SONG_SELECT_LEVEL_BUILD_INDEX);
		}

		public void OnAgainButtonClicked() {
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
		}

		public void OnNextButtonClicked() {
			LoadMidiAndPlay(GameScheduler.instance.resourceStorage.QueryNextMidiById(GameScheduler.instance.midiId));
		}

		public void OnRandomButtonClicked() {
			LoadMidiAndPlay(GameScheduler.instance.resourceStorage.QueryRandomMidi());
		}

		void LoadMidiAndPlay(Storage.Protos.Api.MidiProto midi) {
			var game = GameScheduler.instance;
			game.midiId = midi._id;
			game.midiFile = new Midif.V3.MidiFile(Storage.ResourceStorage.ReadMidiBytes(midi));
			game.noteSequenceCollection = new Midif.V3.NoteSequenceCollection(game.midiFile);
			game.title = midi.name;
			game.subtitle = string.Format("{0} • {1}", midi.sourceAlbumName, midi.sourceSongName);
			UnityEngine.SceneManagement.SceneManager.LoadScene(GameScheduler.GAMEPLAY_LEVEL_BUILD_INDEX);
		}

		public void OnLoveButtonClicked() {
			if (love == 0) {
				love = 1;
			} else {
				love = 0;
			}
			GameScheduler.instance.netManager.ClAppMidiAction(MiscHelper.GetHexEncodedMd5Hash(GameScheduler.instance.midiFile.bytes), "love", love, (err, res) => {
				if (err != null) {
					Debug.LogError(err);
					return;
				}
				GameScheduler.instance.ExecuteOnMain(() => loveButtonColor.Set(love == 1 ? loveColor : Color.white));
			});
		}

		public void OnUpButtonClicked() {
			GameScheduler.instance.netManager.ClAppMidiAction(MiscHelper.GetHexEncodedMd5Hash(GameScheduler.instance.midiFile.bytes), "vote", 1, (err, res) => {
				if (err != null) {
					Debug.LogError(err);
					return;
				}
				GameScheduler.instance.ExecuteOnMain(() => {
					upButtonColor.Set(upColor);
					downButtonColor.Set(Color.white);
				});
			});
		}

		public void OnDownButtonClicked() {
			GameScheduler.instance.netManager.ClAppMidiAction(MiscHelper.GetHexEncodedMd5Hash(GameScheduler.instance.midiFile.bytes), "vote", -1, (err, res) => {
				if (err != null) {
					Debug.LogError(err);
					return;
				}
				GameScheduler.instance.ExecuteOnMain(() => {
					upButtonColor.Set(Color.white);
					downButtonColor.Set(downColor);
				});
			});
		}
	}
}

