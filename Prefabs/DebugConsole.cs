using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DebugConsole : MonoBehaviour {
	static Text uiText;
	static int maxLineCount = 100;

	static readonly LinkedList<string> textLines = new LinkedList<string>();
	static string textString;

	static bool dirty;

	void Awake () {
		uiText = GetComponent<Text>(); uiText.text = "";
		maxLineCount = (int)(600.0f / uiText.fontSize);

		textLines.Clear();
	
		#if DEBUG
		WriteLine("Console ready - " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
		#endif
	}

	void OnEnable() {
		Application.logMessageReceived += HandleLog;
	}

	void OnDisable() {
		Application.logMessageReceived -= HandleLog;
	}

	void HandleLog(string logString, string stackTrace, LogType type) {
		WriteLine(logString);
		if (type != LogType.Log) {
			WriteLine(stackTrace);
		}
	}

	void FixedUpdate () {
		if (dirty) {
			dirty = false;

			textString = "";

			foreach (var line in textLines)
				textString += line + "\n";

			uiText.text = textString;
		}
	}

	public static void Clear () {
		textLines.Clear();

		dirty = true;
	}

	public static void Refresh (object o) {
		// Debug.Log(o);
		textLines.Last.Value = o.ToString();
	
		dirty = true;
	}

	public static void Write (object o) {
		// Debug.Log(o);
		textLines.Last.Value += o.ToString();
	
		dirty = true;
	}

	public static void WriteLine (object o = null) {
		o = o ?? "";

		// Debug.Log(o);

		textLines.AddLast(o.ToString());

		while (textLines.Count > maxLineCount)
			textLines.RemoveFirst();
	
		dirty = true;
	}

	// Alias for WriteLine(object o)
	public static void Log (object o = null) {
		#if DEBUG
		WriteLine(o);
		#endif
	}

	public static void Log (object o, string color) {
		#if DEBUG
		WriteLine("<color=" + color + ">" + o + "</color>");
		#endif
	}
}
