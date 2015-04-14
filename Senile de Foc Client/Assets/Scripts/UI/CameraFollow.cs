﻿using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour 
{
	public Transform player;
	
	public float speed;
	// Distance the player can move before the camera catches up
	public float threshold;

	Vector3 target;

	float
		maxTop,
		maxBot,
		maxLeft,
		maxRight;

	void Awake ()
	{
		var camExtents = (Camera.main.ViewportToWorldPoint (Vector3.one) - Camera.main.ViewportToWorldPoint (Vector3.zero)) / 2f;
		Utils.ComputeBoundaries (camExtents, ref maxTop, ref maxBot, ref maxLeft, ref maxRight);
	}
	
	void Update ()
	{
		target = transform.position;

		target.x = player.position.x;
		target.y = player.position.y;

		// Lerping doesn't work when moving diagonally (don't know why)
//		if (Mathf.Abs (transform.position.x - player.position.x) > threshold)
//			target.x = Mathf.Lerp (transform.position.x, player.position.x, speed);
//		if (Mathf.Abs (transform.position.y - player.position.y) > threshold)
//			target.y = Mathf.Lerp (transform.position.y, player.position.y, speed);

		// The camera stops at the world edges
		target.x = Mathf.Clamp (target.x, maxLeft, maxRight);
		target.y = Mathf.Clamp (target.y, maxBot, maxTop);

		transform.position = target;
	}


}
