using UnityEngine;

namespace Commonwealth.Script.Utility
{
	public class BoundsUtils : Object {

		public static Rect BoundsToScreenRect(Bounds bounds, Camera camera)
		{
			// Get mesh origin and farthest extent (this works best with simple convex meshes)
			Vector3 origin = camera.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.max.y, 0f));
			Vector3 extent = camera.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.min.y, 0f));
     
			// Create rect in screen space and return - does not account for camera perspective
			return new Rect(origin.x, Screen.height - origin.y, extent.x - origin.x, origin.y - extent.y);
		}
	}
}
