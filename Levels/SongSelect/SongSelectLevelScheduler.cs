using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Uif;
using Uif.Tasks;

namespace TouhouMix.Levels.SongSelect {
	public sealed class SongSelectLevelScheduler : MonoBehaviour, ILevelScheduler {
		public Button backButton;
		public RawImage backgroundImage;
		public Texture2D defaultBackgroundTexture;

		[Space]
		public SongSelectPageScheduler songSelectPage;
		public MidiDetailPageScheduler midiDetailPage;
		public SynthConfigPageScheduler synthConfigPage;
		public MidiDirectPageScheduler midiDirectPage;
		public DownloadedSongSelectPageScheduler downloadedSongSelectPage;
		public AppConfigPageScheduler appConfigPageScheduler;

		public float albumSelectScrollViewPositionY { 
			get { return game.uiState.albumSelectScrollViewPositionY; } 
			set { game.uiState.albumSelectScrollViewPositionY = value; } 
		}
		public string selectedAlbumId { 
			get { return game.uiState.selectedAlbumId; }
			set { game.uiState.selectedAlbumId = value; } 
		}
		public float songSelectScrollViewPositionY { 
			get { return game.uiState.songSelectScrollViewPositionY; } 
			set { game.uiState.songSelectScrollViewPositionY = value; } 
		}
		public string selectedSongId { 
			get { return game.uiState.selectedSongId; }
			set { game.uiState.selectedSongId = value; } 
		}
		public float midiSelectScrollViewPositionY { 
			get { return game.uiState.midiSelectScrollViewPositionY; } 
			set { game.uiState.midiSelectScrollViewPositionY = value; } 
		}
		public string selectedMidiId { 
			get { return game.uiState.selectedMidiId; }
			set { game.uiState.selectedMidiId = value; } 
		}

		//public DownloadedSongSelectPageScheduler.Midi selectedDownloadedMidi;

		readonly Stack<IPageScheduler<SongSelectLevelScheduler>> pageStack = new Stack<IPageScheduler<SongSelectLevelScheduler>>();
		GameScheduler game;
		AnimationManager anim;

		void Start() {
			game = GameScheduler.instance;
			anim = AnimationManager.instance;

			pageStack.Push(songSelectPage);

			synthConfigPage.Init(this);
			synthConfigPage.Disable();

			midiDetailPage.Init(this);
			midiDetailPage.Disable();

			songSelectPage.Init(this);
			songSelectPage.Enable();

			midiDirectPage.Init(this);
			midiDirectPage.Disable();

			downloadedSongSelectPage.Init(this);
			downloadedSongSelectPage.Disable();

			appConfigPageScheduler.Init(this);
			appConfigPageScheduler.Disable();
		}

		private void OnDisable() {
			StopAllCoroutines();
		}

		public void OnBackButtonClicked() {
			pageStack.Peek().Back();
		}

		public void OpenMidiDirectPage() {
			Push(midiDirectPage);
		}

		public void OnDownloadedButtonClicked() {
			Push(downloadedSongSelectPage);
		}

		public void OnAppConfigButtonClicked() {
			Push(appConfigPageScheduler);
		}

		public void EnableBackButton() {
			backButton.interactable = true;
		}

		public void DisableBackButton() {
			backButton.interactable = false;
		}

		public void Push(IPageScheduler<SongSelectLevelScheduler> page) {
			DisableBackButton();

			var topPage = pageStack.Peek();
			pageStack.Push(page);
			anim.New()
				.Append(page.Show)
				.Append(topPage.Hide).Then()
				.Call(EnableBackButton);
		}

		public void Pop() {
			DisableBackButton();

			var topPage = pageStack.Peek();
			pageStack.Pop();
			anim.New()
				.Append(pageStack.Peek().Show)
				.Append(topPage.Hide).Then()
				.Call(EnableBackButton);
		}
	}
}