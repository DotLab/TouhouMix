using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Uif;
using Uif.Settables;
using Uif.Tasks;
using TouhouMix.Net;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;

namespace TouhouMix.Levels.SongSelect {
  public class UserPageScheduler : MonoBehaviour {
    public CanvasGroup loginPopupGroup;
    public CanvasGroup loginButtonGroup;
    public CanvasGroup userInfoGroup;

    public InputField usernameInput;
    public InputField passwordInput;
    public Text nameText;
    public RawImage avatarIamge;

    AnimationManager anim;
    NetManager net;
    WebCache web;
    GameScheduler game;

    public void Start() {
      game = GameScheduler.instance;
      anim = AnimationManager.instance;
      net = GameScheduler.instance.netManager;
      web = WebCache.instance;

      if (game.userObj != null) {
        DisplayUserInfo(game.userObj);
      } else if (!string.IsNullOrEmpty(game.username)) {
        net.ClAppUserLogin(game.username, game.password, (error, data) => {
          if (!string.IsNullOrEmpty(error)) {
            return;
          }

          game.userObj = (JsonObj)data;
          DisplayUserInfo((JsonObj)data);
        });
      }
    }

    [Button]
    public void OnLoginToggleClicked() {
      if (loginPopupGroup.alpha > 0) {
        anim.New(this).FadeOut(loginPopupGroup, .2f, 0).Then()
          .Call(() => { loginPopupGroup.gameObject.SetActive(false); });
      } else {
        anim.New(this)
          .Call(() => { loginPopupGroup.gameObject.SetActive(true); })
          .FadeIn(loginPopupGroup, .2f, 0);
      }
    }

    [Button]
    public void OnLoginButtonClicked() {
      if (!net.available) {
        return;
      }

      string username = usernameInput.text;
      string password = passwordInput.text;
      if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) {
        return;
      }

      net.ClAppUserLogin(username, password, (error, data) => {
        if (!string.IsNullOrEmpty(error)) {
          return;
        }

        game.username = username;
        game.password = password;

        game.userObj = (JsonObj)data;
        DisplayUserInfo((JsonObj)data);
      });
    }

    public void OnRegisterButtonClicked() {
      Application.OpenURL("https://asia.thmix.org");
    }

    void DisplayUserInfo(JsonObj userDict) {
      anim.New(this).FadeOut(loginPopupGroup, .2f, 0)
        .FadeOut(loginButtonGroup, .2f, 0).Then()
        .Call(() => {
          loginPopupGroup.gameObject.SetActive(false);
          loginButtonGroup.gameObject.SetActive(false);
          userInfoGroup.gameObject.SetActive(true);
        })
        .FadeIn(userInfoGroup, .2f, 0);

      game.ExecuteOnMain(() => {
        nameText.text = (string)userDict["name"];
        if (userDict.ContainsKey("avatarUrl")) {
          web.LoadTexture((string)userDict["avatarUrl"], job => {
            avatarIamge.texture = job.GetData();
          });
        }
      });
    }
  }
}
