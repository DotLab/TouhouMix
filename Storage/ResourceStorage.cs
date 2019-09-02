using System.Collections.Generic;
using Systemf;
using TouhouMix.Storage.Protos.Resource;
using System.Linq;

namespace TouhouMix.Storage {
	public sealed class ResourceStorage {
		const string ALBUMS_FILE_PATH = "Albums";
		const string SONGS_FILE_PATH = "Songs";
		readonly string[] MIDIS_FILE_PATH_LIST = {"MidisContrib", "MidisDmbn", "MidisDmbnNew"};

		public AlbumsProto albumsProto;
		public SongsProto songsProto;
		public MidisProto[] albumsProtoList;

		public Dictionary<int, AlbumProto> albumProtoDict = new Dictionary<int, AlbumProto>();
		public Dictionary<int, Dictionary<int, SongProto>> songProtoDict = new Dictionary<int, Dictionary<int, SongProto>>();
		public Dictionary<Tuple<int, int>, List<MidiProto>> midiProtoDict = new Dictionary<Tuple<int, int>, List<MidiProto>>();

		public void Load() {
			albumsProto = UnityEngine.JsonUtility.FromJson<AlbumsProto>(LoadText(ALBUMS_FILE_PATH));
			songsProto = UnityEngine.JsonUtility.FromJson<SongsProto>(LoadText(SONGS_FILE_PATH));
			albumsProtoList = new MidisProto[MIDIS_FILE_PATH_LIST.Length];
			for (int i = 0; i < MIDIS_FILE_PATH_LIST.Length; i++) {
				albumsProtoList[i] = UnityEngine.JsonUtility.FromJson<MidisProto>(LoadText(MIDIS_FILE_PATH_LIST[i]));
			}

		}

		static string LoadText(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).text;
		}

		static byte[] LoadBytes(string path) {
			return UnityEngine.Resources.Load<UnityEngine.TextAsset>(path).bytes;
		}
	}
}