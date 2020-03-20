﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Jsonf;
using System.Threading.Tasks;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using JsonList = System.Collections.Generic.List<object>;
using RpcCallback = System.Action<string, object>;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TouhouMix.Net {
	public sealed class NetManager {
		const int RETRY_DELAY = 500;
		const int RETRY_DELAY_MAX = 1000 * 10;

		static string GenerateId() {
			return System.IO.Path.GetRandomFileName();
		}

		const string KEY_DATA = "data";
		const string KEY_ERROR = "error";

		public bool available = false;
		public bool connecting = false;

		public int rtt;

		JsonContext json = new JsonContext();

		WebSocket websocket;

		Dictionary<string, RpcCallback> callbackDict = new Dictionary<string, RpcCallback>();

		Stopwatch retryWatch = new Stopwatch();
		int retryDelay = RETRY_DELAY;

		Task pingTask;

		public void Init() {
//#if UNITY_EDITOR
//			websocket = new WebSocket("ws://192.168.1.102:6008");
//#else
			websocket = new WebSocket("ws://asia.thmix.org/websocket");
//#endif

			websocket.OnOpen += OnSocketOpen;
			websocket.OnClose += OnSocketClose;
			websocket.OnMessage += OnSocketMessage;
			websocket.OnError += OnSocketError;

			Debug.Log("Connecting...");
			new Task(() => {
				try {
					connecting = true;
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
								Debug.Log("pong " + rtt);
							});
						}
					}
				} catch(System.Exception e) {
					Debug.LogError(e);
				}
			});
			pingTask.Start();
		}

		~NetManager() {
			websocket.Close();
			pingTask.Dispose();
		}

		public void Dispose() {
			websocket.Close();
		}

		void OnSocketOpen(object sender, System.EventArgs e) {
			Debug.Log("connected");
			available = true;
			retryDelay = RETRY_DELAY;
			EndReconnect();
		}

		void OnSocketClose(object sender, CloseEventArgs e) {
			Debug.LogWarning("closed " + e.Code + " " + e.Reason);
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
			}

			if (connecting) {
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
			Debug.Log("retry delay " + retryDelay);
			new Task(() => { 
				websocket.Connect();
				if (!string.IsNullOrEmpty(Levels.GameScheduler.instance.username)) {
					ClAppUserLogin(
						Levels.GameScheduler.instance.username, Levels.GameScheduler.instance.password, null);
				}
			}).Start();
		}

		void EndReconnect() {
			if (!connecting) {
				return;
			}

			connecting = false;
			retryWatch.Start();
			retryWatch.Reset();
			retryWatch.Start();
		}

		void HandleRpcResponse(string id, JsonObj args) {
			string callbackId = (string)args["id"];
			//Debug.Log("HandleRpcResponse " + id + " " + callbackId);

			if (callbackDict.ContainsKey(callbackId)) {
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
			}, callback);
		}

		public void ClAppMidiRecordList(string hash, int page, RpcCallback callback) {
			Rpc("ClAppMidiRecordList", new JsonObj() {
				["hash"] = hash, ["page"] = page,
			}, callback);
		}
	}
}