using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public interface __IRotator
    {
        void OnRotate(float amt);
    }
    
    public class BetterCamera2DFollow : MonoBehaviour
    {
        public Transform Target;
        public float Damping = 1;
        public float LookAheadFactor = 3;
        public float LookAheadReturnSpeed = 0.5f;
        public float LookAheadMoveThreshold = 0.1f;

        private float _mOffsetZ;
        private float _mRotOffset;
        private Vector3 _mLastTargetPosition;
        private Vector3 _mCurrentVelocity;
        private Vector3 _mLookAheadPos;

        public void ShiftOffsetZBy(float offsetZ)
        {
            float newOffset = _mOffsetZ + offsetZ; 
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
            
            
            /*float rotX = transform.eulerAngles.x + (_mRotOffset * _mOffsetZ * 0.02f);
 
            Quaternion rotation = Quaternion.Euler(0, rotX, 0);
            
            // only update lookahead pos if accelerating or changed direction
            float xMoveDelta = (Target.position - _mLastTargetPosition).x;

            bool updateLookAheadTarget = Mathf.Abs(xMoveDelta) > LookAheadMoveThreshold;

            if (updateLookAheadTarget)
            {
                _mLookAheadPos = LookAheadFactor * Vector3.right * Mathf.Sign(xMoveDelta);
            }
            else
            {
                _mLookAheadPos =
                    Vector3.MoveTowards(_mLookAheadPos, Vector3.zero, Time.deltaTime * LookAheadReturnSpeed);
            }

            Vector3 aheadTargetPos = Target.position + _mLookAheadPos + Vector3.forward * _mOffsetZ;
            Vector3 newPos = Vector3.SmoothDamp(transform.position, aheadTargetPos, ref _mCurrentVelocity, Damping);

            transform.position = newPos;
            transform.rotation = rotation;

            _mLastTargetPosition = Target.position;*/
        //}
        
    }
}