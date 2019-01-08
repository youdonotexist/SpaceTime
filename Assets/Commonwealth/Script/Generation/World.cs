using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Commonwealth.Script.Generation
{
	public class World : MonoBehaviour
	{
		[SerializeField] private Transform _shipTransform;
		[SerializeField] private SpriteRenderer _mapBounds;
		[SerializeField] private GameObject _thingPrefab;
		[SerializeField] private Transform _shipMarker;
		
		private int _seed = 13;
		//private int _gridSize = Int32.MaxValue;
		//private Vector2 _startPos = new Vector2(0.0f, 0.0f);
		//private Vector2 _minExtents; 
		//private Vector2 _maxExtents;
		private Vector3 _shipOrigin;
		
		Vector2[] _things = new Vector2[50]; 

		private void Awake()
		{
			Random.InitState(_seed);
			
			//_minExtents = new Vector2(_gridSize * -0.5f, _gridSize * -0.5f);
			//_maxExtents = new Vector2(_gridSize * 0.5f, _gridSize * 0.5f);

			for (int i = 0; i < _things.Length; i++)
			{
				_things[i] = new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
				
			}
		}

		private void Start()
		{
			Bounds bounds = _mapBounds.bounds;
			
			for (int i = 0; i < _things.Length; i++)
			{
				GameObject go = Instantiate(_thingPrefab);
				float x = Mathf.Lerp(bounds.min.x, bounds.max.x, _things[i].x);
				float y = Mathf.Lerp(bounds.min.y, bounds.max.y, _things[i].y);

				go.transform.position = new Vector3(x, y, _mapBounds.transform.position.z);
				go.transform.parent = _mapBounds.transform;
			}
		}

		public void AttachTransform(Transform initialTransform)
		{
			_shipTransform = initialTransform;
			_shipOrigin = _shipTransform.position;
		}

		void Update()
		{
			if (_shipTransform == null) return;
 			
			Vector3 change = _shipOrigin - _shipTransform.position;
			Vector3 newPos = _shipMarker.position - (change / 1000.0f);
			newPos.z = _shipMarker.position.z;

			_shipMarker.transform.position = newPos;
			_shipOrigin = _shipTransform.position;
		}


		//Ship starts at origin
		// First, in a radius (eventually chunk-radius) 
		// Place energy sources
		// Place unknown markers (derelict ship, asteroid, etc)
	}
}
