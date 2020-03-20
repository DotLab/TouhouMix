﻿using System.Collections.Generic;
using System.Linq;
using Systemf;
using TouhouMix.Storage.Protos.Resource;

namespace TouhouMix.Storage {
	public sealed class ResourceStorage {
		const string AUTHORS_FILE_PATH = "Authors";
		const string ALBUMS_FILE_PATH = "Albums";
		const string SONGS_FILE_PATH = "Songs";
		const string MIDI_HASH_LIST_FILE_PATH = "MidiHashList";
		readonly string[] MIDIS_FILE_PATH_LIST = {"MidisContrib", "MidisDmbn", "MidisDmbnNew"};

		static string LoadText(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).text;
		}

		static byte[] LoadBytes(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).bytes;
		}

		public readonly List<AuthorProto> authorProtoList = new List<AuthorProto>();
		public readonly List<AlbumProto> albumProtoList = new List<AlbumProto>();
		public readonly List<SongProto> songProtoList = new List<SongProto>();
		public readonly List<MidiProto> midiProtoList = new List<MidiProto>();
		public readonly HashSet<string> midiHashSet = new HashSet<string>();
		public readonly HashSet<string> customMidiPathSet = new HashSet<string>();

		public readonly Dictionary<int, AuthorProto> authorProtoDict = new Dictionary<int, AuthorProto>();
		public readonly Dictionary<int, AlbumProto> albumProtoDict = new Dictionary<int, AlbumProto>();
		public readonly Dictionary<Tuple<int, int>, SongProto> songProtoDict = new Dictionary<Tuple<int, int>, SongProto>();
		public readonly Dictionary<Tuple<int, int, string>, MidiProto> midiProtoDict = new Dictionary<Tuple<int, int, string>, MidiProto>();

		public void Load() {
			authorProtoList.AddRange(UnityEngine.JsonUtility.FromJson<AuthorsProto>(LoadText(AUTHORS_FILE_PATH)).authorList);
			albumProtoList.AddRange(UnityEngine.JsonUtility.FromJson<AlbumsProto>(LoadText(ALBUMS_FILE_PATH)).albumList);
			songProtoList.AddRange(UnityEngine.JsonUtility.FromJson<SongsProto>(LoadText(SONGS_FILE_PATH)).songList);
			foreach (string hash in UnityEngine.JsonUtility.FromJson<HashListProto>(LoadText(MIDI_HASH_LIST_FILE_PATH)).hashList) {
				midiHashSet.Add(hash);
			}
			for (int i = 0; i < MIDIS_FILE_PATH_LIST.Length; i++) {
				midiProtoList.AddRange(UnityEngine.JsonUtility.FromJson<MidisProto>(LoadText(MIDIS_FILE_PATH_LIST[i])).midiList);
			}

			foreach (var authorProto in authorProtoList) {
				authorProtoDict.Add(authorProto.author, authorProto);
			}
			foreach (var albumProto in albumProtoList) {
				albumProtoDict.Add(albumProto.album, albumProto);
			}
			foreach (var songProto in songProtoList) {
				songProtoDict.Add(Tuple.Create(songProto.album, songProto.song), songProto);
				albumProtoDict[songProto.album].songCount += 1;
			}
			foreach (var midiProto in midiProtoList) {
				midiProtoDict.Add(Tuple.Create(midiProto.album, midiProto.song, midiProto.name), midiProto);
				authorProtoDict[midiProto.author].midiCount += 1;
				albumProtoDict[midiProto.album].midiCount += 1;
				songProtoDict[Tuple.Create(midiProto.album, midiProto.song)].midiCount += 1;
			}

			LoadCustomMidis();
		}

		public void LoadCustomMidis() {
			string[] paths = System.IO.Directory.GetFiles(UnityEngine.Application.persistentDataPath, "*.mid");
			if (paths.Length == 0) return;

			foreach (string path in paths) {
				if (customMidiPathSet.Contains(path)) {
					continue;
				}

				var fileName = System.IO.Path.GetFileName(path);
				var midiProto = new MidiProto {
					author = 0,
					album = 0,
					song = 1,
					name = fileName,
					path = path,
					isFile = true
				};
				midiProtoList.Add(midiProto);
				customMidiPathSet.Add(path);

				midiProtoDict.Add(Tuple.Create(midiProto.album, midiProto.song, midiProto.name), midiProto);
				authorProtoDict[midiProto.author].midiCount += 1;
				albumProtoDict[midiProto.album].midiCount += 1;
				songProtoDict[Tuple.Create(midiProto.album, midiProto.song)].midiCount += 1;
			}
		}

		public IEnumerable<object> QueryAlbums() {
			return albumProtoList.Where(x => x.album == 0 || x.midiCount > 0).Cast<object>();
		}

		public IEnumerable<object> QuerySongsByAlbum(int album) {
			return songProtoList.Where(x => x.album == album && x.midiCount > 0).Cast<object>();
		}

		public IEnumerable<object> QueryMidisByAlbumAndSong(int album, int song) {
			return midiProtoList.Where(x => x.album == album && x.song == song).Cast<object>();
		}
	}
}