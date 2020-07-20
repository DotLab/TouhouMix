using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using Midif.V3;
using Systemf;
using Uif.Extensions;
using Uif.Settables.Components;
using TouhouMix.Storage.Protos.Json.V1;

namespace TouhouMix.Levels.Gameplay {
	public sealed partial class GameplayLevelScheduler : MonoBehaviour {
		private Dictionary<int, Vector2> touchPositionDict = new Dictionary<int, Vector2>();
		private Dictionary<int, Vector2> touchPositionOldDict = new Dictionary<int, Vector2>();

		private Dictionary<int, int> touchLaneDict = new Dictionary<int, int>();
		private Dictionary<int, int> touchLaneOldDict = new Dictionary<int, int>();

		void HandleTouch(int touchId, Vector2 position) {
			if (touchPositionOldDict.ContainsKey(touchId)) {
				// Already down
				gameplayManager.ProcessTouchHold(touchId, position.x, position.y);
			} else {
				// First frame down
				gameplayManager.ProcessTouchDown(touchId, position.x, position.y);
			}
			touchPositionDict[touchId] = position;
		}

		void BounceTouchesUp() {
			foreach (var pair in touchPositionOldDict) {
				int touchId = pair.Key;
				if (!touchPositionDict.ContainsKey(touchId)) {
					// Old touch not in new dict: touch up
					var position = pair.Value;
					gameplayManager.ProcessTouchUp(touchId, position.x, position.y);
				}
			}

			touchPositionOldDict.Clear();
			var clearedDict = touchPositionOldDict;
			touchPositionOldDict = touchPositionDict;
			touchPositionDict = clearedDict;
		}

		void HandleLane(int touchId, int lane) {
			if (touchLaneOldDict.ContainsKey(touchId)) {
				// Already down
				gameplayManager.ProcessLaneHold(touchId, lane);
			} else {
				// First frame down
				gameplayManager.ProcessLaneDown(touchId, lane);
			}
			touchLaneDict[touchId] = lane;
		}

		void BounceLanesUp() {
			foreach (var pair in touchLaneOldDict) {
				int touchId = pair.Key;
				if (!touchLaneDict.ContainsKey(touchId)) {
					// Old touch not in new dict: touch up
					var lane = pair.Value;
					gameplayManager.ProcessLaneUp(touchId, lane);
				}
			}

			touchLaneOldDict.Clear();
			var clearedDict = touchLaneOldDict;
			touchLaneOldDict = touchLaneDict;
			touchLaneDict = clearedDict;
		}

		void ProcessMouse() {
			var position = Input.mousePosition.Vec2().Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);
			if (Input.GetMouseButton(0)) {
				HandleTouch(MOUSE_TOUCH_ID, position);
			}
			BounceTouchesUp();
		}

		void ProcessTouches() {
			var touchCount = Input.touchCount;

			for (int i = 0; i < touchCount; i++) {
				var touch = Input.GetTouch(i);
				var position = touch.position.Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);
				HandleTouch(touch.fingerId, position);
			}

			BounceTouchesUp();
		}

		void ProcessKeyboard() {
			foreach (var key in keyLaneDict.Keys) {
				int touchId = keyTouchIdDict[key];
				if (Input.GetKey(key)) {
					HandleLane(keyTouchIdDict[key], keyLaneDict[key]);
				}
			}

			BounceLanesUp();
		}
	}
}
