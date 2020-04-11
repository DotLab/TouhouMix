using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

namespace TouhouMix.Net {
	public class WebCache : MonoBehaviour {
		public static WebCache instance;

		public string rootPath;

		void Awake() {
			if (instance == null) {
				instance = this;
				DontDestroyOnLoad(gameObject);
			} else {
				Destroy(gameObject);
			}

			rootPath = Path.Combine(Application.persistentDataPath, "WebCache");

			StartCoroutine(CheckJobsHandler());
		}

		const int MAX_CONCURRENT_DOWNLOAD_COUNT = 5;
		const int MAX_CONCURRENT_CALLBACK_COUNT = 20;

		public readonly Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
		public readonly Dictionary<string, AudioClip> audioClipDict = new Dictionary<string, AudioClip>();
		public readonly Dictionary<string, string> textDict = new Dictionary<string, string>();

		readonly LinkedList<ILoadJob> pendingLoadJobList = new LinkedList<ILoadJob>();
		readonly Dictionary<string, ILoadJob> pendingLoadJobDict = new Dictionary<string, ILoadJob>();

		readonly LinkedList<IWwwLoadJob> activeWwwLoadJobList = new LinkedList<IWwwLoadJob>();
		readonly LinkedList<IWwwLoadJob> wwwLoadJobList = new LinkedList<IWwwLoadJob>();

		public interface ILoadJob {
			string GetKey();
			float GetProgress();
			bool IsFinished();
			void Abort();
			bool IsAborted();

			void ExecuteCallback();
		}

		public interface ILoadJob<T> : ILoadJob {
			T GetData();

			IEnumerable<Action<ILoadJob<T>>> GetCallbacks();
			void AddCallback(Action<ILoadJob<T>> callback);
		}

		public interface IWwwLoadJob : ILoadJob {
			void StartDownload();
		}

		public abstract class LoadJob<T> : ILoadJob<T> {
			readonly List<Action<ILoadJob<T>>> callbackList = new List<Action<ILoadJob<T>>>();
			readonly string key;
			bool isCallbackCalled;
			bool isAborted;

			public LoadJob(string key, Action<ILoadJob<T>> callback) {
				callbackList.Add(callback);
				this.key = key;

				instance.pendingLoadJobList.AddLast(this);
				instance.pendingLoadJobDict.Add(key, this);
			}

			public IEnumerable<Action<ILoadJob<T>>> GetCallbacks() {
				return callbackList;
			}

			public void AddCallback(Action<ILoadJob<T>> callback) {
				if (callback != null) callbackList.Add(callback);
			}

			public void ExecuteCallback() {
				if (isCallbackCalled) return;
				foreach (var callback in callbackList) {
					callback(this);
				}
				isCallbackCalled = true;
			}

			public string GetKey() {
				return key;
			}

			public abstract float GetProgress();
			public abstract bool IsFinished();
			public abstract T GetData();

			public void Abort() {
				isAborted = true;
			}

			public bool IsAborted() {
				return isAborted;
			}
		}

		public class SimpleLoadJob<T> : LoadJob<T> {
			readonly T data;
			readonly int delay;
			int counter;

			public SimpleLoadJob(string key, T data, Action<ILoadJob<T>> callback, int delay = 0) : base(key, callback) {
				this.data = data;
				this.delay = delay;
			}

			public override float GetProgress() {
				return (float)counter / delay;
			}

			public override bool IsFinished() {
				counter += 1;
				return counter > delay;
			}

			public override T GetData() {
				return data;
			}
		}

		public abstract class SavedLoadJob<T> : LoadJob<T> {
			protected T data;
			protected byte[] bytes;

			protected readonly string key;
			protected readonly string filePath;

			public SavedLoadJob(string key, string filePath, Action<ILoadJob<T>> callback) : base(key, callback) {
				this.key = key;
				this.filePath = filePath;
			}

			public override T GetData() {
				if (!IsFinished()) throw new Exception("Job not finished!");

				if (data != null) return data;
				new FileInfo(filePath).Directory.Create();
				//Debug.Log(filePath + " " + path);
				LoadDataAndBytes();
				File.WriteAllBytes(instance.GetLocalFilePath(key), bytes);
				return data;
			}

