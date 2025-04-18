using Commonwealth.Script.Life;
using UnityEngine;

namespace Commonwealth.Script.Utility
{
    /// <summary>
    /// DEPRECATION NOTICE:
    /// This class is deprecated and will be removed in a future update.
    /// Please use the new SpriteFollowCamera class instead, which provides improved
    /// 2D sprite following with better performance and features.
    /// </summary>
    [System.Obsolete("This class is deprecated. Use SpriteFollowCamera instead.")]
    public interface __IRotator
    {
        void OnRotate(float amt);
    }
    
    /// <summary>
    /// DEPRECATION NOTICE:
    /// This class is deprecated and will be removed in a future update.
    /// Please use the new SpriteFollowCamera class instead, which provides improved
    /// 2D sprite following with better performance and features.
    /// </summary>
    [System.Obsolete("This class is deprecated. Use SpriteFollowCamera instead.")]
    public class BetterCamera2DFollow : MonoBehaviour
    {
        public Transform Target;
        public Human Human;
        public Transform SecondaryTarget;
        public float Damping = 1;
        public float LookAheadFactor = 3;
        public float LookAheadReturnSpeed = 0.5f;
        public float LookAheadMoveThreshold = 0.1f;

        public Transform FlatTranform;
        public Transform CubeTransform;

        [SerializeField]
        private float _mOffsetZ;
        private float _mRotOffset;
        private Vector3 _mLastTargetPosition;
        private Vector3 _mCurrentVelocity;
        private Vector3 _mLookAheadPos;

        public void ShiftOffsetZBy(float offsetZ)
        {
            _mOffsetZ += offsetZ;
        }

        public void Rotate(float rotation)
        {
            _mRotOffset = rotation;
        }

        public float GetOffsetZ()
        {
            return _mOffsetZ;
        }

        // Use this for initialization
        private void Start()
        {
            Debug.LogWarning("BetterCamera2DFollow is deprecated. Please use SpriteFollowCamera instead.");
            
            _mLastTargetPosition = Target.position;
            _mOffsetZ = (transform.position - Target.position).z;
            transform.parent = null;
        }

        // Update is called once per frame
        private void LateUpdate()
        {
            if (Target != null) 
            {
                _mLastTargetPosition.x += _mRotOffset * /*speed*/ 1.0f * _mOffsetZ * 0.02f;
 
                Quaternion rotation = Quaternion.Euler(0.0f, _mLastTargetPosition.x, 0);
 
                Vector3 negDistance = new Vector3(0.0f, 0.0f, _mOffsetZ);
                Vector3 position = rotation * negDistance + Target.position;
 
                transform.position = position;
                transform.LookAt(Target);

                _mRotOffset = 0.0f;
            }
        }
 
        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}