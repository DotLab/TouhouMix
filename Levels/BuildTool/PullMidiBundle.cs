using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TouhouMix.Net;
using TouhouMix.Storage.Protos.Api;

namespace TouhouMix.Levels.BuildTool {
	public sealed class PullMidiBundle : MonoBehaviour {
#if UNITY_EDITOR
		public MidiBundleProto bundle;

		[Button]
		public void GetMidiBundle() {
			GameScheduler.instance.netManager.ClAppMidiBundleBuild((err, doc) => {
				if (!string.IsNullOrEmpty(err)) {
					Debug.LogError(err);
					return;
				}
				bundle = new Jsonf.JsonContext().Parse<MidiBundleProto>((Dictionary<string, object>)doc);
				foreach (var midi in bundle.midis) {
					GameScheduler.instance.localDb.WriteDoc(LocalDb.COLLECTION_MIDIS, midi._id, midi);
					GameScheduler.instance.netManager.ClAppMidiDownload(midi.hash, (error, data) => {
						WebCache.instance.LoadNull(midi.hash, (string)data, job => {
							Debug.Log(job.GetKey());
							job.GetData();
						});
					});
				}
				foreach (var album in bundle.albums) {
					GameScheduler.instance.localDb.WriteDoc(LocalDb.COLLECTION_ALBUMS, album._id, album);
					if (album.coverUrl != null) {
						WebCache.instance.LoadNull(album.coverUrl, job => {
							Debug.Log(job.GetKey());
							job.GetData();
						});
						WebCache.instance.LoadNull(album.coverBlurUrl, job => {
							Debug.Log(job.GetKey());
							job.GetData();
						});
					}
				}
				foreach (var song in bundle.songs) {
					GameScheduler.instance.localDb.WriteDoc(LocalDb.COLLECTION_SONGS, song._id, song);
				}
				foreach (var person in bundle.persons) {
					GameScheduler.instance.localDb.WriteDoc(LocalDb.COLLECTION_PERSONS, person._id, person);
				}

				foreach (var translation in bundle.translations) {
					GameScheduler.instance.translationSevice.Set(translation.src, translation.lang, translation.ns, translation.text);
				}
				GameScheduler.instance.translationSevice.Flush();
			});
		}

		[Button]
		public void TestMidiBundle() {
			byte[] bytes = Resources.Load<TextAsset>("MidiBundle").bytes;
			using (var stream = new System.IO.MemoryStream(bytes)) {
				new ICSharpCode.SharpZipLib.Zip.FastZip().ExtractZip(stream, Application.persistentDataPath, ICSharpCode.SharpZipLib.Zip.FastZip.Overwrite.Always, null, null, null, true, true);
				Debug.Log("Done");
			}
		}
	#endif
	}
}
