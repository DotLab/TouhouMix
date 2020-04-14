namespace TouhouMix.Levels.Gameplay {
	public interface IGameplayManager {
		void Init(GameplayLevelScheduler level);

		void GenerateBlocks();
		void UpdateBlocks();

		void ProcessTouchDown(int id, float x, float y);
		void ProcessTouchUp(int id, float x, float y);
		void ProcessTouchHold(int id, float x, float y);
	}
}