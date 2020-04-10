using Systemf;
using Uif;

namespace TouhouMix.Levels {
	public interface IPageScheduler<T> : IInitable<T> where T : ILevelScheduler {
		void Back();
		void Enable();
		void Disable();

		AnimationSequence Show(AnimationSequence seq);
		AnimationSequence Hide(AnimationSequence seq);
	}
}