			protected abstract void LoadDataAndBytes();
		}

		public abstract class WwwLoadJob<T> : LoadJob<T>, IWwwLoadJob {
			protected WWW www;
			protected T data;

			protected readonly string path, url, filePath;

			public WwwLoadJob(string path, string url, string filePath, Action<ILoadJob<T>> callback) : base(path, callback) {
				this.path = path;
				this.url = url;
				if (Levels.GameScheduler.instance.appConfig.networkEndpoint == 1) {
					this.url = url
						.Replace("https://storage.thmix.org", "https://asia.storage.thmix.org")
						.Replace("https://storage.googleapis.com/microvolt-bucket-1", "https://asia.storage.thmix.org");
					Debug.Log(url + " to " + this.url);
				}
				this.filePath = filePath;

				instance.wwwLoadJobList.AddFirst(this);
			}

			public void StartDownload() {
				www = new WWW(url);
			}

			public override float GetProgress() {
				return www == null ? 0 : www.progress;
			}

			public override bool IsFinished() {
				return www != null && (!string.IsNullOrEmpty(www.error) || www.isDone);
			}

			public override T GetData() {
				if (!string.IsNullOrEmpty(www.error)) {
					Debug.LogError(url + " " + www.error);
					return default;
				}

				if (!IsFinished()) throw new Exception("Job not finished!");

				if (data != null) return data;
				new FileInfo(filePath).Directory.Create();
				//Debug.Log(filePath + " " + path);
				File.WriteAllBytes(instance.GetLocalFilePath(path), www.bytes);
				return data = LoadData();
			}

			protected abstract T LoadData();
		}

		public sealed class WwwLoadTextureJob : WwwLoadJob<Texture2D> {
			public WwwLoadTextureJob(string path, string url, string filePath, Action<ILoadJob<Texture2D>> callback) : base(path, url, filePath, callback) {
			}

			protected override Texture2D LoadData() {
				var texture = www.texture;
				instance.textureDict.Add(path, texture);
				return texture;
			}
		}

		public sealed class WwwLoadAudioClipJob : WwwLoadJob<AudioClip> {
			public WwwLoadAudioClipJob(string path, string url, string filePath, Action<ILoadJob<AudioClip>> callback) : base(path, url, filePath, callback) {
			}

			protected override AudioClip LoadData() {
				var clip = www.GetAudioClip(false);
				instance.audioClipDict.Add(path, clip);
				return clip;
			}
		}

		public sealed class WwwLoadTextJob : WwwLoadJob<string> {
			public WwwLoadTextJob(string path, string url, string filePath, Action<ILoadJob<string>> callback) : base(path, url, filePath, callback) {
			}

			protected override string LoadData() {
				var text = www.text;
				instance.textDict.Add(path, text);
				return text;
			}
		}

		public sealed class WwwLoadNullJob : WwwLoadJob<object> {
			public WwwLoadNullJob(string path, string url, string filePath, Action<ILoadJob<object>> callback) : base(path, url, filePath, callback) {
			}

			protected override object LoadData() {
				return null;
			}
		}

		public sealed class CutTextureJob : SavedLoadJob<Texture2D> {
			readonly Texture2D texture; 
			readonly Vector2 size; 
			readonly float borderRadius;

			public CutTextureJob(string key, string filePath, Texture2D texture, Vector2 size, float borderRadius, Action<ILoadJob<Texture2D>> callback) : base(key, filePath, callback) {
				this.texture = texture;
				this.size = size;
				this.borderRadius = borderRadius;
			}

			protected override void LoadDataAndBytes() {
				data = RawImageCutter.CutImage(texture, size, borderRadius);
				instance.textureDict.Add(key, data);
				bytes = data.EncodeToPNG();
			}

			public override float GetProgress() {
				return 1;
			}

			public override bool IsFinished() {
				return true;
			}

			public static string BuildKey(string textureKey, Vector2 size, float borderRadius) {
				return string.Format("{0}-cut{1}x{2}-{3}.png", textureKey, (int)size.x, (int)size.y, (int)borderRadius);
			}
		}

		public bool IsJobPending(string path) {
			return pendingLoadJobDict.ContainsKey(path);
		}

