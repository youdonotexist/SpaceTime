using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.Ship.EngineMod
{
    public class EngineModUi : MonoBehaviour
    {
        [SerializeField] private Button _simulateButton;

        public IObservable<Unit> GetSimulateStream()
        {
           return _simulateButton.OnClickAsObservable();
        }
    }
}