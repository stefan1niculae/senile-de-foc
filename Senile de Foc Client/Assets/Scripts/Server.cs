﻿using UnityEngine;
using System.Collections;

//TODO switch from this dummy server 'interface'
public class Server : MonoBehaviour 
{
	static string u1, p1;

	// Login handling
	public static bool UsernameExists (string username)
	{
		return username == "s";
	}

	public static bool PasswordMatches (string username, string password)
	{
		return (username == "s" && password == "a") || (username == u1 && password == p1);
	}

	public static void CreateUser (string username, string password)
	{
		print ("created " + username + ", " + password);
		u1 = username; p1 = password;
	}

	public static void Login (string username, string password)
	{
		// I'm not sure this server interaction is needed
		print (username + " logged");
	}
}