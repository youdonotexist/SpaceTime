using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{

	private Transform _transform;
	
	// Use this for initialization
	void Start ()
	{
		_transform = GetComponent<Transform>();
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		Vector3 camPos = GameObject.Find("Main Camera").GetComponent<Transform>().position;
		_transform.position = new Vector3(camPos.x, camPos.y, _transform.position.z);
	}
}
