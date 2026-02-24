using UnityEngine;

namespace Liminal.Tools.Common
{
    public class SmoothFollowEye : MonoBehaviour
    {
        [Header("Dependencies")]
        public Transform Camera;

        [Header("Follow Settings")]
        public float Speed = 5f;
        public float Delay = .5f;
        public float DistanceThreshold = 0.6f;
        public float SnapDistance = 0.1f;
        public Vector3 Offset;

        [Header("Placement Settings")]
        public float Distance = 5f;

        [Header("Options")]
        public bool Interpolate = true;
        public bool FaceCamera = true;

        private bool _isFollowing = false;
        private float _followTimer = 0f;

        private Vector3 _followingPosition;

        public bool FlipFacing;

        private void OnEnable()
        {
            if (Camera == null && UnityEngine.Camera.main != null)
                Camera = UnityEngine.Camera.main.transform;

            // Immediately place the object in front of the user
            PlaceInFront();
        }

        // TODO support locking tilt & height
        private void LateUpdate()
        {
            if (Camera == null)
                return;

            var targetPosition = Camera.position + Camera.forward * Distance;
            var distance = Vector3.Distance(transform.position, targetPosition);
            
            // Follow states
            if (distance >= DistanceThreshold)
            {
                _followTimer += Time.deltaTime;

                if (_followTimer >= Delay)
                {
                    _isFollowing = true;
                    _followingPosition = targetPosition;
                }
            }

            if (_isFollowing)
            {
                var followingDistance = Vector3.Distance(transform.position, _followingPosition);

                if (followingDistance <= SnapDistance)
                {
                    _isFollowing = false;
                    _followTimer = 0;
                    //PlaceInFront();
                }

                var adjustedSpeed = Speed * (1 + (1 / (followingDistance + 0.1f))); // Increase speed as distance decreases
                var newPosition = Interpolate ? Vector3.Slerp(transform.position, _followingPosition, adjustedSpeed * Time.deltaTime) : _followingPosition;
                newPosition.y = Mathf.Lerp(transform.position.y, _followingPosition.y, adjustedSpeed * Time.deltaTime);
                transform.position = newPosition;

            }

            if (FaceCamera)
                AlignRotation();
        }

        public void AlignRotation()
        {
            // Use the camera's rotation and apply any local adjustments
            Quaternion cameraRotation = Camera.rotation;

            // Apply local rotation adjustment if needed (e.g., flip or offsets)
            Quaternion localRotationOffset = Quaternion.Euler(0, FlipFacing ? 180 : 0, 0);

            // Combine the camera's rotation with the local offset
            Quaternion targetRotation = cameraRotation * localRotationOffset;

            // Smoothly interpolate the current rotation to the target rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * Speed);
        }

        private void PlaceInFront()
        {
            transform.position = GetTargetPosition();
            AlignRotation();
        }

        public Vector3 GetTargetPosition() => Camera.position + Offset + Camera.forward * Distance;
    }
}
