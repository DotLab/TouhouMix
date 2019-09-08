using UnityEngine;
using Uif;
using Uif.Settables;
using Uif.Tasks;

namespace TouhouMix.Levels {
	public abstract class PageScheduler<T> : MonoBehaviour, IPageScheduler<T> where T : ILevelScheduler {
		protected AnimationManager anim_;
		protected GameScheduler game_;

		protected T level_;

		protected CanvasGroup group_;

		public virtual void Init(T level) {
			anim_ = AnimationManager.instance;
			game_ = GameScheduler.instance;

			level_ = level;

			group_ = GetComponent<CanvasGroup>();	
		}

		public virtual void Back() {}

		public virtual void Enable() {
			gameObject.SetActive(true);
		}

		public virtual void Disable() {
			gameObject.SetActive(false);
		}

		public virtual AnimationSequence Hide(AnimationSequence seq) {
			return seq.FadeOut(group_, .25f, 0).Then().Call(Disable);
		}

		public virtual AnimationSequence Show(AnimationSequence seq) {
			return seq.Call(Enable).FadeInFromZero(group_, .25f, 0);
		}
	}
}