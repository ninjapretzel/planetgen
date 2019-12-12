using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SimpleNoClip : MonoBehaviour {

	public float speed = 5;
	public float panSpeed = 3;
	public float boost = 3;
	public Vector2 mouseSens = new Vector2(4, 1.5f);

	public float pitchCap = 89.99f;
	float yaw; float pitch;

	void Update() {
		Vector3 movement = Vector3.zero;
		if (Input.GetKey(KeyCode.W)) { movement.z += 1.0f; }
		if (Input.GetKey(KeyCode.A)) { movement.x -= 1.0f; }
		if (Input.GetKey(KeyCode.S)) { movement.z -= 1.0f; }
		if (Input.GetKey(KeyCode.D)) { movement.x += 1.0f; }
		if (Input.GetKey(KeyCode.Q)) { movement.y -= 1.0f; }
		if (Input.GetKey(KeyCode.E)) { movement.y += 1.0f; }
		if (Input.GetKey(KeyCode.LeftShift)) { movement *= boost; }

		movement *= speed;
		Vector2 mouse = Vector2.zero;
		mouse.x = Input.GetAxisRaw("Mouse X");
		mouse.y = Input.GetAxisRaw("Mouse Y");

		if (Input.GetMouseButton(2)) {
			movement.x = mouse.x;
			movement.y = mouse.y;
			movement.z = 0;
			movement *= panSpeed;
		} else if (Input.GetMouseButton(1)) {
			var rot = transform.rotation.eulerAngles;
			yaw = rot.y;
			pitch = rot.x;
			if (pitch > 180) { pitch -= 360; }
			if (pitch < -180) { pitch += 360; }


			yaw += mouse.x * mouseSens.x;
			pitch -= mouse.y * mouseSens.y;
			pitch = Mathf.Clamp(pitch, -pitchCap, pitchCap);

			transform.rotation = Quaternion.identity;
			transform.Rotate(0, yaw, 0);
			transform.Rotate(pitch, 0, 0);

		}
		transform.position += transform.rotation * movement * Time.deltaTime;
	}

}
