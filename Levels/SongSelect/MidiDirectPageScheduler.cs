using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TouhouMix.Net;
using TouhouMix.Storage;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using TouhouMix.Levels.SongSelect.MidiDirectPage;
using Jsonf;
using TouhouMix.Fonts;
using Uif;
using Uif.Settables;

namespace TouhouMix.Levels.SongSelect {
  public sealed class MidiDirectPageScheduler : PageScheduler<SongSelectLevelScheduler> {
    public InputField searchInputField;
    public InputField pageControlInputField;

    public RectTransform scrollContentRect;
    public RectTransform pageControlRowRect;
    
    public GameObject scrollItemPrefab;
    public GameObject scrollRowPrefab;
    public int itemsPerRow = 3;

    public string currentQuery;
    public int currentPage;

    NetManager net;
    WebCache web;
    ResourceStorage res;
    LocalDb db;

    public override void Init(SongSelectLevelScheduler level) {
      base.Init(level);
      net = game.netManager;
      web = WebCache.instance;
      res = game.resourceStorage;
      db = game.localDb;
    }

    public override void Enable() {
      base.Enable();
      pageControlInputField.text = currentPage.ToString();
      searchInputField.text = currentQuery.ToString();
      Fetch();
    }

    public override void Back() {
      base.Back();
      res.LoadCustomMidis();
    }

    void Fetch() {
      net.ClAppMidiListQuery(currentQuery, currentPage, (error, data) => {
        if (error != null) {
          Debug.Log(error);
          return;
        }

        var list = (JsonList)data;

        game.ExecuteOnMain(() => Render(list));
      });
    }

    void Render(JsonList midiList) {
      int count = midiList.Count;
      int rows = count / itemsPerRow;
      if (count % itemsPerRow != 0) {
        rows += 1;
      }

      int generatedRows = scrollContentRect.childCount - 1;

      for (int currentRow = 0; currentRow < rows; currentRow++) {
        if (currentRow < generatedRows) {
          var rowTrans = scrollContentRect.GetChild(currentRow);
          rowTrans.gameObject.SetActive(true);
          for (int j = 0; j < itemsPerRow; j++) {
            int i = currentRow * itemsPerRow + j;
            if (i < count) {
              var itemController = rowTrans.GetChild(j).GetComponent<MidiDirectScrollItemController>();
              itemController.gameObject.SetActive(true);
              RenderItem(itemController, (JsonObj)midiList[i]);
            } else {
              rowTrans.GetChild(j).gameObject.SetActive(false);
            }
          }
        } else {
          // Generate new row
          var rowTrans = Instantiate(scrollRowPrefab, scrollContentRect).transform;
          for (int j = 0; j < itemsPerRow; j++) {
            var itemController = Instantiate(scrollItemPrefab, rowTrans).GetComponent<MidiDirectScrollItemController>();
            RenderItem(itemController, (JsonObj)midiList[currentRow * itemsPerRow + j]);
          }
        }
      }

      for (int currentRow = rows; currentRow < generatedRows; currentRow++) {
        scrollContentRect.GetChild(currentRow).gameObject.SetActive(false);
      }

      pageControlRowRect.SetAsLastSibling();
    }

    void RenderItem(MidiDirectScrollItemController item, JsonObj midi) {
      item.nameText.text = (string)midi["name"];
      item.authorText.text = (string)midi["artistName"];
      item.uploaderText.text = "upload by " + (string)midi["uploaderName"];
      int touhouAlbumIndex = (int)(double)midi["touhouAlbumIndex"];
      int touhouSongIndex = (int)(double)midi["touhouSongIndex"];
      if (touhouAlbumIndex >= 0 && touhouSongIndex >= 0) {
        item.albumText.text = game.resourceStorage.albumProtoDict[touhouAlbumIndex].name;
        item.songText.text = game.resourceStorage.songProtoDict[
          new Systemf.Tuple<int, int>(touhouAlbumIndex, touhouSongIndex)].name;
      } else {
        item.albumText.text = (string)midi["sourceAlbumName"];
        item.songText.text = (string)midi["sourceSongName"];
      }

      if (midi.ContainsKey("coverUrl")) {
        string coverUrl = (string)midi["coverUrl"];
        web.LoadTexture(coverUrl, textureJob => {
          item.coverImageCutter.Cut(textureJob.GetData());
        });
      } else {
        item.coverImageCutter.Cut(item.defaultTexture);
      }

      item.downloadButton.onClick.RemoveAllListeners();
      item.downloadButton.onClick.AddListener(() => DownloadMidi(item, midi));

      string hash = JsonHelper.Get<string>(midi, "hash");
      if (web.CheckUrlFileExists(hash) || res.midiHashSet.Contains(hash)) {
        item.coverImageCutter.image.color = new Color(1, 1, 1, .5f);
        item.iconText.text = FontAwesome.Solid.CheckCircle;
      } else {
        item.coverImageCutter.image.color = new Color(1, 1, 1, 1);
        item.iconText.text = FontAwesome.Solid.CloudDownloadAlt;
      }
    }

    public void DownloadMidi(MidiDirectScrollItemController item, JsonObj midiInfo) {
      string hash = midiInfo.Get<string>("hash");
      if (web.CheckUrlFileExists(hash) || res.midiHashSet.Contains(hash)) {
        return;
      }

      item.iconText.text = FontAwesome.Solid.Spinner;
      anim.New(item)
        .RotateTo(item.iconText.transform, 360, 1, EsType.BackOut).Then()
        .RotateTo(item.iconText.transform, 0, 1, EsType.BackOut).Then().Repeat();

      net.ClAppMidiDownload(hash, (error, data) => {
        web.LoadNull(hash, (string)data, job => {
          db.WriteDoc(LocalDb.COLLECTION_MIDIS, midiInfo.Get<string>("id"), midiInfo);
          job.GetData();
          anim.Clear(item);
          item.coverImageCutter.image.color = new Color(1, 1, 1, .5f);
          item.iconText.transform.localRotation = Quaternion.identity;
          item.iconText.text = FontAwesome.Solid.CheckCircle;
        });
      });
    }

    public void Search(string query) {
      currentPage = 0;
      currentQuery = query;
      Fetch();
      pageControlInputField.text = currentPage.ToString();
      scrollContentRect.anchoredPosition = new Vector2(0, 0);
    }

    public void GoToPage(string page) {
      currentPage = int.Parse(page);
      Fetch();
      scrollContentRect.anchoredPosition = new Vector2(0, 0);
    }

    public void PreviousPage() {
      currentPage -= 1;
      Fetch();
      pageControlInputField.text = currentPage.ToString();
      scrollContentRect.anchoredPosition = new Vector2(0, 0);
    }

    public void NextPage() {
      currentPage += 1;
      Fetch();
      pageControlInputField.text = currentPage.ToString();
      scrollContentRect.anchoredPosition = new Vector2(0, 0);
    }
  }
}
