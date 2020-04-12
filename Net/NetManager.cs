using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Jsonf;
using System.Threading.Tasks;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using RpcCallback = System.Action<string, object>;
using SafeRpcCallback = System.Action<object>;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TouhouMix.Net {
	public sealed class NetManager : System.IDisposable {
		const int RETRY_DELAY = 100;
		const int RETRY_DELAY_MAX = 1000 * 10;

		public enum NetStatus {
			ONLINE,
			OFFLINE,
			CONNECTING,
		}

		static string GenerateId() {
			return System.IO.Path.GetRandomFileName();
		}

		const string KEY_DATA = "data";
		const string KEY_ERROR = "error";

		public bool available = false;
		public bool connecting = false;

		public int rtt;

		public readonly JsonContext json = new JsonContext();

		WebSocket websocket;

		bool isDisposed = false;

		Dictionary<string, RpcCallback> callbackDict = new Dictionary<string, RpcCallback>();

		Stopwatch retryWatch = new Stopwatch();
		int retryDelay = RETRY_DELAY;

		Task pingTask;
		//System.Threading.CancellationTokenSource cancellationToken; 

		public NetStatus netStatus = NetStatus.OFFLINE;
		public event System.Action<NetStatus> onNetStatusChangedEvent;
		public event System.Action<string> onNetErrorEvent;

		public void Init(Levels.GameScheduler game) {
#if UNITY_EDITOR
			websocket = new WebSocket("ws://192.168.1.102:6008");
#else
			if (game.appConfig.networkEndpoint == 0) {
				websocket = new WebSocket("wss://thmix.org/websocket");
			} else {
				websocket = new WebSocket("wss://asia.thmix.org/websocket");
			}
#endif

			websocket.OnOpen += OnSocketOpen;
			websocket.OnClose += OnSocketClose;
			websocket.OnMessage += OnSocketMessage;
			websocket.OnError += OnSocketError;

			Debug.Log("Connecting...");
			new Task(() => {
				try {
					connecting = true;
					netStatus = NetStatus.CONNECTING;
					onNetStatusChangedEvent?.Invoke(NetStatus.CONNECTING);
					websocket.Connect();
				} catch(System.Exception ex) {
					Debug.LogError(ex);
					Reconnect();
				}
			}).Start();

			pingTask = new Task(async () => { 
				try {
					var watch = new Stopwatch();
					watch.Start();
					while (true) {
						await Task.Delay(50000);

						watch.Stop();
						watch.Reset();
						watch.Start();

						if (available) {
							ClAppPing((int)watch.ElapsedMilliseconds, (err, data) => { 
								if (err != null) {
									Debug.LogError(err);
									return;
								}

								rtt = (int)(watch.ElapsedMilliseconds) - (int)(double)data;
								netStatus = NetStatus.ONLINE;
								onNetStatusChangedEvent?.Invoke(NetStatus.ONLINE);
								Debug.Log("pong " + rtt);
							});
						}

						if (isDisposed) {
							break;
						}
					}
				} catch(System.Exception e) {
					Debug.LogError(e);
				}
			});
			pingTask.Start();
		}

		public void Dispose() {
			isDisposed = true;
		}

		void OnSocketOpen(object sender, System.EventArgs e) {
			Debug.Log("connected");
			available = true;
			EndReconnect();
		}

		void OnSocketClose(object sender, CloseEventArgs e) {
			if (e != null) {
				Debug.LogWarning("closed " + e.Code + " " + e.Reason);
				onNetErrorEvent?.Invoke("socket closed");
			}
			available = false;
			EndReconnect();
			Reconnect();
		}

		void OnSocketError(object sender, ErrorEventArgs e) {
			HandleError(e.Exception);
			available = false;
			EndReconnect();
			Reconnect();
		}

		void HandleError(System.Exception e) {
			Debug.LogWarning("error " + e);
			onNetErrorEvent?.Invoke(e.Message);
		}

		void OnSocketMessage(object sender, MessageEventArgs e) {
			string data = e.Data;
			
			try {
				var resJson = (JsonObj)json.Parse(data);
				string id = (string)resJson["id"];
				string command = (string)resJson["command"];
				var args = (JsonObj)resJson["args"];

				switch (command) {
					case "SvAppHandleRpcResponse": HandleRpcResponse(id, args); break;
				}
			} catch(System.Exception ex) {
				HandleError(ex);
			}
		}

		async void Reconnect() {
			if (callbackDict.Count > 0) {
				foreach (var key in callbackDict.Keys) {
						callbackDict[key].Invoke("Connection closed", null);
				}
				callbackDict.Clear();
			}

			if (connecting || isDisposed) {
				return;
			}
			connecting = true;

			int time = (int)retryWatch.ElapsedMilliseconds;
			if (time < retryDelay) {
				await Task.Delay(retryDelay - time);
			}

			retryDelay = retryDelay << 1;
			if (retryDelay > RETRY_DELAY_MAX) {
				retryDelay = RETRY_DELAY_MAX;
			}
			Debug.Log("next retry delay " + retryDelay);

			netStatus = NetStatus.CONNECTING;
			onNetStatusChangedEvent?.Invoke(NetStatus.CONNECTING);
			new Task(() => {
				websocket.Connect();
				if (!string.IsNullOrEmpty(Levels.GameScheduler.instance.username)) {
					ClAppUserLogin(
						Levels.GameScheduler.instance.username, Levels.GameScheduler.instance.password, null);
				}
			}).Start();
		}

		void EndReconnect() {
			if (available) {
				netStatus = NetStatus.ONLINE;
				onNetStatusChangedEvent?.Invoke(NetStatus.ONLINE);
			} else {
				netStatus = NetStatus.OFFLINE;
				onNetStatusChangedEvent?.Invoke(NetStatus.OFFLINE);
			}

			connecting = false;
			retryWatch.Stop();
			retryWatch.Reset();
			retryWatch.Start();
		}

		void HandleRpcResponse(string id, JsonObj args) {
			string callbackId = (string)args["id"];
			//Debug.Log("HandleRpcResponse " + id + " " + callbackId);

			if (callbackDict.ContainsKey(callbackId)) {
				if (args.ContainsKey(KEY_ERROR)) {
					onNetErrorEvent?.Invoke((string)args[KEY_ERROR]);
				}
#if UNITY_EDITOR
				callbackDict[callbackId].Invoke(
					args.ContainsKey(KEY_ERROR) ? (string)args[KEY_ERROR] : null,
					args.ContainsKey(KEY_DATA) ? args[KEY_DATA] : null);
#else
				try {
					callbackDict[callbackId].Invoke(
						args.ContainsKey(KEY_ERROR) ? (string)args[KEY_ERROR] : null,
						args.ContainsKey(KEY_DATA) ? args[KEY_DATA] : null);
				} catch(System.Exception ex) {
					Debug.LogError(ex);
				}
#endif
				callbackDict.Remove(callbackId);
			} else {
				Debug.Log("rpcResponse to nothing");
			}
		}

		void Rpc(string command, object args, RpcCallback callback = null) {
			if (!available) {
				Reconnect();
				callback?.Invoke("Not available", null);
				return;
			}

			string id = GenerateId();
			Debug.Log("Rpc " + command + " " + id);
			if (callback != null) {
				callbackDict[id] = callback;
			}
			new Task(() => {
				try {
					websocket.Send(json.Stringify(new JsonObj() {
						["id"] = id,
						["command"] = command,
						["args"] = args,
					}));
				} catch(System.Exception e) {
					Debug.LogError(e);
					Reconnect();
				}
			}).Start();
		}

		void ReturnSuccess(string id, object data) {
			Rpc("svAppHandleRpcResponse", new JsonObj(){
				["id"] = id,
				["data"] = data,
			});
		}

		void ReturnError(string id, string message) {
			Rpc("svAppHandleRpcResponse", new JsonObj() {
				["id"] = id,
				["error"] = message,
			});
		}

		public void ClAppUserLogin(string username, string password, RpcCallback callback) {
			Rpc("ClAppUserLogin", new JsonObj() {
				["name"] = username,
				["password"] = password,
			}, callback);
		}

		public void ClAppMidiListQuery(string query, int page, RpcCallback callback) {
			Rpc("ClAppMidiListQuery", new JsonObj() {
				["query"] = query,
				["page"] = page,
			}, callback);
		}

		public void ClAppMidiDownload(string hash, RpcCallback callback) {
			Rpc("ClAppMidiDownload", new JsonObj() { 
				["hash"] = hash,
			}, callback);
		}

		public void ClAppPing(int time, RpcCallback callback) {
			Rpc("ClAppPing", new JsonObj() {
				["time"] = time,
			}, callback);
		}

		public sealed class Trial {
			public string hash;
			public int score;
			public int combo;
			public float accuracy;

      public int perfectCount; 
			public int greatCount; 
			public int goodCount; 
			public int badCount; 
			public int missCount;
		}

		public void ClAppTrialUpload(Trial trial, RpcCallback callback) {
			Rpc("ClAppTrialUpload", new JsonObj() {
				["hash"] = trial.hash,
				["score"] = trial.score, ["combo"] = trial.combo, ["accuracy"] = trial.accuracy,

				["perfectCount"] = trial.perfectCount, ["greatCount"] = trial.greatCount, 
				["goodCount"] = trial.goodCount, ["badCount"] = trial.badCount, ["missCount"] = trial.missCount,

				["version"] = 3,
			}, callback);
		}

		public void ClAppMidiRecordList(string hash, int page, RpcCallback callback) {
			Rpc("ClAppMidiRecordList", new JsonObj() {
				["hash"] = hash, ["page"] = page,
			}, callback);
		}

		public void ClAppTranslate(string src, string lang, string ns, RpcCallback callback) {
			Rpc("ClAppTranslate", new JsonObj() {
				["src"] = src,
				["lang"] = lang,
				["namespace"] = ns,
			}, callback);
		}

		public void ClAppCheckVersion(RpcCallback callback) {
			Rpc("ClAppCheckVersion", new JsonObj() {
				["version"] = Application.version,
				["platform"] = GetPlatform(),
				["runtime"] = Application.platform.ToString().ToLower(),
			}, callback);
		}

		public void ClAppReportDeviceInfo(RpcCallback callback) {
			var audioConfig = AudioSettings.GetConfiguration();
			Rpc("ClAppReportDeviceInfo", new JsonObj() {
				["platform"] = GetPlatform(),
				["runtime"] = Application.platform.ToString().ToLower(),
				
				["sampleRate"] = audioConfig.sampleRate,
				["bufferSize"] = audioConfig.dspBufferSize,

				["model"] = SystemInfo.deviceModel,
				["name"] = SystemInfo.deviceName,
				["os"] = SystemInfo.operatingSystem,
				["cpu"] = SystemInfo.processorType,
				["gpu"] = SystemInfo.graphicsDeviceType,
			}, callback);
		}

		public void ClAppMidiBundleBuild(RpcCallback callback) {
			var audioConfig = AudioSettings.GetConfiguration();
			Rpc("ClAppMidiBundleBuild", new JsonObj() {
			}, callback);
		}

		public static string GetPlatform() {
#if UNITY_STANDALONE || UNITY_EDITOR
			return "standalone";
#elif UNITY_ANDROID
			return "android";
#elif UNITY_IOS
			return "ios";
#else
			return "unknown";
#endif
		}

		public void ClAppDocAction(string collection, string docId, string action, int value, RpcCallback callback) {
			Rpc("ClAppDocAction", new JsonObj() {
				["col"] = collection,
				["docId"] = docId,
				["action"] = action,
				["value"] = value,
			}, callback);
		}
	}
}
