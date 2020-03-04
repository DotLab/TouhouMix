using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIOClient;

namespace TouhouMix.Net {
	public sealed class NetManager {
		SocketIO socket;

		public void Init() {
//#if UNITY_EDITOR
//			socket = new SocketIO("http://localhost:6003");
//#else
			socket = new SocketIO("https://thmix.org");
//#endif
			
			socket.OnConnected += OnSocketConnected;
			socket.OnClosed += OnSocketClosed;

			socket.ConnectAsync();
		}

		void OnSocketConnected() {
			Debug.Log("connected");


		}

		void OnSocketClosed(ServerCloseReason reason) {
			Debug.Log("closed " + reason);
		}
	}
}
