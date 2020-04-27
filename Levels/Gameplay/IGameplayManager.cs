namespace TouhouMix.Levels.Gameplay {
	public interface IGameplayManager {
		void Init(GameplayLevelScheduler level);

		void GenerateBlocks();
		void UpdateBlocks();

		void ProcessTouchDown(int id, float x, float y);
		void ProcessTouchUp(int id, float x, float y);
		void ProcessTouchHold(int id, float x, float y);

		void ProcessLaneDown(int id, int lane);
		void ProcessLaneUp(int id, int lane);
		void ProcessLaneHold(int id, int lane);
	}
}