using UnityEngine;

namespace TouhouMix {
	public class RefContainer : MonoBehaviour {
		public static RefContainer instance;

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