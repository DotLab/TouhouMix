using UnityEngine.UI;
using UnityEngine;
using TouhouMix.Storage;
using TouhouMix.Storage.Protos.Resource;
using Systemf;
using Uif;
using Uif.Tasks;
using Midif.V3;
using System.Security.Cryptography;

namespace TouhouMix.Levels.SongSelect {
	public class MidiDetailPageScheduler : PageScheduler<SongSelectLevelScheduler> {
		public Text titleText;
		public Text infoText;

		public AlbumProto album;
		public SongProto song;
		public MidiProto midi;
		public AuthorProto author;

		public MidiFile midiFile;
		public NoteSequenceCollection sequenceCollection;

		ResourceStorage res_;

		public override void Init(SongSelectLevelScheduler level) {
			base.Init(level);

			res_ = game_.resourceStorage;
		}

		public void Init() {
			try {
				album = res_.albumProtoDict[level_.selectedAlbum];
				song = res_.songProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong)];
				midi = res_.midiProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong, level_.selectedMidi)];
			} catch(System.Exception e) {
				Debug.LogError(e);

				album = res_.albumProtoDict[level_.selectedAlbum = 6];
				song = res_.songProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong = 1)];
				midi = res_.midiProtoDict[Tuple.Create(level_.selectedAlbum, level_.selectedSong, level_.selectedMidi = "aka_easy")];
			}
			author = res_.authorProtoDict[midi.author];

			byte[] bytes = Resources.Load<TextAsset>("dmbn_old/" + midi.name).bytes;
			string sha256Hash = MiscHelper.GetBase64EncodedSha256Hash(bytes);
			midiFile = new MidiFile(bytes);
			sequenceCollection = new NoteSequenceCollection(midiFile);

			titleText.text = midi.name;
			infoText.text = string.Format(
				"{0} • {1}\n" +
				"by {2}\n" +
				"{4:N0} Sequences • {5:N0} Notes • {3}", album.name, song.name, author.name, sha256Hash, sequenceCollection.sequences.Count, sequenceCollection.noteCount);
		}

		public override AnimationSequence Show(AnimationSequence seq) {
			return seq.Call(Init).Append(base.Show);
		}

		public override void Back() {
			level_.selectedMidi = null;
			level_.Pop();
		}

		public void OnMusicButtonClicked() {
			level_.Push(level_.synthConfigPage);
		}
	}
}