using UE.Script.Utility.ServiceLocatorSample.ServiceLocator;
using UnityEngine;

namespace UE.Script
{
    public class Main : MonoBehaviour
    {
        void Awake()
        {
            ServiceLocator.Initiailze();
        }
    }
}