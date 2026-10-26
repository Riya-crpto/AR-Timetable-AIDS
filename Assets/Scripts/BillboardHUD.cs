using UnityEngine;

namespace CollegeAR.Timetable
{
    /// <summary>
    /// Utility component that rotates a world-space UI or header element to always face the AR camera.
    /// Supports yaw-only lock for clean upright readability.
    /// </summary>
    public class BillboardHUD : MonoBehaviour
    {
        [Tooltip("Lock pitch and roll so text only rotates horizontally.")]
        [SerializeField] private bool lockPitchAndRoll = true;

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Vector3 directionToCamera = _mainCamera.transform.position - transform.position;

            if (lockPitchAndRoll)
            {
                directionToCamera.y = 0; // Keep horizontal only
            }

            if (directionToCamera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}
