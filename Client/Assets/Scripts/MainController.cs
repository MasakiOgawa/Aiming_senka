﻿using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using RPC = WebSocketSample.RPC;

public class MainController : MonoBehaviour
{
	WebSocket webSocket;    // WebSocketコネクション

	[SerializeField]
	string connectAddress;

	[SerializeField]
	GameObject playerPrefab;
	[SerializeField]
	GameObject otherPlayerPrefab;
	[SerializeField]
	GameObject itemPrefab;

	GameObject playerObj;
	Vector3 previousPlayerObjPosition; // 前フレームでの位置
	int playerId;
	Dictionary<int, GameObject> otherPlayerObjs = new Dictionary<int, GameObject>();

	void Start()
	{
		webSocket = new WebSocket(connectAddress);

		// コネクションを確立したときのハンドラ
		webSocket.OnOpen += (sender, eventArgs) =>
		{
			Debug.Log("WebSocket Opened");
		};

		// エラーが発生したときのハンドラ
		webSocket.OnError += (sender, eventArgs) =>
		{
			Debug.Log("WebSocket Error Message: " + eventArgs.Message);
		};

		// コネクションを閉じたときのハンドラ
		webSocket.OnClose += (sender, eventArgs) =>
		{
			Debug.Log("WebSocket Closed");
		};

		// メッセージを受信したときのハンドラ
		webSocket.OnMessage += (sender, eventArgs) =>
		{
			Debug.Log("WebSocket Message: " + eventArgs.Data);

			var header = JsonUtility.FromJson<RPC.Header>(eventArgs.Data);
			switch (header.Method)
			{
				case "ping":
					{
						var pong = JsonUtility.FromJson<RPC.Ping>(eventArgs.Data);
						Debug.Log(pong.Payload.Message);
						break;
					}
				case "login_response":
					{
						var loginResponse = JsonUtility.FromJson<RPC.LoginResponse>(eventArgs.Data);
						MainThreadExecutor.Enqueue(() => OnLoginResponse(loginResponse.Payload));
						break;
					}
				case "sync":
					{
						var syncMessage = JsonUtility.FromJson<RPC.Sync>(eventArgs.Data);
						MainThreadExecutor.Enqueue(() => OnSync(syncMessage.Payload));
						break;
					}
				case "spawn":
					{
						var spawnResponse = JsonUtility.FromJson<RPC.Spawn>(eventArgs.Data);
						MainThreadExecutor.Enqueue(() => OnSpawn(spawnResponse.Payload));
						break;
					}
			}
		};

		webSocket.Connect();

		Login();
	}

	void Update()
	{
		UpdatePosition();
	}

	void OnDestroy()
	{
		webSocket.Close();
	}

	void Login()
	{
		var jsonMessage = JsonUtility.ToJson(new RPC.Login(new RPC.LoginPayload("PlayerName")));
		Debug.Log(jsonMessage);

		webSocket.Send(jsonMessage);
		Debug.Log(">> Login");
	}

	void OnLoginResponse(RPC.LoginResponsePayload response)
	{
		Debug.Log("<< LoginResponse");
		playerId = response.Id;
		Debug.Log(playerId);
		playerObj = Instantiate(playerPrefab, new Vector3(0.0f, 0.5f, 0.0f), Quaternion.identity) as GameObject;
	}

	void UpdatePosition()
	{
		if (playerObj == null) return;

		var currentPlayerPosition = playerObj.transform.position;
		if (currentPlayerPosition == previousPlayerObjPosition) return;

		Debug.Log(">> Update");

		previousPlayerObjPosition = currentPlayerPosition;

		var rpcPosition = new RPC.Position(currentPlayerPosition.x, currentPlayerPosition.y, currentPlayerPosition.z);
		var jsonMessage = JsonUtility.ToJson(new RPC.PlayerUpdate(new RPC.PlayerUpdatePayload(playerId, rpcPosition)));
		Debug.Log(jsonMessage);
		webSocket.Send(jsonMessage);
	}

	void OnSync(RPC.SyncPayload payload)
	{
		Debug.Log("<< Sync");
		foreach (var otherPlayer in payload.Players)
		{
			// 自分だったら捨てる
			if (otherPlayer.Id == playerId) continue;

			var otherPlayerPoision = new Vector3(otherPlayer.Position.X, otherPlayer.Position.Y, otherPlayer.Position.Z);

			if (otherPlayerObjs.ContainsKey(otherPlayer.Id))
			{
				// 既にGameObjectがいたら位置更新
				otherPlayerObjs[otherPlayer.Id].transform.position = otherPlayerPoision;
			}
			else
			{
				// GameObjectがいなかったら新規作成
				var otherPlayerObj = Instantiate(otherPlayerPrefab, otherPlayerPoision, Quaternion.identity) as GameObject;
				otherPlayerObj.name = "Other" + otherPlayer.Id;
				otherPlayerObjs.Add(otherPlayer.Id, otherPlayerObj);
				Debug.Log("Instantiated a new player: " + otherPlayer.Id);
			}
		}
	}

	void OnSpawn(RPC.SpawnPayload payload)
	{
		Debug.Log("<< OnSpawn");
		var position = new Vector3(payload.Position.X, payload.Position.Y, payload.Position.Z);
		Instantiate(itemPrefab, position, Quaternion.identity);
	}
}