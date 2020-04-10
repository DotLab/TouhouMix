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

		public AnimationSequence Hide(AnimationSequence seq) {
			return seq.FadeOut(group, .25f, EsType.CubicOut)
				.ScaleTo(transform, Vector3.one * 2, .25f, EsType.CubicOut).Then().Call(Disable);
		}

		public AnimationSequence Show(AnimationSequence seq) {
			return seq.Call(Enable).FadeInFromZero(group, .25f, EsType.CubicOut)
				.ScaleFromTo(transform, Vector3.zero, Vector3.one, .25f, EsType.CubicOut);
		}
	}
}