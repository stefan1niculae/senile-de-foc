using UnityEngine;
using System.Collections;

public class TankMovement : MonoBehaviour 
{
	TankInfo tankInfo;

	public float 
		forwardSpeed,
		backwardSpeed,
		rotationSpeed;

	Rigidbody2D body;
	[HideInInspector] public TankTracksManager tracks;

	const float STATIONARY_PERIOD = 1f;
	float lastMove;

	void Awake ()
	{
		// Setting the reference
		body = GetComponent <Rigidbody2D> ();
		tankInfo = GetComponent <TankInfo> ();
	}
		
	public void Move (float horiz, float vert, bool playerInput = true, float targetRot = float.NaN)
	{
		float magnitude;
		float input = AxisToRotation (horiz, vert, out magnitude);
		tankInfo.sounds.tracksVolume = magnitude;

		if (magnitude > 0) {

			float self = transform.eulerAngles.z;
			if (self != 0)
				self = 360 - self;
			
			float diff = Mathf.Abs (input - self);
			if (diff > 180)
				diff = 360 - diff;
			
			bool forwards = diff < 120;


			if (!forwards) {
				input += 180;
				input %= 360;
			}
			transform.rotation = Quaternion.RotateTowards (
				transform.rotation,
				Quaternion.Euler (new Vector3 (0, 0, 360 - input)),
				rotationSpeed
			);

			float speed = forwards ? forwardSpeed : backwardSpeed;
			speed *= magnitude;
			Vector2 dir = Utils.ForwardDirection (transform);
			if (!forwards)
				dir *= -1;
			body.AddForce (dir * speed * Time.deltaTime);
		
			// Only spawn tracks if the tank is moving (and only for the player tank)
			tracks.Show (transform.position, transform.rotation);


			if (Time.time > lastMove + STATIONARY_PERIOD)
				tankInfo.sounds.engine.Play ();
			lastMove = Time.time;
		}
	}

	float AxisToRotation (float x, float y, out float magnitude)
	{
		Vector2 projected = new Vector2 (x * Mathf.Sqrt (1 - y * y / 2),
										 y * Mathf.Sqrt (1 - x * x / 2));
		magnitude = projected.magnitude;

		// For help, checkout the Movement Illustrator
		float rot = Mathf.Atan2 (projected.x,
			   			  	     projected.y);
		rot *= Mathf.Rad2Deg;

		if (rot < 0)
			rot += 360;

		return rot;
	}

}
