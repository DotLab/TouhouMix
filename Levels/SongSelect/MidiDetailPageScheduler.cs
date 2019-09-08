using UnityEngine.UI;
using TouhouMix.Storage;
using TouhouMix.Storage.Protos.Resource;
using Systemf;
using Uif;
using Uif.Tasks;

namespace TouhouMix.Levels.SongSelect {
	public class MidiDetailPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public Text titleText;
		public Text infoText;

		public AlbumProto album;
		public SongProto song;
		public MidiProto midi;
		public AuthorProto author;

		ResourceStorage res_;

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			res_ = game_.resourceStorage;

//			Init();
		}

		public void Init() {
			try {
				album = res_.albumProtoDict[level_.selectedAlbum];
				song = res_.songProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong)];
				midi = res_.midiProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong, level_.selectedMidi)];
			} catch(System.Exception e) {
				UnityEngine.Debug.LogError(e);

				album = res_.albumProtoDict[level_.selectedAlbum = 6];
				song = res_.songProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong = 1)];
				midi = res_.midiProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong, level_.selectedMidi = "aka_easy")];
			}
			author = res_.authorProtoDict[midi.author];

			titleText.text = song.name;
			infoText.text = string.Format("from {0}\nby {1}\n{2}", album.name, author.name, midi.path);
		}

		public override AnimationSequence Show(AnimationSequence seq) {
			return seq.Call(Init).Append(base.Show);
		}

		public override void Back() {
			level_.selectedMidi = null;
			level_.Pop();
		}
	}
}