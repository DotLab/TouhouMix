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

		public float albumSelectScrollViewPositionY { 
			get { return game_.uiStateProto.albumSelectScrollViewPositionY; } 
			set { game_.uiStateProto.albumSelectScrollViewPositionY = value; } 
		}
		public int selectedAlbum { 
			get { return game_.uiStateProto.selectedAlbum; }
			set { game_.uiStateProto.selectedAlbum = value; } 
		}
		public float songSelectScrollViewPositionY { 
			get { return game_.uiStateProto.songSelectScrollViewPositionY; } 
			set { game_.uiStateProto.songSelectScrollViewPositionY = value; } 
		}
		public int selectedSong { 
			get { return game_.uiStateProto.selectedSong; }
			set { game_.uiStateProto.selectedSong = value; } 
		}
		public float midiSelectScrollViewPositionY { 
			get { return game_.uiStateProto.midiSelectScrollViewPositionY; } 
			set { game_.uiStateProto.midiSelectScrollViewPositionY = value; } 
		}
		public string selectedMidi { 
			get { return game_.uiStateProto.selectedMidi; }
			set { game_.uiStateProto.selectedMidi = value; } 
		}

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
			synthConfigPage.Disable();

			songSelectPage.Init(this);
			songSelectPage.Enable();
		}

		public void OnBackButtonClicked() {
			pageStack_.Peek().Back();
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