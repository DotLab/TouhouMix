using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System;
using System.Threading;
using System.Text;
using UnityEngine;

public class NetworkTester : MonoBehaviour {
	async void Start() {
		var socket = new ClientWebSocket();
//		socket.Options.AddSubProtocol("Tls");
		var uri = new Uri("ws://localhost:1337");
		await socket.ConnectAsync(uri, CancellationToken.None);

		var bytesToSend = new ArraySegment<byte>(
			Encoding.UTF8.GetBytes("hello fury from unity")
		);
		await socket.SendAsync(
			bytesToSend, 
			WebSocketMessageType.Text, 
			true, 
			CancellationToken.None
		);
	}
}
