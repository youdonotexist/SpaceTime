using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class TimeTracker : MonoBehaviour
    {
        private readonly Subject<long> _timeChangeStream = new Subject<long>();

        private void Awake()
        {
            string lastTimeValue = PlayerPrefs.GetString("LastTime");
            long lastTime = long.Parse(lastTimeValue);
            _timeChangeStream.OnNext(lastTime);
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.SetString("LastTime", System.DateTime.Now.Ticks.ToString());
        }
    }
}