		public void ClearCache() {
			foreach (var item in textureDict.Values) Destroy(item);
			foreach (var item in audioClipDict.Values) Destroy(item);

			textureDict.Clear();
			audioClipDict.Clear();
			textDict.Clear();
		}

		public bool CheckFileExists(string path) {
			return File.Exists(GetLocalFilePath(path));
		}

		public bool CheckUrlFileExists(string url) {
			return File.Exists(GetLocalFilePath(GetLocalFileNameFromUrl(url)));
		}

		public ILoadJob<Texture2D> LoadTexture(string url, Action<ILoadJob<Texture2D>> callback = null) {
			return LoadTexture(GetLocalFileNameFromUrl(url), url, callback);
		}

		public ILoadJob<Texture2D> LoadTexture(string key, string url, Action<ILoadJob<Texture2D>> callback = null) {
			if (pendingLoadJobDict.ContainsKey(key)) {  // Duplicated job
				var pendingJob = (ILoadJob<Texture2D>)pendingLoadJobDict[key];
				pendingJob.AddCallback(callback);
				return pendingJob;
			} else if (textureDict.ContainsKey(key)) {  // From memory
				return new SimpleLoadJob<Texture2D>(key, textureDict[key], callback);
			} else if (CheckFileExists(key)) {  // From disk
				byte[] bytes = File.ReadAllBytes(GetLocalFilePath(key));
				var texture = new Texture2D(4, 4);
				texture.LoadImage(bytes);
				textureDict.Add(key, texture);
				return new SimpleLoadJob<Texture2D>(key, texture, callback);
			} else {  // Create new
				return new WwwLoadTextureJob(key, url, GetLocalFilePath(key), callback);
			}
		}

		public ILoadJob<Texture2D> CutTexture(string sourceKey, Texture2D source, Vector2 size, float borderRadius, Action<ILoadJob<Texture2D>> callback = null) {
			string key = CutTextureJob.BuildKey(sourceKey, size, borderRadius);

			if (pendingLoadJobDict.ContainsKey(key)) {  // Duplicated job
				var pendingJob = (ILoadJob<Texture2D>)pendingLoadJobDict[key];
				pendingJob.AddCallback(callback);
				return pendingJob;
			} else if (textureDict.ContainsKey(key)) {  // From memory
				return new SimpleLoadJob<Texture2D>(key, textureDict[key], callback);
			} else if (CheckFileExists(key)) {  // From disk
				byte[] bytes = File.ReadAllBytes(GetLocalFilePath(key));
				var texture = new Texture2D(4, 4);
				texture.LoadImage(bytes);
				textureDict.Add(key, texture);
				return new SimpleLoadJob<Texture2D>(key, texture, callback);
			} else {  // Create new
				return new CutTextureJob(key, GetLocalFilePath(key), source, size, borderRadius, callback);
			}
		}

		public ILoadJob<string> LoadText(string path, string url, Action<ILoadJob<string>> callback = null) {
			if (pendingLoadJobDict.ContainsKey(path)) {  // Duplicated job
				return (ILoadJob<string>)pendingLoadJobDict[path];
			} else if (textDict.ContainsKey(path)) {  // From memory
				return new SimpleLoadJob<string>(path, textDict[path], callback);
			} else if (CheckFileExists(path)) {  // From disk
				string text = File.ReadAllText(GetLocalFilePath(path));
				textDict.Add(path, text);
				return new SimpleLoadJob<string>(path, text, callback);
			} else {  // Create new
				return new WwwLoadTextJob(path, url, GetLocalFilePath(path), callback);
			}
		}

		public ILoadJob<AudioClip> LoadAudioClip(string url, Action<ILoadJob<AudioClip>> callback = null) {
			return LoadAudioClip(GetLocalFileNameFromUrl(url), url, callback);
		}

		public ILoadJob<AudioClip> LoadAudioClip(string path, string url, Action<ILoadJob<AudioClip>> callback = null) {
			if (pendingLoadJobDict.ContainsKey(path)) {  // Duplicated job
				return (ILoadJob<AudioClip>)pendingLoadJobDict[path];
			} else if (audioClipDict.ContainsKey(path)) {  // From memory
				return new SimpleLoadJob<AudioClip>(path, audioClipDict[path], callback);
			} else if (CheckFileExists(path)) {  // From disk
				return new WwwLoadAudioClipJob(path, GetLocalFileUrl(path), GetLocalFilePath(path), callback);
			} else {  // Create new
				return new WwwLoadAudioClipJob(path, url, GetLocalFilePath(path), callback);
			}
		}

