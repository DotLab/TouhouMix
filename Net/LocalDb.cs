using System.IO;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using Jsonf;
using Systemf;
using System.Linq;

namespace TouhouMix.Net {
  public sealed class LocalDb {
    const string DB_FOLDER = "Db";

    public const string COLLECTION_MIDIS = "midis";
    public const string COLLECTION_SONGS = "songs";
    public const string COLLECTION_ALBUMS = "albums";
    public const string COLLECTION_PERSONS = "persons";

    string rootPath;
    JsonContext json;

    public void Init() {
      rootPath = Path.Combine(UnityEngine.Application.persistentDataPath, DB_FOLDER);
      json = new JsonContext();

      new FileInfo(Path.Combine(rootPath, COLLECTION_MIDIS, "test")).Directory.Create();
      new FileInfo(Path.Combine(rootPath, COLLECTION_SONGS, "test")).Directory.Create();
      new FileInfo(Path.Combine(rootPath, COLLECTION_ALBUMS, "test")).Directory.Create();
      new FileInfo(Path.Combine(rootPath, COLLECTION_PERSONS, "test")).Directory.Create();
    }

    public bool CheckDocExists(string collection, string id) {
      return File.Exists(GetDocFilePath(collection, id));
    }

    public JsonObj ReadDoc(string collection, string id) {
      return (JsonObj)json.Parse(File.ReadAllText(GetDocFilePath(collection, id), System.Text.Encoding.UTF8));
    }

    public T ReadDoc<T>(string collection, string id) {
      return json.Parse<T>(File.ReadAllText(GetDocFilePath(collection, id), System.Text.Encoding.UTF8));
    }

    //public void WriteDoc(string collection, string id, JsonObj doc) {
    //  File.WriteAllText(GetDocFilePath(collection, id), json.Stringify(doc), System.Text.Encoding.UTF8);
    //}

    public void WriteDoc(string collection, string id, string text) {
      File.WriteAllText(GetDocFilePath(collection, id), text, System.Text.Encoding.UTF8);
    }

    public void WriteDoc(string collection, string id, object obj) {
      File.WriteAllText(GetDocFilePath(collection, id), json.Stringify(obj), System.Text.Encoding.UTF8);
    }

    public string[] GetAllDocIds(string collection) {
      return Directory.GetFiles(Path.Combine(rootPath, collection))
        .Select(x => Path.GetFileName(x)).ToArray();
    }

    public string GetDocFilePath(string collection, string id) {
      return Path.Combine(rootPath, collection, id);
    }
  }
}
