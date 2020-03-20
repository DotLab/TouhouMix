using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Uif;
using Uif.Tasks;

namespace TouhouMix.Levels.SongSelect {
	public sealed class SongSelectLevelScheduler : MonoBehaviour, ILevelScheduler {
		public Button backButton;

		[Space]
		public SongSelectPageScheduler songSelectPage;
		public MidiDetailPageScheduler midiDetailPage;
		public SynthConfigPageScheduler synthConfigPage;
		public MidiDirectPageScheduler midiDirectPage;
		public DownloadedSongSelectPageScheduler downloadedSongSelectPage;

		public float albumSelectScrollViewPositionY { 
			get { return game_.uiState.albumSelectScrollViewPositionY; } 
			set { game_.uiState.albumSelectScrollViewPositionY = value; } 
		}
		public int selectedAlbum { 
			get { return game_.uiState.selectedAlbum; }
			set { game_.uiState.selectedAlbum = value; } 
		}
		public float songSelectScrollViewPositionY { 
			get { return game_.uiState.songSelectScrollViewPositionY; } 
			set { game_.uiState.songSelectScrollViewPositionY = value; } 
		}
		public int selectedSong { 
			get { return game_.uiState.selectedSong; }
			set { game_.uiState.selectedSong = value; } 
		}
		public float midiSelectScrollViewPositionY { 
			get { return game_.uiState.midiSelectScrollViewPositionY; } 
			set { game_.uiState.midiSelectScrollViewPositionY = value; } 
		}
		public string selectedMidi { 
			get { return game_.uiState.selectedMidi; }
			set { game_.uiState.selectedMidi = value; } 
		}

		public DownloadedSongSelectPageScheduler.Midi selectedDownloadedMidi;

		readonly Stack<IPageScheduler<SongSelectLevelScheduler>> pageStack_ = new Stack<IPageScheduler<SongSelectLevelScheduler>>();
		GameScheduler game_;
		AnimationManager anim_;

		void Start() {
			game_ = GameScheduler.instance;
			anim_ = AnimationManager.instance;

			pageStack_.Push(songSelectPage);

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
		}

		public void OnBackButtonClicked() {
			pageStack_.Peek().Back();
		}

		public void OpenMidiDirectPage() {
			Push(midiDirectPage);
		}

		public void OnDownloadedButtonClicked() {
			Push(downloadedSongSelectPage);
		}

		public void EnableBackButton() {
			backButton.interactable = true;
		}

		public void DisableBackButton() {
			backButton.interactable = false;
		}

		public void Push(IPageScheduler<SongSelectLevelScheduler> page) {
			var topPage = pageStack_.Peek();

			DisableBackButton();
			pageStack_.Push(page);
			anim_.New()
				.Append(topPage.Hide)
				.Append(page.Show).Then()
				.Call(EnableBackButton);
		}

		public void Pop() {
			var topPage = pageStack_.Peek();

			DisableBackButton();
			pageStack_.Pop();
			anim_.New()
				.Append(topPage.Hide)
				.Append(pageStack_.Peek().Show).Then()
				.Call(EnableBackButton);
		}
	}
}