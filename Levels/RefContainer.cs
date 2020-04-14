using UnityEngine;

namespace TouhouMix {
	public class RefContainer : MonoBehaviour {
		public static RefContainer instance;

		public Texture2D imageCutterDefaultTexture;
		public AudioClip buttonSound;

		void Awake() {
			if (instance == null) {
				instance = this;
				DontDestroyOnLoad(gameObject);
			} else {
				Destroy(gameObject);
			}
		}
	}
}