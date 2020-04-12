using UnityEngine;
using Uif;
using Uif.Settables;
using Uif.Tasks;

namespace TouhouMix.Levels {
	public abstract class PageScheduler<T> : MonoBehaviour, IPageScheduler<T> where T : ILevelScheduler {
		protected AnimationManager anim;
		protected GameScheduler game;

		protected T level;

		public CanvasGroup group;

		public virtual void Init(T level) {
			anim = AnimationManager.instance;
			game = GameScheduler.instance;

			this.level = level;

			group = GetComponent<CanvasGroup>();	
		}

		public virtual void Back() {
			level.Pop();
		}

		public virtual void Enable() {
			gameObject.SetActive(true);
		}

		public virtual void Disable() {
			gameObject.SetActive(false);
		}
	}
}