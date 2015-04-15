﻿using UnityEngine;
using System.Collections;

public class Patrol : MonoBehaviour 
{
	public static readonly float EPSILON = 1.5f; // we leave such a large margin because of inertia

	public Transform[] path;
	public int currentPoint; // this can be set from the editor in order to change the first point
	public float rotationPressure;
	public float movingPressure;

	TankMovement movement;

	void Awake ()
	{
		movement = GetComponentInChildren <TankMovement> ();

		// Getting the first point
		point = path [currentPoint].position;
	}

	float targetRot;
	Vector3 point;
	void Update ()
	{
		// I don't know why the hell this doesn't work when the patrolling thank is derailed
		// it flickers between rotating and moving

		// Getting the correct rotation
		// we do this every update and not only when we change the point because the tank can be derailed
		targetRot = Utils.LookAt2D (transform, point).eulerAngles.z;

		// First we align towards the point
		if (Mathf.Abs (movement.transform.rotation.eulerAngles.z - targetRot) > EPSILON) { // Debug.Log ("rotating (" + movement.transform.rotation.eulerAngles.z + " -> " + targetRot + ")");
			movement.Move (rotationPressure * Time.deltaTime, 0, targetRot: targetRot); }

		// Then we move towards it
		else if (Vector2.Distance (transform.localPosition, point) > EPSILON) { // Debug.Log ("moving");
			movement.Move (0f, movingPressure * Time.deltaTime); }

		// Then go to the next point
		else {
			currentPoint = (currentPoint + 1) % path.Length;
			point = path [currentPoint].position;
		}
	}

}