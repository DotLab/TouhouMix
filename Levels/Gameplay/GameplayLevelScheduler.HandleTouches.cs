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
		void ProcessMouse() {
			var position = Input.mousePosition.Vec2().Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);

			if (Input.GetMouseButtonDown(0)) {
				// find the nearest note to press
				gameplayManager.ProcessTouchDown(MOUSE_TOUCH_ID, position.x, position.y);
			} else if (Input.GetMouseButtonUp(0)) {
				// clean up hold and find the nearest perfect instant key to press
				gameplayManager.ProcessTouchUp(MOUSE_TOUCH_ID, position.x, position.y);
			} else if (Input.GetMouseButton(0)) {
				// update hold and find the perfect instant key to press
				gameplayManager.ProcessTouchHold(MOUSE_TOUCH_ID, position.x, position.y);
			}
		}

		void ProcessTouches() {
			var touchCount = Input.touchCount;

			for (int i = 0; i < touchCount; i++) {
				var touch = Input.GetTouch(i);
				var position = touch.position.Div(sizeWatcher.resolution).Mult(sizeWatcher.canvasSize);

				if (touch.phase == TouchPhase.Began) {
					// find the nearest note to press
					gameplayManager.ProcessTouchDown(touch.fingerId, position.x, position.y);
				} else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
					// clean up hold and find the nearest perfect instant key to press
					gameplayManager.ProcessTouchUp(touch.fingerId, position.x, position.y);
				} else {
					// update hold and find the perfect instant key to press
					gameplayManager.ProcessTouchHold(touch.fingerId, position.x, position.y);
				}
			}
		}
	}
}
