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

		const int MaxConcurrentJobCount = 4;

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

			void ExecuteCallback();
		}

		public interface ILoadJob<T> : ILoadJob {
			T GetData();

			void AddCallback(Action<ILoadJob<T>> callback);
		}

		public interface IWwwLoadJob : ILoadJob {
			void StartDownload();
		}

		public abstract class LoadJob<T> : ILoadJob<T> {
			readonly List<Action<ILoadJob<T>>> callbackList = new List<Action<ILoadJob<T>>>();
			readonly string key;
			bool isCallbackCalled;

			public LoadJob(string key, Action<ILoadJob<T>> callback) {
				callbackList.Add(callback);
				this.key = key;

				instance.pendingLoadJobList.AddLast(this);
				instance.pendingLoadJobDict.Add(key, this);
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
		}

		public class SimpleLoadJob<T> : LoadJob<T> {
			readonly T data;
			readonly int delay;
			int counter;

			public SimpleLoadJob(string key, T data, Action<ILoadJob<T>> callback, int delay = 2) : base(key, callback) {
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

		public class WwwLoadTextureJob : WwwLoadJob<Texture2D> {
			public WwwLoadTextureJob(string path, string url, string filePath, Action<ILoadJob<Texture2D>> callback) : base(path, url, filePath, callback) {
			}

			protected override Texture2D LoadData() {
				var texture = www.texture;
				instance.textureDict.Add(path, texture);
				return texture;
			}
		}

		public class WwwLoadAudioClipJob : WwwLoadJob<AudioClip> {
			public WwwLoadAudioClipJob(string path, string url, string filePath, Action<ILoadJob<AudioClip>> callback) : base(path, url, filePath, callback) {
			}

			protected override AudioClip LoadData() {
				var clip = www.GetAudioClip(false);
				instance.audioClipDict.Add(path, clip);
				return clip;
			}
		}

		public class WwwLoadTextJob : WwwLoadJob<string> {
			public WwwLoadTextJob(string path, string url, string filePath, Action<ILoadJob<string>> callback) : base(path, url, filePath, callback) {
			}

			protected override string LoadData() {
				var text = www.text;
				instance.textDict.Add(path, text);
				return text;
			}
		}

		public class WwwLoadNullJob : WwwLoadJob<object> {
			public WwwLoadNullJob(string path, string url, string filePath, Action<ILoadJob<object>> callback) : base(path, url, filePath, callback) {
			}

			protected override object LoadData() {
				return null;
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

		public ILoadJob<Texture2D> LoadTexture(string path, string url, Action<ILoadJob<Texture2D>> callback = null) {
			if (pendingLoadJobDict.ContainsKey(path)) {  // Duplicated job
				var pendingJob = (ILoadJob<Texture2D>)pendingLoadJobDict[path];
				pendingJob.AddCallback(callback);
				return pendingJob;
			} else if (textureDict.ContainsKey(path)) {  // From memory
				return new SimpleLoadJob<Texture2D>(path, textureDict[path], callback);
			} else if (CheckFileExists(path)) {  // From disk
				byte[] bytes = File.ReadAllBytes(GetLocalFilePath(path));
				var texture = new Texture2D(4, 4);
				texture.LoadImage(bytes);
				textureDict.Add(path, texture);
				return new SimpleLoadJob<Texture2D>(path, texture, callback);
			} else {  // Create new
				return new WwwLoadTextureJob(path, url, GetLocalFilePath(path), callback);
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
				yield return new WaitForSeconds(.1f);
			}
		}

		public void CheckJobs() {
			if (pendingLoadJobList.Count > 0) {
				var node = pendingLoadJobList.First;
				while (node != null && !node.Value.IsFinished()) node = node.Next;
				if (node != null) {
					node.Value.ExecuteCallback();
					pendingLoadJobDict.Remove(node.Value.GetKey());
					pendingLoadJobList.Remove(node);
					Debug.LogFormat("Job Finish ({0})", pendingLoadJobList.Count);
				}
			}

			if (activeWwwLoadJobList.Count > 0) {
				var node = activeWwwLoadJobList.First;
				while (node != null) {
					var next = node.Next;
					if (node.Value.IsFinished()) {
						activeWwwLoadJobList.Remove(node);
						Debug.LogFormat("DL Finish ({0} / {1})", activeWwwLoadJobList.Count, activeWwwLoadJobList.Count + wwwLoadJobList.Count);
					}
					node = next;
				}
			}

			while (wwwLoadJobList.Count > 0 && activeWwwLoadJobList.Count < MaxConcurrentJobCount) {
				var job = wwwLoadJobList.First.Value;
				wwwLoadJobList.RemoveFirst();
				job.StartDownload();
				activeWwwLoadJobList.AddLast(job);
				Debug.LogFormat("DL Start ({0} / {1})", activeWwwLoadJobList.Count, activeWwwLoadJobList.Count + wwwLoadJobList.Count);
			}
		}
	}
}
