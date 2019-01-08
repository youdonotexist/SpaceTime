using System.Linq;
using Commonwealth.Script.Ship;
using Commonwealth.Script.Utility.Transformations;
using ProceduralToolkit.Examples;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class InputManager : MonoBehaviour
    {
        public Canvas GuiCanvas;
        
        public static Vector3 MouseWorldFlat;
        public static Vector3 MouseWorldCube;
        public static Camera ActiveCameraFlat;
        public static Camera ActiveCameraCube;
        public static Camera ActiveCameraUi;
        public static Canvas MainCanvas;
        public static Side CurrentSide;

        public static TransformContext[] TransformContexts;

        public static Subject<Side> mSideSubject = new Subject<Side>();

        void Awake()
        {
            MainCanvas = GuiCanvas;
            ActiveCameraUi = Camera.allCameras.First(cam => cam.name.Equals("UICamera"));
            ActiveCameraCube = Camera.main;
            //ActiveCameraFlat is calculated by the Side class
        }

        public enum InputState
        {
            Cube,
            Flat,
            Ui
        }

        void Update()
        {
            if (ActiveCameraFlat != null)
            {
                Vector3 origin = ActiveCameraFlat.transform.position;
                Vector3 direction = MouseWorldFlat - ActiveCameraFlat.transform.position;
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

        public static TransformContext StateForCamera(InputState state)
        {
            if (state == InputState.Cube)
            {
                return new TransformContext(ActiveCameraCube, MouseWorldCube);
            }
            else if (state == InputState.Flat)
            {
                return new TransformContext(ActiveCameraFlat, MouseWorldFlat);
            }
            else if (state == InputState.Ui)
            {
                Vector2 mousePos = Input.mousePosition;
                Vector3 world = ActiveCameraUi.ScreenToWorldPoint(mousePos);
                world.z = MainCanvas.planeDistance;
                    
                return new TransformContext(ActiveCameraUi, world);
            }
            
            return new TransformContext(ActiveCameraCube, MouseWorldCube);
        }
    }
}