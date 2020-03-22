using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Sirenix.OdinInspector;

namespace TouhouMix {
	public sealed class TextTranslator : MonoBehaviour {
		public Text text;
		public bool auto = true;

		public string src;

		private void OnValidate() {
			text = GetComponent<Text>();
		}

		private void OnEnable() {
			if (auto) {
				Translate();
			}
		}

		private void Start() {
			if (auto) {
				Translate();
			}
		}

		[Button]
		public void Translate() {
			if (string.IsNullOrEmpty(src)) {
				src = text.text;
			}
			Levels.GameScheduler.instance.translationSevice.Translate(src, res => {
				Levels.GameScheduler.instance.ExecuteOnMain(() => text.text = res);
			});
		}
	}
}
