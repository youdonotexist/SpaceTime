using UnityEngine;

namespace Commonwealth.Script.Ship.Monitors
{
    [CreateAssetMenu(fileName = "ResourceMonitor", menuName = "Scriptable Objects/ResourceMonitor")]
    public class ResourceMonitor : ScriptableObject
    {
        public float FoodOut { get; set; } = 10;

        public float FoodIn { get; set; }
    }
}
