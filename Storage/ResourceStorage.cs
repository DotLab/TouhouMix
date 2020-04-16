using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;
using System.Linq;
using Systemf;
//using TouhouMix.Storage.Protos.Resource;
using TouhouMix.Net;

namespace TouhouMix.Storage {
	public sealed class ResourceStorage {
		const string LANG_OPTION_LIST_FILE_PATH = "LangOptionList";

		static string LoadText(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).text;
		}

		static byte[] LoadBytes(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).bytes;
		}

		public readonly List<Protos.Api.MidiProto> midiProtoList = new List<Protos.Api.MidiProto>();
		public readonly List<Protos.Api.SongProto> songProtoList = new List<Protos.Api.SongProto>();
		public readonly List<Protos.Api.AlbumProto> albumProtoList = new List<Protos.Api.AlbumProto>();
		public readonly List<Protos.Api.PersonProto> personProtoList = new List<Protos.Api.PersonProto>();
		public readonly Dictionary<string, string> coverUrlById = new Dictionary<string, string>();

		public readonly List<Protos.Resource.LangOptionProto> langOptionList = new List<Protos.Resource.LangOptionProto>();
		public readonly Dictionary<string, Protos.Resource.LangOptionProto> langOptionDictByLang = new Dictionary<string, Protos.Resource.LangOptionProto>();
		public readonly Dictionary<string, Protos.Resource.LangOptionProto> langOptionDictByName = new Dictionary<string, Protos.Resource.LangOptionProto>();
		public readonly Dictionary<int, Protos.Resource.LangOptionProto> langOptionDictByIndex = new Dictionary<int, Protos.Resource.LangOptionProto>();

		public void Init(Levels.GameScheduler game) {
			LoadMidis();
			LoadLangOptions();
		}

		/// <summary>
		/// Need localDb and translationService
		/// </summary>
		public void LoadMidis() {
			var db = Levels.GameScheduler.instance.localDb;
			midiProtoList.Clear();  midiProtoList.AddRange(db.ReadAllDocs<Protos.Api.MidiProto>(Net.LocalDb.COLLECTION_MIDIS));
			songProtoList.Clear(); songProtoList.AddRange(db.ReadAllDocs<Protos.Api.SongProto>(Net.LocalDb.COLLECTION_SONGS));
			albumProtoList.Clear(); albumProtoList.AddRange(db.ReadAllDocs<Protos.Api.AlbumProto>(Net.LocalDb.COLLECTION_ALBUMS));
			personProtoList.Clear(); personProtoList.AddRange(db.ReadAllDocs<Protos.Api.PersonProto>(Net.LocalDb.COLLECTION_PERSONS));
			LoadCustomMidis();
			GenerateFakeAlbums();
		}

		void GenerateFakeAlbums() {
			var albumDict = new Dictionary<string, Protos.Api.AlbumProto>();
			var songDict = new Dictionary<string, Protos.Api.SongProto>();

			foreach (var midi in midiProtoList) {
				if (midi.songId != null) {
					continue;
				}

				string albumName = midi.sourceAlbumName == null ? "Unknown".Translate() : midi.sourceAlbumName;
				string songName = midi.sourceSongName == null ? "Unknown".Translate() : midi.sourceSongName;
				string songId = GetFakeSongId(albumName, songName);
				
				if (midi.coverUrl != null) {
					coverUrlById[albumName] = midi.coverUrl;
					coverUrlById[songId] = midi.coverUrl;
				}

				if (!albumDict.TryGetValue(albumName, out _)) {
					Protos.Api.AlbumProto album = new Protos.Api.AlbumProto { _id = albumName, name = albumName, date = System.DateTime.Now.ToString() };
					albumDict.Add(album.name, album);
					albumProtoList.Add(album);
				}

				if (!songDict.TryGetValue(songId, out _)) {
					Protos.Api.SongProto song = new Protos.Api.SongProto { _id = songId, albumId = albumName, name = songName, track = 0 };
					songDict.Add(songId, song);
					songProtoList.Add(song);
				}

				midi.songId = songId;
			}
		}

		string GetFakeSongId(string albumName, string songName) {
			UnityEngine.Debug.Log(songName);
			return string.Format("{0}/{1}", albumName, songName);
		}

		void LoadLangOptions() {
			langOptionList.AddRange(UnityEngine.JsonUtility.FromJson<Protos.Resource.LangOptionListProto>(LoadText(LANG_OPTION_LIST_FILE_PATH)).langOptionList);
			for (int i = 0; i < langOptionList.Count; i++) {
				var option = langOptionList[i];
				option.index = i;
				langOptionDictByLang.Add(option.lang, option);
				langOptionDictByName.Add(option.name, option);
				langOptionDictByIndex.Add(option.index, option);
			}
		}

		void LoadCustomMidis() {
			string[] paths = System.IO.Directory.GetFiles(UnityEngine.Application.persistentDataPath, "*.mid");
			if (paths.Length == 0) return;

			foreach (string path in paths) {
				var fileName = System.IO.Path.GetFileName(path);
				var midiProto = new Protos.Api.MidiProto {
					_id = path,
					name = fileName,
					sourceSongName = "Unknown".Translate(),
					sourceAlbumName = "Local Midis".Translate(),
				};
				midiProtoList.Add(midiProto);
			}
		}

		public static void DecompressMidiBundle() {
			string bundleId = "202004110825";
			if (UnityEngine.PlayerPrefs.GetString("installedMidiBundleId", "") == bundleId) {
				return;
			}
			UnityEngine.PlayerPrefs.SetString("installedMidiBundleId", bundleId);

			UnityEngine.Debug.Log("Decompressing MidiBundle " + bundleId);
			string dataPath = UnityEngine.Application.persistentDataPath;
			byte[] bytes = UnityEngine.Resources.Load<UnityEngine.TextAsset>("MidiBundle").bytes;
			using (var stream = new System.IO.MemoryStream(bytes)) {
				new UnityFastZip().ExtractZip(stream, dataPath, UnityFastZip.Overwrite.Always, null, null, null, true, true);
			}
			UnityEngine.Debug.Log("MidiBundle decompressed");
		}

		public static byte[] ReadMidiBytes(Protos.Api.MidiProto midi) {
			return System.IO.File.Exists(midi._id) ? System.IO.File.ReadAllBytes(midi._id) :
				System.IO.File.ReadAllBytes(System.IO.Path.Combine(WebCache.instance.rootPath, midi.hash));
		}

		public Protos.Api.AlbumProto QueryAlbumById(string albumId) {
			return albumProtoList.Find(x => x._id == albumId);
		}

		public Protos.Api.SongProto QuerySongById(string songId) {
			return songProtoList.Find(x => x._id == songId);
		}

		public Protos.Api.MidiProto QueryMidiById(string midiId) {
			return midiProtoList.Find(x => x._id == midiId);
		}

		public Protos.Api.MidiProto QueryNextMidiById(string midiId) {
			bool found = false;
			foreach (var album in QueryAllAlbums()) {
				foreach (var song in QuerySongsByAlbumId(album._id)) {
					foreach (var midi in QueryMidisBySongId(song._id)) {
						if (found) {
							return midi;
						}
						if (midi._id == midiId) {
							found = true;
						}
					}
				}
			}
			return QueryMidisBySongId(QuerySongsByAlbumId(QueryAllAlbums().First()._id).First()._id).First();
		}

		public Protos.Api.MidiProto QueryRandomMidi() {
			return midiProtoList[UnityEngine.Random.Range(0, midiProtoList.Count)];
		}

		public Protos.Api.PersonProto QueryPersonById(string personId) {
			return personProtoList.Find(x => x._id == personId);
		}

		public IEnumerable<Protos.Api.AlbumProto> QueryAllAlbums() {
		return albumProtoList.OrderBy(x => System.DateTime.Parse(x.date));
	}

	public IEnumerable<Protos.Api.SongProto> QuerySongsByAlbumId(string albumId) {
		return songProtoList.Where(x => x.albumId == albumId).OrderBy(x => x.track);
		}

		public IEnumerable<Protos.Api.MidiProto> QueryMidisBySongId(string songId) {
			return midiProtoList.Where(x => x.songId == songId).OrderBy(x => x.name);
		}

		public string QueryCoverUrlById(string docId) {
			if (coverUrlById.TryGetValue(docId, out string url)) {
				return url;
			} else {
				return null;
			}
		}
	}
}