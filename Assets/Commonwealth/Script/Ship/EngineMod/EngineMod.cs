﻿using UnityEngine;

namespace Commonwealth.Script.Ship.EngineMod
{
	public class EngineMod : MonoBehaviour
	{

		[SerializeField] public GameObject SlotPrefab;
		
		// Use this for initialization
		void Start ()
		{
			int max = 25;
			int rows = 5;
			//int cols = 5;
			float spacing = 0.1f;
			for (int i = 0; i < max; i++)
			{
				int x = i % rows;
				int y = Mathf.FloorToInt((float) i / rows);

				GameObject go = Instantiate(SlotPrefab);
				Collider slotCollider = go.GetComponent<Collider>();
				Vector3 pos = go.transform.position;
				pos.x = (x * (slotCollider.bounds.size.x + spacing));
				pos.y = (y * (slotCollider.bounds.size.y + spacing));
				go.transform.position = pos;
			}
		}
	}
}
