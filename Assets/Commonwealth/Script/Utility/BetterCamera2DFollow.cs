using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class BetterCamera2DFollow : MonoBehaviour
    {
        public Transform Target;
        public float Damping = 1;
        public float LookAheadFactor = 3;
        public float LookAheadReturnSpeed = 0.5f;
        public float LookAheadMoveThreshold = 0.1f;

        private float _mOffsetZ;
        private Vector3 _mLastTargetPosition;
        private Vector3 _mCurrentVelocity;
        private Vector3 _mLookAheadPos;

        public float ShiftOffsetZBy(float offsetZ)
        {
            return (_mOffsetZ += offsetZ);
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
        private void Update()
        {
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

            _mLastTargetPosition = Target.position;
        }
    }
}