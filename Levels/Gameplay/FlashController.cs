using UnityEngine;

public class FlashController : MonoBehaviour {
	public CanvasGroup group;
	public RectTransform rect;

	public float dimRate;
	public float dimRateDrift;

	float currentDimRate;
	float delayCounter;
	bool isFinished;

	void Update () {
		if (isFinished)
			return;
		if (group.alpha > 0) {
			delayCounter -= Time.deltaTime;
			group.alpha -= currentDimRate * Time.deltaTime;
			currentDimRate += dimRateDrift * Time.deltaTime;
		} else {
			isFinished = true;
			if (group.alpha != 0) group.alpha = 0;
		}
	}

	public void Dim(float alpha) {
		isFinished = false;
		group.alpha = alpha;
		currentDimRate = dimRate;
	}

	public void Sustain() {
		isFinished = false;
		currentDimRate = dimRate;
	}
}
