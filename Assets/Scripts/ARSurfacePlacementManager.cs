using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CollegeAR.Timetable
{
    /// <summary>
    /// Surface-Based AR Placement Manager:
    /// - Uses ARRaycastManager and ARPlaneManager to detect real-world surfaces (walls, floors, tables).
    /// - Places an upright 3D futuristic timetable board upon touching any detected surface.
    /// - Aligns flush against vertical walls or stands upright on horizontal floors/tables.
    /// - Prevents duplicate boards by smoothly repositioning the existing board on subsequent taps.
    /// - Provides a reset/remove option and surface detection feedback.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    [RequireComponent(typeof(ARPlaneManager))]
    public class ARSurfacePlacementManager : MonoBehaviour
    {
        [Header("Prefab & Display Settings")]
        [Tooltip("The 3D Holographic Timetable Board prefab to place on surfaces.")]
        [SerializeField] private GameObject timetableBoardPrefab;

        [Header("Plane Detection Settings")]
        [Tooltip("Enable both horizontal (floors/tables) and vertical (walls) surface detection.")]
        [SerializeField] private PlaneDetectionMode detectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothSpeed = 16f;
        [SerializeField] private float rotationSmoothSpeed = 14f;

        [Header("Events & UI Hooks")]
        public event Action OnBoardPlaced;
        public event Action OnBoardRepositioned;
        public event Action OnBoardReset;
        public event Action OnNoSurfaceDetected;

        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private Camera _arCamera;

        private GameObject _spawnedBoardInstance;
        private TimetableBoardController _boardController;
        private Transform _boardTransform;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isPlaced = false;

        private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        public bool IsPlaced => _isPlaced;

        private void Awake()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _planeManager = GetComponent<ARPlaneManager>();
            _arCamera = Camera.main;

            if (_planeManager != null)
            {
                _planeManager.requestedDetectionMode = detectionMode;
            }
        }

        private void Start()
        {
            // Pre-instantiate single instance pool to guarantee zero runtime duplicate boards
            if (timetableBoardPrefab != null && _spawnedBoardInstance == null)
            {
                _spawnedBoardInstance = Instantiate(timetableBoardPrefab, Vector3.zero, Quaternion.identity);
                _spawnedBoardInstance.name = "AR_3D_Timetable_Board_SingleInstance";
                _boardTransform = _spawnedBoardInstance.transform;
                _boardController = _spawnedBoardInstance.GetComponent<TimetableBoardController>();
                _spawnedBoardInstance.SetActive(false);
            }
        }

        private void Update()
        {
            HandleScreenTouch();
            SmoothBoardTransform();
        }

        /// <summary>
        /// Detects user screen tap and performs raycasting against detected physical planes.
        /// Compatible with both Unity 6 New Input System and Legacy Input.
        /// </summary>
        private void HandleScreenTouch()
        {
            if (!TryGetScreenTouch(out Vector2 touchPosition)) return;

            // Check if user tapped a UI element (ignore UI clicks)
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Raycast against real-world detected surfaces (both horizontal & vertical planes)
            if (_raycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated))
            {
                ARRaycastHit hit = s_Hits[0];
                Pose hitPose = hit.pose;

                // Inspect the plane orientation to align correctly
                ARPlane hitPlane = _planeManager.GetPlane(hit.trackableId);
                Quaternion placementRotation = CalculatePlacementRotation(hitPose, hitPlane);

                if (!_isPlaced)
                {
                    // First placement
                    PlaceBoard(hitPose.position, placementRotation);
                }
                else
                {
                    // Reposition existing board without duplicating
                    RepositionBoard(hitPose.position, placementRotation);
                }
            }
            else
            {
                // Tapped on empty air / un-detected surface
                OnNoSurfaceDetected?.Invoke();
            }
        }

        private bool TryGetScreenTouch(out Vector2 touchPosition)
        {
            touchPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    touchPosition = touch.position.ReadValue();
                    return true;
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                touchPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            try
            {
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        touchPosition = touch.position;
                        return true;
                    }
                }
                else if (Input.GetMouseButtonDown(0))
                {
                    touchPosition = Input.mousePosition;
                    return true;
                }
            }
            catch (Exception)
            {
                // Fallback for custom configurations
            }
#endif

            return false;
        }

        /// <summary>
        /// Computes exact 3D orientation for walls vs floors:
        /// - Wall (Vertical Plane): Mounts upright flush against the wall facing outward.
        /// - Floor / Table (Horizontal Plane): Stands upright on the surface facing the user's camera.
        /// </summary>
        private Quaternion CalculatePlacementRotation(Pose hitPose, ARPlane plane)
        {
            if (plane != null && plane.alignment == PlaneAlignment.Vertical)
            {
                // Vertical Wall: Align directly with the plane's normal pointing away from the wall
                Vector3 wallNormal = plane.normal;
                return Quaternion.LookRotation(wallNormal, Vector3.up);
            }
            else
            {
                // Horizontal Surface (Floor, Desk, Table):
                // Stand upright with forward vector pointing toward the camera's horizontal view direction
                Vector3 cameraForward = _arCamera != null ? _arCamera.transform.forward : Vector3.forward;
                Vector3 projectedForward = Vector3.ProjectOnPlane(-cameraForward, Vector3.up);

                if (projectedForward.sqrMagnitude < 0.001f)
                {
                    projectedForward = Vector3.forward;
                }

                return Quaternion.LookRotation(projectedForward.normalized, Vector3.up);
            }
        }

        private void PlaceBoard(Vector3 position, Quaternion rotation)
        {
            if (_spawnedBoardInstance == null) return;

            _targetPosition = position;
            _targetRotation = rotation;

            _boardTransform.position = position;
            _boardTransform.rotation = rotation;

            _spawnedBoardInstance.SetActive(true);
            _isPlaced = true;

            if (_boardController != null)
            {
                _boardController.OnMarkerDetected(); // Triggers boot animation & audio
            }

            OnBoardPlaced?.Invoke();
        }

        private void RepositionBoard(Vector3 position, Quaternion rotation)
        {
            _targetPosition = position;
            _targetRotation = rotation;

            if (_boardController != null)
            {
                _boardController.OnMarkerDetected(); // Re-trigger sound & subtle pulse
            }

            OnBoardRepositioned?.Invoke();
        }

        private void SmoothBoardTransform()
        {
            if (!_isPlaced || _boardTransform == null) return;

            _boardTransform.position = Vector3.Lerp(
                _boardTransform.position,
                _targetPosition,
                Time.deltaTime * positionSmoothSpeed
            );

            _boardTransform.rotation = Quaternion.Slerp(
                _boardTransform.rotation,
                _targetRotation,
                Time.deltaTime * rotationSmoothSpeed
            );
        }

        /// <summary>
        /// Resets/removes the placed timetable board so the user can re-scan and place again.
        /// </summary>
        public void ResetPlacement()
        {
            _isPlaced = false;

            if (_boardController != null)
            {
                _boardController.OnMarkerLost();
            }
            else if (_spawnedBoardInstance != null)
            {
                _spawnedBoardInstance.SetActive(false);
            }

            OnBoardReset?.Invoke();
        }
    }
}
