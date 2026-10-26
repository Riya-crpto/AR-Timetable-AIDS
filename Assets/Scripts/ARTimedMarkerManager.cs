using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CollegeAR.Timetable
{
    /// <summary>
    /// Touch-To-Reveal AR Manager:
    /// - Tracks reference image markers using ARTrackedImageManager.
    /// - Requires the user to TOUCH the marker on screen to reveal the 3D Timetable.
    /// - Stands the 3D board UPRIGHT relative to the physical marker or surface.
    /// - Prevents duplicate boards and supports tapping to toggle/hide or rescan.
    /// - Supports ARRaycastManager plane raycast fallback for un-marked real-world surfaces.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ARTimedMarkerManager : MonoBehaviour
    {
        [Header("Prefab & Target Settings")]
        [Tooltip("The 3D Holographic Timetable Board prefab.")]
        [SerializeField] private GameObject timetableBoardPrefab;

        [Tooltip("Exact name of reference image in XRReferenceImageLibrary.")]
        [SerializeField] private string targetMarkerName = "AR_Reference_Marker";

        [Header("Touch-To-Reveal Interaction")]
        [Tooltip("Require user screen touch to reveal the board (User finger is trigger).")]
        [SerializeField] private bool touchToRevealEnabled = true;

        [Tooltip("Radius around marker center (in meters) considered a valid touch hit.")]
        [SerializeField] private float markerHitRadiusMeters = 0.15f;

        [Header("Smoothing & Stability")]
        [Range(1f, 30f)]
        [SerializeField] private float positionSmoothSpeed = 14f;
        [Range(1f, 30f)]
        [SerializeField] private float rotationSmoothSpeed = 12f;
        [SerializeField] private float deadbandDistance = 0.002f;

        [Header("Optional Surface Raycasting (Un-marked surfaces)")]
        [SerializeField] private ARRaycastManager raycastManager;

        [Header("Events & UI Hooks")]
        public event Action<string, TrackingState> OnMarkerStateChanged;
        public event Action OnMarkerRecognized; // Recognized by camera, waiting for touch
        public event Action OnBoardRevealed;    // Revealed by user touch
        public event Action OnBoardHidden;

        private ARTrackedImageManager _imageManager;
        private Camera _arCamera;
        private GameObject _spawnedBoardInstance;
        private TimetableBoardController _boardController;
        private Transform _boardTransform;

        private ARTrackedImage _activeTrackedImage;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _isMarkerInView = false;
        private bool _isBoardRevealed = false;

        public bool IsMarkerInView => _isMarkerInView;
        public bool IsBoardRevealed => _isBoardRevealed;
        public string TargetMarkerName => targetMarkerName;

        private void Awake()
        {
            _imageManager = GetComponent<ARTrackedImageManager>();
            _arCamera = Camera.main;
            if (raycastManager == null) raycastManager = GetComponent<ARRaycastManager>();
        }

        private void OnEnable()
        {
            if (_imageManager != null)
                _imageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        private void OnDisable()
        {
            if (_imageManager != null)
                _imageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        private void Start()
        {
            // Pre-instantiate single instance pool to prevent runtime frame drops
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
            HandleTouchInput();
            UpdateBoardTransformSmoothing();
        }

        /// <summary>
        /// Detects user finger touch on screen and raycasts against the tracked marker or surface.
        /// Compatible with both Unity 6 New Input System and Legacy Input.
        /// </summary>
        private void HandleTouchInput()
        {
            if (!TryGetScreenTouch(out Vector2 touchPos) || _arCamera == null) return;

            Ray ray = _arCamera.ScreenPointToRay(touchPos);

            // 1. Check if touch hits the tracked marker
            if (_isMarkerInView && _activeTrackedImage != null)
            {
                Vector3 markerCenter = _activeTrackedImage.transform.position;
                Vector3 markerNormal = _activeTrackedImage.transform.up;

                // Create a mathematical plane at the marker
                Plane markerPlane = new Plane(markerNormal, markerCenter);
                if (markerPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    float distanceToMarkerCenter = Vector3.Distance(hitPoint, markerCenter);

                    // Touch verified on marker location!
                    if (distanceToMarkerCenter <= markerHitRadiusMeters)
                    {
                        ToggleBoardReveal(markerCenter, CalculateUprightRotation(_activeTrackedImage.transform));
                        return;
                    }
                }
            }

            // 2. Check if board is already revealed and user tapped directly on the board to hide it
            if (_isBoardRevealed && _boardTransform != null)
            {
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform.IsChildOf(_boardTransform))
                    {
                        // Tapped existing board -> toggle off
                        HideBoard();
                        return;
                    }
                }
            }

            // 3. Fallback: Real-world surface plane raycast (if no printed marker is used)
            if (raycastManager != null && !_isBoardRevealed)
            {
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    // Place upright on detected real-world surface!
                    Quaternion uprightRot = Quaternion.LookRotation(
                        Vector3.ProjectOnPlane(_arCamera.transform.forward, Vector3.up),
                        Vector3.up
                    );
                    ToggleBoardReveal(hitPose.position, uprightRot);
                }
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

        private void ToggleBoardReveal(Vector3 position, Quaternion rotation)
        {
            if (_isBoardRevealed)
            {
                HideBoard();
            }
            else
            {
                RevealBoard(position, rotation);
            }
        }

        public void RevealBoard(Vector3 position, Quaternion rotation)
        {
            if (_spawnedBoardInstance == null) return;

            _targetPosition = position;
            _targetRotation = rotation;

            _boardTransform.position = position;
            _boardTransform.rotation = rotation;

            _spawnedBoardInstance.SetActive(true);
            _isBoardRevealed = true;

            if (_boardController != null)
            {
                _boardController.OnMarkerDetected(); // Triggers boot animation & audio
            }

            OnBoardRevealed?.Invoke();
        }

        public void HideBoard()
        {
            if (_spawnedBoardInstance == null) return;

            _isBoardRevealed = false;
            if (_boardController != null)
            {
                _boardController.OnMarkerLost();
            }
            else
            {
                _spawnedBoardInstance.SetActive(false);
            }

            OnBoardHidden?.Invoke();
        }

        private void UpdateBoardTransformSmoothing()
        {
            if (!_isBoardRevealed || _boardTransform == null) return;

            // If an image marker is tracked, continuously update target position
            if (_activeTrackedImage != null && _activeTrackedImage.trackingState == TrackingState.Tracking)
            {
                _targetPosition = _activeTrackedImage.transform.position;
                _targetRotation = CalculateUprightRotation(_activeTrackedImage.transform);
            }

            float dist = Vector3.Distance(_boardTransform.position, _targetPosition);
            if (dist > deadbandDistance)
            {
                _boardTransform.position = Vector3.Lerp(
                    _boardTransform.position,
                    _targetPosition,
                    Time.deltaTime * positionSmoothSpeed
                );
            }

            _boardTransform.rotation = Quaternion.Slerp(
                _boardTransform.rotation,
                _targetRotation,
                Time.deltaTime * rotationSmoothSpeed
            );
        }

        /// <summary>
        /// Calculates an UPRIGHT standing orientation for the 3D board relative to the marker.
        /// The board stands like an upright information monument rather than lying flat on the surface.
        /// </summary>
        private Quaternion CalculateUprightRotation(Transform markerTransform)
        {
            // If marker is on horizontal table (up = Vector3.up), board stands vertically:
            Vector3 forwardDir = Vector3.ProjectOnPlane(-markerTransform.forward, Vector3.up);
            if (forwardDir.sqrMagnitude < 0.001f)
            {
                forwardDir = Vector3.ProjectOnPlane(_arCamera.transform.position - markerTransform.position, Vector3.up);
            }
            return Quaternion.LookRotation(forwardDir.normalized, Vector3.up);
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            foreach (var img in eventArgs.added) ProcessTrackedImage(img);
            foreach (var img in eventArgs.updated) ProcessTrackedImage(img);
            foreach (var img in eventArgs.removed)
            {
                if (_activeTrackedImage == img)
                {
                    _isMarkerInView = false;
                    _activeTrackedImage = null;
                }
            }
        }

        private void ProcessTrackedImage(ARTrackedImage img)
        {
            if (!string.Equals(img.referenceImage.name, targetMarkerName, StringComparison.OrdinalIgnoreCase))
                return;

            OnMarkerStateChanged?.Invoke(img.referenceImage.name, img.trackingState);

            if (img.trackingState == TrackingState.Tracking)
            {
                _activeTrackedImage = img;
                if (!_isMarkerInView)
                {
                    _isMarkerInView = true;
                    OnMarkerRecognized?.Invoke();

                    // If Touch-To-Reveal is disabled, auto-reveal
                    if (!touchToRevealEnabled && !_isBoardRevealed)
                    {
                        RevealBoard(img.transform.position, CalculateUprightRotation(img.transform));
                    }
                }
            }
            else if (img.trackingState == TrackingState.None)
            {
                _isMarkerInView = false;
            }
        }

        /// <summary>
        /// Rescan hook for UI button.
        /// </summary>
        public void Rescan()
        {
            HideBoard();
            _activeTrackedImage = null;
            _isMarkerInView = false;
        }
    }
}
