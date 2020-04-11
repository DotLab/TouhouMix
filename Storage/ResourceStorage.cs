using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;
using System.Linq;
using Systemf;
//using TouhouMix.Storage.Protos.Resource;
using TouhouMix.Net;

namespace TouhouMix.Storage {
	public sealed class ResourceStorage {
		//const string AUTHORS_FILE_PATH = "Authors";
		//const string ALBUMS_FILE_PATH = "Albums";
		//const string SONGS_FILE_PATH = "Songs";
		//const string MIDI_HASH_LIST_FILE_PATH = "MidiHashList";
		const string LANG_OPTION_LIST_FILE_PATH = "LangOptionList";
		//readonly string[] MIDIS_FILE_PATH_LIST = {"MidisContrib", "MidisDmbn", "MidisDmbnNew"};

		static string LoadText(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).text;
		}

		static byte[] LoadBytes(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).bytes;
		}

		//public readonly List<AuthorProto> authorProtoList = new List<AuthorProto>();
		//public readonly List<AlbumProto> albumProtoList = new List<AlbumProto>();
		//public readonly List<SongProto> songProtoList = new List<SongProto>();
		//public readonly List<MidiProto> midiProtoList = new List<MidiProto>();
		//public readonly HashSet<string> midiHashSet = new HashSet<string>();
		//public readonly HashSet<string> customMidiPathSet = new HashSet<string>();

		//public readonly Dictionary<int, AuthorProto> authorProtoDict = new Dictionary<int, AuthorProto>();
		//public readonly Dictionary<int, AlbumProto> albumProtoDict = new Dictionary<int, AlbumProto>();
		//public readonly Dictionary<Tuple<int, int>, SongProto> songProtoDict = new Dictionary<Tuple<int, int>, SongProto>();
		//public readonly Dictionary<Tuple<int, int, string>, MidiProto> midiProtoDict = new Dictionary<Tuple<int, int, string>, MidiProto>();

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
			//authorProtoList.AddRange(UnityEngine.JsonUtility.FromJson<AuthorsProto>(LoadText(AUTHORS_FILE_PATH)).authorList);
			//albumProtoList.AddRange(UnityEngine.JsonUtility.FromJson<AlbumsProto>(LoadText(ALBUMS_FILE_PATH)).albumList);
			//songProtoList.AddRange(UnityEngine.JsonUtility.FromJson<SongsProto>(LoadText(SONGS_FILE_PATH)).songList);
			//foreach (string hash in UnityEngine.JsonUtility.FromJson<HashListProto>(LoadText(MIDI_HASH_LIST_FILE_PATH)).hashList) {
			//	midiHashSet.Add(hash);
			//}
			//for (int i = 0; i < MIDIS_FILE_PATH_LIST.Length; i++) {
			//	midiProtoList.AddRange(UnityEngine.JsonUtility.FromJson<MidisProto>(LoadText(MIDIS_FILE_PATH_LIST[i])).midiList);
			//}

			//foreach (var authorProto in authorProtoList) {
			//	authorProtoDict.Add(authorProto.author, authorProto);
			//}
			//foreach (var albumProto in albumProtoList) {
			//	albumProtoDict.Add(albumProto.album, albumProto);
			//}
			//foreach (var songProto in songProtoList) {
			//	songProtoDict.Add(Tuple.Create(songProto.album, songProto.song), songProto);
			//	albumProtoDict[songProto.album].songCount += 1;
			//}
			//foreach (var midiProto in midiProtoList) {
			//	midiProtoDict.Add(Tuple.Create(midiProto.album, midiProto.song, midiProto.name), midiProto);
			//	authorProtoDict[midiProto.author].midiCount += 1;
			//	albumProtoDict[midiProto.album].midiCount += 1;
			//	songProtoDict[Tuple.Create(midiProto.album, midiProto.song)].midiCount += 1;
			//}

			//LoadCustomMidis();

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
					sourceAlbumName = "Custom Midis".Translate(),
				};
				midiProtoList.Add(midiProto);
			}
		}

		public static void DecompressMidiBundle() {
			string bundleId = "20200411";
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

		class Test : ICSharpCode.SharpZipLib.Zip.IEntryFactory {
			public INameTransform NameTransform { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

			public ZipEntry MakeDirectoryEntry(string directoryName) {
				throw new System.NotImplementedException();
			}

			public ZipEntry MakeDirectoryEntry(string directoryName, bool useFileSystem) {
				throw new System.NotImplementedException();
			}

			public ZipEntry MakeFileEntry(string fileName) {
				throw new System.NotImplementedException();
			}

			public ZipEntry MakeFileEntry(string fileName, bool useFileSystem) {
				throw new System.NotImplementedException();
			}

			public string TransformDirectory(string name) {
				UnityEngine.Debug.Log(name);
				return name;
			}

			public string TransformFile(string name) {
				UnityEngine.Debug.Log(name);
				return name;
			}
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

		//public IEnumerable<object> QueryAlbums() {
		//	return albumProtoList.Where(x => x.album == 0 || x.midiCount > 0).Cast<object>();
		//}

		//public IEnumerable<object> QuerySongsByAlbum(int album) {
		//	return songProtoList.Where(x => x.album == album && x.midiCount > 0).Cast<object>();
		//}

		//public IEnumerable<object> QueryMidisByAlbumAndSong(int album, int song) {
		//	return midiProtoList.Where(x => x.album == album && x.song == song).Cast<object>();
		//}
	}
}