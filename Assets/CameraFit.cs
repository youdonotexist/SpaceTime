
using UnityEngine;

public class CameraFit : MonoBehaviour
{

	[SerializeField] private Transform _target;
	[SerializeField] private SpriteRenderer _framer;
	
	// Use this for initialization
	void Awake ()
	{
		


	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void FitFitter()
	{
		Debug.Log("fitting...");
		
		Camera cam = GetComponent<Camera>();
		Debug.Log(cam.pixelWidth + " !!! " + cam.pixelHeight);
		
		SpriteRenderer[] renderers = _target.GetComponentsInChildren<SpriteRenderer>();

		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;
		
		

		foreach (var spriteRenderer in renderers)
		{
			if (spriteRenderer.gameObject.name == "Framer")
			{
				continue;
			}
			
			if (spriteRenderer.bounds.min.x < minX)
			{
				minX = spriteRenderer.bounds.min.x;
			}
			
			if (spriteRenderer.bounds.min.y < minY)
			{
				minY = spriteRenderer.bounds.min.y;
			}
			
			if (spriteRenderer.bounds.max.x > maxX)
			{
				maxX = spriteRenderer.bounds.max.x;
			}
			
			if (spriteRenderer.bounds.max.y > maxY)
			{
				maxY = spriteRenderer.bounds.max.y;
			}
		}
		
		Debug.Log(minX + " " + maxX + " " + minY +  " " + maxY);

		_framer.transform.position = new Vector3((maxX - minX) * 0.5f, _framer.transform.position.y, _framer.transform.position.z);
		_framer.transform.localScale = new Vector3((maxX - minX) * 10, (maxY - minY) * 10 , 1.0f);
	}
}
