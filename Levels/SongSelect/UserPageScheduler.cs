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
    public RawImageCutter rawImageCutter;

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
        usernameInput.text = game.username;
        passwordInput.text = "";
        net.ClAppUserLogin(game.username, game.password, (error, data) => {
          if (!string.IsNullOrEmpty(error)) {
            return;
          }

          game.ExecuteOnMain(() => { 
            game.userObj = (JsonObj)data;
            DisplayUserInfo((JsonObj)data);
          });
        });
      } else {
        usernameInput.text = "";
        passwordInput.text = "";
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
        game.ExecuteOnMain(() => {
          if (!string.IsNullOrEmpty(error)) {
            usernameInput.text = "";
            passwordInput.text = "";
            return;
          }

          game.username = username;
          game.password = password;

          game.userObj = (JsonObj)data;
          DisplayUserInfo((JsonObj)data);
        });
      });
    }

    public void OnRegisterButtonClicked() {
      if (game.appConfig.networkEndpoint == 0) {
        Application.OpenURL("https://thmix.org/register");
      } else {
        Application.OpenURL("https://asia.thmix.org/register");
      }
    }

    void DisplayUserInfo(JsonObj userDict) {
      TopToolBarScheduler.FadeOutAndDeactivate(loginPopupGroup);
      loginButtonGroup.gameObject.SetActive(false);
      userInfoGroup.gameObject.SetActive(true);
      anim.New(userInfoGroup).FadeIn(userInfoGroup, .2f, 0);

      game.ExecuteOnMain(() => {
        nameText.text = (string)userDict["name"];
        if (userDict.ContainsKey("avatarUrl")) {
          web.LoadTexture((string)userDict["avatarUrl"], job => {
            rawImageCutter.Cut(job.GetKey(), job.GetData());
          });
        }
      });
    }
  }
}