		public ILoadJob<object> LoadNull(string url, Action<ILoadJob<object>> callback = null) {
			return LoadNull(GetLocalFileNameFromUrl(url), url, callback);
		}

		public ILoadJob<object> LoadNull(string path, string url, Action<ILoadJob<object>> callback = null) {
			if (pendingLoadJobDict.ContainsKey(path)) {  // Duplicated job
				return (ILoadJob<object>)pendingLoadJobDict[path];
			} else if (CheckFileExists(path)) {  // From disk
				return new SimpleLoadJob<object>(path, null, callback); ;
			} else {  // Create new
				return new WwwLoadNullJob(path, url, GetLocalFilePath(path), callback);
			}
		}

		static string GetLocalFileNameFromUrl(string url) {
			return Path.GetFileName(url);
		}
		
		public string GetLocalFilePath(string path) {
			return Path.Combine(rootPath, path);
		}

		string GetLocalFileUrl(string path) {
			return "file:///" + GetLocalFilePath(path);
		}

		public System.Collections.IEnumerator CheckJobsHandler() {
			while (true) {
				CheckJobs();
				yield return new WaitForSeconds(.05f);
			}
		}

		//readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();

		public void CheckJobs() {
			if (pendingLoadJobList.Count > 0) {
				int finishCount = 0;
				int abortCount = 0;

				var node = pendingLoadJobList.First;
				for (int i = 0; node != null && i < MAX_CONCURRENT_CALLBACK_COUNT;) {
					while (node != null && !node.Value.IsFinished()) node = node.Next;
					if (node != null) {
						if (!node.Value.IsAborted()) {
							i += 1;
							finishCount += 1;
							node.Value.ExecuteCallback();
#if UNITY_EDITOR
							node.Value.ExecuteCallback();
#else
							try {
								node.Value.ExecuteCallback();
							} catch(System.Exception ex) {
								Debug.LogError(ex);
							}
#endif
							//Debug.LogFormat("Job Finish ({0})", pendingLoadJobList.Count);
						} else {
							abortCount += 1;
							//Debug.LogFormat("Job aborted ({0})", pendingLoadJobList.Count);
						}
						var next = node.Next;
						pendingLoadJobDict.Remove(node.Value.GetKey());
						pendingLoadJobList.Remove(node);
						node = next;
					}
				}
				if (finishCount != 0 || abortCount != 0) {
					Debug.LogFormat("Job finish {0} abort {1}", finishCount, abortCount);
				}
			}

			if (activeWwwLoadJobList.Count > 0) {
				int finishCount = 0;
				var node = activeWwwLoadJobList.First;
				while (node != null) {
					var next = node.Next;
					if (node.Value.IsFinished()) {
						activeWwwLoadJobList.Remove(node);
						finishCount += 1;
						//Debug.LogFormat("DL Finish ({0} / {1})", activeWwwLoadJobList.Count, activeWwwLoadJobList.Count + wwwLoadJobList.Count);
					}
					node = next;
				}
				if (finishCount != 0) {
					Debug.LogFormat("DL finish {0}", finishCount);
				}
			}

			if (wwwLoadJobList.Count > 0) {
				int startCount = 0;
				while (wwwLoadJobList.Count > 0 && activeWwwLoadJobList.Count < MAX_CONCURRENT_DOWNLOAD_COUNT) {
					var job = wwwLoadJobList.First.Value;
					wwwLoadJobList.RemoveFirst();
					job.StartDownload();
					activeWwwLoadJobList.AddLast(job);
					startCount += 1;
					//Debug.LogFormat("DL Start ({0} / {1})", activeWwwLoadJobList.Count, activeWwwLoadJobList.Count + wwwLoadJobList.Count);
				}
				if (startCount != 0) {
					Debug.LogFormat("DL start {0}", startCount);
				}
			}
		}
	}
}
