﻿using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour 
{
	public Transform player;
	public bool isFollowingPlayer = true;

	public float followSpeed;
	public float followThreshold; // distance the player can move before the camera catches up

	public float bumpSpeed;
	public float bumpAcceleration = .01f; // in percentages
	public float bumpMargin = .05f; // in percentages

	Transform camTransf;

	float
		maxTop,
		maxBot,
		maxLeft,
		maxRight;

	void Awake ()
	{
		camTransf = Camera.main.transform;
	}

	Vector3 target;
	void Update ()
	{
		// Y switches following / not following the player
		if (Input.GetKeyDown (KeyCode.Y))
			isFollowingPlayer = !isFollowingPlayer;
		
		// Holding space is not a toogle
		HandleHolding ();

		target = camTransf.position;

		if (isFollowingPlayer)
			FollowPlayer ();
		else 
			RegisterBumps ();

		// These can change due to zoom
		var camExtents = (Camera.main.ViewportToWorldPoint (Vector3.one) - 
						  Camera.main.ViewportToWorldPoint (Vector3.zero)) / 2f;
		Utils.ComputeBoundaries (camExtents, ref maxTop, ref maxBot, ref maxLeft, ref maxRight);

		// The camera stops at the world edges
		target.x = Mathf.Clamp (target.x, maxLeft, maxRight);
		target.y = Mathf.Clamp (target.y, maxBot, maxTop);

		camTransf.position = target;
	}
	
	bool valueBeforeHolding;
	Vector3 posBeforeHolding;
	void HandleHolding ()
	{
		// On press, remember the toggle value and camera position
		if (Input.GetKeyDown (KeyCode.Space)) {
			valueBeforeHolding = isFollowingPlayer;
			posBeforeHolding = camTransf.position;
		}

		if (Input.GetKey (KeyCode.Space))
			isFollowingPlayer = true;

		if (Input.GetKeyUp (KeyCode.Space)) {
			isFollowingPlayer = valueBeforeHolding;
			// If the camera was not set to follow before holding, return it to where it was
			if (!isFollowingPlayer)
				camTransf.position = posBeforeHolding;
		}
	}

	void FollowPlayer ()
	{
		// Only if the player has been spawned
		if (player != null) {
			// Snapping to the player
			target.x = player.position.x;
			target.y = player.position.y;
		}

		// Lerping doesn't work when moving diagonally (don't know why)
		//		if (Mathf.Abs (transform.position.x - player.position.x) > followThreshold)
		//			target.x = Mathf.Lerp (transform.position.x, player.position.x, followSpeed);
		//		if (Mathf.Abs (transform.position.y - player.position.y) > followThreshold)
		//			target.y = Mathf.Lerp (transform.position.y, player.position.y, followSpeed);
	}

	bool movedLastCall;
	float velocity; // speed AND acceleration
	void RegisterBumps ()
	{
		var mousePos = Camera.main.ScreenToViewportPoint (Input.mousePosition);
		var beforeMove = target;

		// If the mouse is held on an edge
		if (movedLastCall)
			// Increase the speed
			velocity *= 1f + bumpAcceleration;

		// When no longer holding an edge
		else
			// Reset the speed
			velocity = bumpSpeed;

		if 		(mousePos.x >= 1f - bumpMargin)	target.x += velocity * Time.deltaTime;
		else if (mousePos.x <= bumpMargin)		target.x -= velocity * Time.deltaTime;

		if 		(mousePos.y >= 1f - bumpMargin)	target.y += velocity * Time.deltaTime;
		else if (mousePos.y <= bumpMargin)		target.y -= velocity * Time.deltaTime;

		movedLastCall = (beforeMove != target);
	}

	bool wasFollowingBeforeDeath;
	public void HandleDeath ()
	{
		wasFollowingBeforeDeath = isFollowingPlayer;
		isFollowingPlayer = false;
	}

	public void HandleRespawn ()
	{
		// Snap once to the player
		var pos = player.position;
		pos.z = transform.position.z;
		transform.position = pos;

		// And resume the old following value
		isFollowingPlayer = wasFollowingBeforeDeath;
	}
}
