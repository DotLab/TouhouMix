using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.Nist;
using System.Text;
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
//#if UNITY_EDITOR
//			websocket = new WebSocket("ws://192.168.1.102:6008");
//#else
			if (game.appConfig.networkEndpoint == 0) {
				websocket = new WebSocket("wss://thmix.org/websocket");
			} else {
				websocket = new WebSocket("wss://asia.thmix.org/websocket");
			}
//#endif

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
				} catch (System.Exception ex) {
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
				} catch (System.Exception e) {
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
					case "SvAppSecureHandshake": OnSvAppSecureHandshake(id, args); break;
				}
			} catch (System.Exception ex) {
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
			Debug.Log("HandleRpcResponse " + callbackId);

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

		byte[] derivedKeyBytes;

		void OnSvAppSecureHandshake(string id, JsonObj args) {
			// https://davidtavarez.github.io/2019/implementing-elliptic-curve-diffie-hellman-c-sharp/
			X9ECParameters x9Params = NistNamedCurves.GetByName("P-521");
			ECDomainParameters domainParams = new ECDomainParameters(x9Params.Curve, x9Params.G, x9Params.N, x9Params.H, x9Params.GetSeed());
			ECKeyPairGenerator generator = (ECKeyPairGenerator)GeneratorUtilities.GetKeyPairGenerator("ECDH");
			generator.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));
			AsymmetricCipherKeyPair aliceKeyPair = generator.GenerateKeyPair();
			ECPublicKeyParameters alicePublicKeyParams = (ECPublicKeyParameters)aliceKeyPair.Public;

			string bobKey = args.Get<string>("key");
			byte[] bobKeyBytes = System.Convert.FromBase64String(bobKey);
			var bobPoint = x9Params.Curve.DecodePoint(bobKeyBytes);
			ECPublicKeyParameters bobPublicKeyParams = new ECPublicKeyParameters("ECDH", bobPoint, SecObjectIdentifiers.SecP521r1);

			IBasicAgreement agreement = AgreementUtilities.GetBasicAgreement("ECDH");
			agreement.Init(aliceKeyPair.Private);
			BigInteger sharedSecret = agreement.CalculateAgreement(bobPublicKeyParams);

			IDigest digest = new Sha256Digest();
			byte[] sharedSecretBytes = sharedSecret.ToBytes(66);
			digest.BlockUpdate(sharedSecretBytes, 0, sharedSecretBytes.Length);
			derivedKeyBytes = new byte[digest.GetDigestSize()];
			digest.DoFinal(derivedKeyBytes, 0);

			Debug.Log(System.BitConverter.ToString(sharedSecretBytes));
			Debug.Log(System.Convert.ToBase64String(derivedKeyBytes));

			ReturnSuccess(id, new JsonObj() {
				["key"] = alicePublicKeyParams.Q.GetEncoded(),
			});
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
				} catch (System.Exception e) {
					Debug.LogError(e);
					Reconnect();
				}
			}).Start();
		}

		void SecureRpc(string command, object args, RpcCallback callback = null) {
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
					string message = json.Stringify(new JsonObj() {
						["id"] = id,
						["command"] = command,
						["args"] = args,
					});

					// https://stackoverflow.com/questions/56618150/please-help-me-to-fix-the-aes-ctr-mode-bouncy-castle-code-in-c-sharp
					byte[] ivBytes = new byte[16];
					new SecureRandom().NextBytes(ivBytes);

					// create AES cipher
					IBufferedCipher cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
					cipher.Init(true, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", derivedKeyBytes), ivBytes));

					// encrypted
					byte[] encryptedBytes = cipher.DoFinal(Encoding.UTF8.GetBytes(message));

					websocket.Send(json.Stringify(new JsonObj() {
						["key"] = id,
						["iv"] = System.Convert.ToBase64String(ivBytes),
						["message"] = System.Convert.ToBase64String(encryptedBytes),
						["mac"] = "",
					}));
				} catch (System.Exception e) {
					Debug.LogError(e);
					Reconnect();
				}
			}).Start();

		}

		void ReturnSuccess(string id, object data) {
			Rpc("ClAppHandleRpcResponse", new JsonObj() {
				["id"] = id,
				["data"] = data,
			});
		}

		void ReturnError(string id, string message) {
			Rpc("ClAppHandleRpcResponse", new JsonObj() {
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
			public bool withdrew;

			public string hash;
			public int score;
			public int combo;
			public float accuracy;
			public int duration;  // in ms

			public int perfectCount;
			public int greatCount;
			public int goodCount;
			public int badCount;
			public int missCount;

			public int version = 3;
		}

		public void ClAppTrialUpload(Trial trial, RpcCallback callback) {
			Rpc("ClAppTrialUpload", trial, callback);
		}

		public void ClAppMidiGet(string hash, RpcCallback callback) {
			Rpc("ClAppMidiGet", new JsonObj() {
				["hash"] = hash,
			}, callback);
		}

		public void ClAppMidiRecordList(string hash, int page, RpcCallback callback) {
			Rpc("ClAppMidiRecordList", new JsonObj() {
				["hash"] = hash,
				["page"] = page,
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

		public void ClAppErrorReport(string message, string stackTrace, bool isException, RpcCallback callback) {
			var audioConfig = AudioSettings.GetConfiguration();
			Rpc("ClAppErrorReport", new JsonObj() {
				["version"] = Application.version,

				["message"] = message,
				["stack"] = stackTrace,
				["exception"] = isException,
				//["source"] = error.Source,

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

		public void ClAppMidiAction(string hash, string action, int value, RpcCallback callback) {
			Rpc("ClAppMidiAction", new JsonObj() {
				["hash"] = hash,
				["action"] = action,
				["value"] = value,
			}, callback);
		}

		sealed class KeyCoords {
			public BigInteger x;
			public BigInteger y;
			public int byteCount;

			public static KeyCoords FromBytes(byte[] bytes) {
				// https://superuser.com/questions/900918/get-x-and-y-components-of-ec-public-key-using-openssl
				int count = bytes.Length >> 1;
				byte[] xBytes = new byte[count];
				byte[] yBytes = new byte[count];
				System.Buffer.BlockCopy(bytes, 1, xBytes, 0, count);
				System.Buffer.BlockCopy(bytes, 1 + count, yBytes, 0, count);
				return new KeyCoords {
					x = new BigInteger(xBytes),
					y = new BigInteger(yBytes),
					byteCount = count,
				};
			}

			public byte[] ToBytes() {
				byte[] bytes = new byte[1 + (byteCount << 1)];
				bytes[0] = 4;
				byte[] xBytes = x.ToByteArray();
				byte[] yBytes = y.ToByteArray();
				System.Buffer.BlockCopy(xBytes, 0, bytes, 1 + (byteCount - xBytes.Length), xBytes.Length);
				System.Buffer.BlockCopy(yBytes, 0, bytes, 1 + byteCount + (byteCount - yBytes.Length), yBytes.Length);
				return bytes;
			}
		}
	}

	static class BigIntegerExtensions {
		public static byte[] ToBytes(this BigInteger self, int byteCount) {
			byte[] intBytes = self.ToByteArray();
			if (intBytes.Length == byteCount) {
				return intBytes;
			}
			byte[] bytes = new byte[byteCount];
			System.Buffer.BlockCopy(intBytes, 0, bytes, byteCount - intBytes.Length, intBytes.Length);
			return bytes;
		}
	}
}
