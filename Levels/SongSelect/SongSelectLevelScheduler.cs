using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Uif;
using Uif.Tasks;
using Uif.Settables;

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

		readonly Stack<PageScheduler<SongSelectLevelScheduler>> pageStack = new Stack<PageScheduler<SongSelectLevelScheduler>>();
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

		public void Push(PageScheduler<SongSelectLevelScheduler> page) {
			DisableBackButton();
			var topPage = pageStack.Peek();
			pageStack.Push(page);
			//anim.New()
			//	.Append(page.Show)
			//	.Append(topPage.Hide).Then()
			//	.Call(EnableBackButton);
			page.Enable();
			anim.New(this)
				.FadeOut(topPage.group, .25f, EsType.CubicOut)
				.ScaleTo(topPage.transform, Vector3.one * 2, .25f, EsType.CubicOut)
				.FadeInFromZero(page.group, .25f, EsType.CubicOut)
				.ScaleFromTo(page.transform, Vector3.zero , Vector3.one, .25f, EsType.CubicOut)
				.Then().Call(() => {
					topPage.Disable();
					EnableBackButton();
				});
		}

		public void Pop() {
			DisableBackButton();
			var topPage = pageStack.Peek();
			pageStack.Pop();
			var page = pageStack.Peek();
			//anim.New()
			//	.Append(pageStack.Peek().Show)
			//	.Append(topPage.Hide).Then()
			//	.Call(EnableBackButton);
			page.Enable();
			anim.New(this)
				.FadeOut(topPage.group, .25f, EsType.CubicOut)
				.ScaleTo(topPage.transform, Vector3.zero, .25f, EsType.CubicOut)
				.FadeInFromZero(page.group, .25f, EsType.CubicOut)
				.ScaleFromTo(page.transform, Vector3.one * 2, Vector3.one, .25f, EsType.CubicOut)
				.Then().Call(() => {
					topPage.Disable();
					EnableBackButton();
				});
		}
	}
}