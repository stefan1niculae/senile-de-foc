﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class RequestsManager : MonoBehaviour
{
	Database database;
	NetworkView netView;
	Dictionary <NetworkPlayer, PlayerInfo> connectedPlayers;

	int killsThisMatch;
	
	public Text logText;
	string log
	{
		get { return logText.text; }
		set { logText.text = value + "\n"; }
	}
	public Text playersText;
	string players
	{
		get { return playersText.text; }
		set { playersText.text = value; }
	}


	enum State { splash, game };
	State state;





	
	void Awake ()
	{
		database = GameObject.FindObjectOfType <Database> ();
		netView = GetComponent <NetworkView> ();
		connectedPlayers = new Dictionary<NetworkPlayer, PlayerInfo> ();

		state = State.splash;
	}

	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.D))
			Network.CloseConnection (Network.connections [0], true);
	}


	// Connected Players
	void OnPlayerConnected (NetworkPlayer player)
	{
		print ("players count = " + connectedPlayers.Count);
		if (connectedPlayers.Count >= NetworkConstants.MAX_PLAYERS || state == State.game) {
			log += player.ipAddress + " tried to connect but state = " + state + " connected players = " + connectedPlayers.Count;
			Network.CloseConnection (player, true);
		}

		log += "Player " + player + " connected from " + player.ipAddress + ":" + player.port;
		connectedPlayers.Add (player, new PlayerInfo (player));

		SendMatchLimits (player);

		UpdatePlayers ();
	}
	
	void OnPlayerDisconnected (NetworkPlayer p)
	{
		log += p.ipAddress + " disconnected";
		if (connectedPlayers.ContainsKey (p))
			connectedPlayers.Remove (p);

		Network.RemoveRPCs(p);
		Network.DestroyPlayerObjects(p);

		UpdatePlayers ();

		// Go back to login state when everyone left
		if (connectedPlayers.Count == 0)
			state = State.splash;
	}

	void UpdatePlayers ()
	{
		string str = "";

		List <PlayerInfo> playerInfos = connectedPlayers.Values.ToList ();
		for (int i = 0; i < playerInfos.Count; i++)
			playerInfos [i].orderNumber = i;
		foreach (PlayerInfo p in playerInfos)
			str += p + "\n";

		players = str;
		netView.RPC ("ReceivePlayerList", RPCMode.Others, NetworkUtils.ObjectToByteArray (playerInfos));
	}
	[RPC]
	void ReceivePlayerList (byte[] bytes)
	{ }





	// Login/Register
	[RPC]
	void RequestUsernameExistance (string username, NetworkMessageInfo info)
	{
		bool value = database.Exists (username);
		log += "Existance of " + username + " = " + value;
		netView.RPC ("ReceiveUsernameExistance", info.sender, value);
	}
	[RPC]
	void ReceiveUsernameExistance (bool value)
	{ }

	[RPC]
	void SendCreateUser (string name, string firstLetter, int passHash) // CHANGED
	{
		log += "Creating " + name + ", " + firstLetter + "... " + passHash;
		database.Create (name, firstLetter[0], passHash);
	}

	[RPC]
	void RequestPasswordMatch (string username, int passHash, NetworkMessageInfo info) // CHANGED
	{
		bool value = database.Matches (username, passHash);
		log += "Password match for " + username + " = " + value;
		netView.RPC ("ReceivePasswordMatch", info.sender, value);
	}
	[RPC]
	void ReceivePasswordMatch (bool value)
	{ }

	[RPC]
	public void SendLogin (string username, NetworkMessageInfo info)
	{
		log += username + " logged in";
		connectedPlayers [info.sender].name = username;
		UpdatePlayers ();
	}





	// Tank Select
	[RPC]
	void SendTankType (byte[] bytes, NetworkMessageInfo info)
	{
		TankType type = NetworkUtils.ByteArrayToObject (bytes) as TankType;
		connectedPlayers [info.sender].tankType = type;
		UpdatePlayers ();

		log += connectedPlayers [info.sender].name + " chose " + type.slotNr;
	}

	[RPC]
	void SendRates (byte[] bytes, NetworkMessageInfo info)
	{ 
		Rates rates = NetworkUtils.ByteArrayToObject (bytes) as Rates;
		connectedPlayers [info.sender].rates = rates;
		UpdatePlayers ();

		log += connectedPlayers [info.sender].name + " picked rates";
	}





	// Lobby
	[RPC]
	public void SendReady (NetworkMessageInfo info)
	{
		connectedPlayers [info.sender].ready = true;
		UpdatePlayers ();

		bool allReady = true;
		foreach (PlayerInfo player in connectedPlayers.Values)
			if (!player.ready) {
				allReady = false;
				break;
			}

		if (allReady) {
			netView.RPC ("ReceiveGameStart", RPCMode.Others);
			log += "Everyone ready => game start";
		}
	}




	
	// Logout
	[RPC]
	public void SendLogout (NetworkMessageInfo info)
	{
		PlayerInfo player = connectedPlayers [info.sender];
		log += player.name + " logged out";

		connectedPlayers [info.sender].Reset ();
		UpdatePlayers ();
	}





	// Splash to Game
	[RPC]
	void ReceiveGameStart ()
	{ }





	// Ingame
	[RPC]
	void RequestInfo (NetworkMessageInfo messageInfo)
	{
		PlayerInfo info = connectedPlayers [messageInfo.sender];

		log += "Sending to " + messageInfo.sender.ipAddress + " the info of " + info.name;
		netView.RPC ("ReceiveInfo", messageInfo.sender, NetworkUtils.ObjectToByteArray (info));

		connectedPlayers [messageInfo.sender].loadedGame = true;
		UpdatePlayers ();

		bool allLoaded = true;
		foreach (var p in connectedPlayers.Values)
			if (!p.loadedGame) {
				allLoaded = false;
				break;
			}
		if (allLoaded) {
			log += "Everyone loaded the game scene => starting the match";
			netView.RPC ("ReceiveMatchStart", RPCMode.Others, database.lastTimeLimit, database.lastKillsLimit);

			state = State.game;
			killsThisMatch = 0;

			StartCoroutine (EndGameAfter (database.lastTimeLimit * 60));
		}

	}
	[RPC]
	void ReceiveInfo (byte[] bytes)
	{ }

	[RPC]
	void ReceiveMatchStart (int timeLim, int killLim)
	{ }

	float matchEndTime;
	IEnumerator EndGameAfter (float seconds)
	{
		matchEndTime = Time.time + seconds;
		yield return new WaitForSeconds (seconds);

		// We do this check because the match may have ended prematurely
		if (Mathf.Abs (Time.time - matchEndTime) < 1)
			SendMatchOver ();
	}

	[RPC]
	public void ReceiveDamageAnnouncement (float damage, int source, int destination)
	{
		log += source + " applied " + damage + " to " + destination;
	}


	[RPC]
	void ReceiveStatsUpdate (int orderNumber, byte[] statsBytes)
	{
		string username = "";
		killsThisMatch = 0;
		var stats = NetworkUtils.ByteArrayToObject (statsBytes) as Stats;

		foreach (var p in connectedPlayers.Values) {
			if (p.orderNumber == orderNumber) {
				username = p.name;
				p.stats = stats;
				log += username + " new stats " + stats;
			}
			killsThisMatch += p.stats.kills;
		}
		
		database.UpdateHighscore (username, stats);

		if (killsThisMatch >= database.lastKillsLimit)
			SendMatchOver ();
	}


	void SendMatchLimits (NetworkPlayer player)
	{
		netView.RPC ("ReceiveTimeLimit" , player, database.lastTimeLimit);
		netView.RPC ("ReceiveKillsLimit", player, database.lastKillsLimit);
	}
	[RPC]
	void ReceiveTimeLimit (int amount)
	{
		database.lastTimeLimit = amount;
		log += "rec time limit = " + amount;
	}
	[RPC]
	void ReceiveKillsLimit (int amount)
	{
		database.lastKillsLimit = amount;
		log += "rec kills limit = " + amount;
	}


	void SendMatchOver ()
	{
		// To avoid duplicate match overs (time limit and kill limit happen at the same time)
		if (state == State.game) {
			netView.RPC ("ReceiveMatchOver", RPCMode.Others);

			foreach (var p in connectedPlayers.Keys) {
				Network.RemoveRPCs (p);
				Network.DestroyPlayerObjects (p);
			}
			connectedPlayers.Clear ();
			UpdatePlayers ();
		
			state = State.splash;
		}
	}
	[RPC]
	public void ReceiveMatchOver ()
	{ }
}
