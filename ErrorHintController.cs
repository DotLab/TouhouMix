using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using TouhouMix.Net;

namespace TouhouMix {
  public class ErrorHintController : MonoBehaviour {
    public Text errorText;
    public CanvasGroup errorGroup;

    const float TRANSITION_DURATION = .1f;

    private void Start() {
      errorGroup.alpha = 0;

      Levels.GameScheduler.instance.netManager.onNetErrorEvent += OnNetError;
    }

    private void OnDestroy() {
      Levels.GameScheduler.instance.netManager.onNetErrorEvent -= OnNetError;
    }

    void OnNetError(string error) {
      Levels.GameScheduler.instance.ExecuteOnMain(() => {
        HintError(error.TranslateVolatile());
      });
    }

    public void HintError(string error) {
      var seq = AnimationManager.instance.New(this);
      if (errorGroup.IsVisible()) {
        seq.FadeOut(errorGroup, TRANSITION_DURATION, 0)
          .ShiftTo(errorGroup.GetComponent<RectTransform>(), new Vector2(0, 10), TRANSITION_DURATION, EsType.CubicIn)
          .Then();
      }
      seq.Call(() => { errorText.text = error; })
        .FadeIn(errorGroup, TRANSITION_DURATION, 0)
        .MoveTo(errorGroup.GetComponent<RectTransform>(), new Vector2(0, 0), TRANSITION_DURATION, EsType.CubicOut);
      seq.Wait(3)
        .FadeOut(errorGroup, TRANSITION_DURATION, 0)
        .ShiftTo(errorGroup.GetComponent<RectTransform>(), new Vector2(0, 10), TRANSITION_DURATION, EsType.CubicIn);
    }
  }
}
