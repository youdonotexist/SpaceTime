using UnityEngine;

namespace Commonwealth.Script.Utility.Extensions
{
	public class ColorLerp : MonoBehaviour {

		public float lerpDuration = 1f;
		public Color[] colors = new Color[] { Color.red, Color.green, Color.blue };

		private float lerpTime = 0f;

		private Color startColor;
		private Color endColor;

		private int colorPtr = 0;

		private Material material;

		void Awake () {
			material = GetComponent<MeshRenderer> ().material;
	
			StartColorLerp (colorPtr);
		}

		void Update () {
			float t = (Time.time - lerpTime) / lerpDuration;
			material.color = Color.Lerp (startColor, endColor, t);
			if (t >= 1f) {
				colorPtr++;
				StartColorLerp (colorPtr);
			}
		}

		private void StartColorLerp (int ptr) {
			startColor = colors [ptr % colors.Length];
			endColor = colors [(ptr + 1) % colors.Length];
			lerpTime = Time.time;
		}
	}
}
