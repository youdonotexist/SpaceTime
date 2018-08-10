using ProceduralToolkit.Examples;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class InputManager : MonoBehaviour
    {
        public static Vector3 MouseWorld;
        public static Camera ActiveCamera;
        public static Side CurrentSide;

        public static Subject<Side> mSideSubject = new Subject<Side>();

        void Update()
        {
            if (ActiveCamera != null)
            {
                Vector3 origin = ActiveCamera.transform.position;
                Vector3 direction = MouseWorld - ActiveCamera.transform.position;
                Debug.DrawRay(origin, direction);
            }
        }

        public static void SetSide(Side side)
        {
            CurrentSide = side;
            mSideSubject.OnNext(side);
        }

        public static IObservable<Side> GetSideStream()
        {
            return mSideSubject;
        }
    }
}