using UnityEngine.UI;

namespace Commonwealth.Script.UI
{
	public class BetterSlider : Slider {

		public void SetValue(float val, bool notify)
		{
			Set(val, notify);
		}
	}
}
