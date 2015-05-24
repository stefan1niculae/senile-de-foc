﻿using UnityEngine;
using System.Collections;

public class NetworkManager : MonoBehaviour 
{
	HostData[] hostData;

	void Start ()
	{
		StartConnecting ();
	}

	void StartConnecting ()
	{
	  	StartCoroutine (GetHostList ());
	}

	IEnumerator GetHostList ()
	{
		NetworkStatus.Show ("Looking for servers", NetworkStatus.MessageType.working);
		MasterServer.RequestHostList (NetworkConstants.GAME_TYPE);

		do {
			hostData = MasterServer.PollHostList ();
			yield return new WaitForEndOfFrame ();
		} while (hostData.Length == 0);

		if (hostData.Length == 0)
			NetworkStatus.Show ("No servers found", NetworkStatus.MessageType.failure);
		else 
			ConnectToFound ();
	}

	void OnConnectedToServer ()
	{
		NetworkStatus.Show ("Connected to server", NetworkStatus.MessageType.success);
	}

	void OnDisconnectedFromServer (NetworkDisconnection info)
	{
		NetworkStatus.Show ("Disconnected from the server " + info, NetworkStatus.MessageType.failure);
	}

	void ConnectToFound ()
	{
		if (hostData.Length > 1)
			Debug.Log ("More than one servers found");
		
		NetworkStatus.Show ("Connecting to sserver", NetworkStatus.MessageType.working);
		Network.Connect (hostData [0]);
	}

}
