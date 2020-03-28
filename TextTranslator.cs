using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Sirenix.OdinInspector;

namespace TouhouMix {
	public static class TextTranslatorTextExtension {
		public static void Translate(this Text self, string text) {
			Levels.GameScheduler.instance.translationSevice.Translate(text, res => {
				Levels.GameScheduler.instance.ExecuteOnMain(() => self.text = res);
			});
		} 
	}

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
			Translate(src);
		}

		public void Translate(string src) {
			this.src = src;
			if (Levels.GameScheduler.instance == null) {
				return;
			}
			Levels.GameScheduler.instance.translationSevice.Translate(src, res => {
				Levels.GameScheduler.instance.ExecuteOnMain(() => text.text = res);
			});
		}
	}
}
