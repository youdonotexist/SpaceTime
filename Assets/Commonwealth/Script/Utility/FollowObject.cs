using UnityEngine;

namespace Commonwealth.Script.Utility
{
	public class FollowObject : MonoBehaviour
	{
		[SerializeField] private Transform _followedObject;
		
		private Transform _transform;

		private float _initialOffset = 0.0f;
		//just xy, for now

		private void Awake()
		{
			_transform = GetComponent<Transform>();
		}

		private void Start()
		{
			Vector3 thisPos = _transform.position;
			Vector3 thatPos = _followedObject.position;

			_initialOffset = thisPos.z - thatPos.z;
		}

		// Update is called once per frame
		void Update () {
			if (_followedObject == null)
			{
				return;
			}

			Vector3 thisPos = _transform.position;
			Vector3 thatPos = _followedObject.position;
			
			_transform.position = new Vector3(thatPos.x, thatPos.y, thatPos.z + _initialOffset);
		}
	}
}
