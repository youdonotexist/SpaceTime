using UnityEngine;

namespace Commonwealth.Script.Ship
{
	public class FuelConverter : MonoBehaviour
	{
		[SerializeField] private Animator[] _converterAnimators;

		public void SetConverterUtilization(float utilization)
		{
			foreach (var converter in _converterAnimators)
			{
				converter.speed = utilization;
			}
		}
		
		// Use this for initialization
		void Start () {
		
		}
	
		// Update is called once per frame
		void Update () {
		
		}
	}
}
