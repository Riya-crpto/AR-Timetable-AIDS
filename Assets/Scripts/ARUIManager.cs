using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CollegeAR.Timetable
{
    /// <summary>
    /// Minimalist Google Lens / PhonePe style camera HUD.
    /// Provides clean, non-intrusive floating instructions, repositioning feedback, and reset controls.
    /// </summary>
    public class ARUIManager : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private ARSurfacePlacementManager placementManager;

        [Header("Minimal HUD Elements")]
        [SerializeField] private Text instructionText;
        [SerializeField] private GameObject instructionCard;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        [Header("Toast Notification")]
        [SerializeField] private Text toastText;
        [SerializeField] private GameObject toastCard;

        private Coroutine _toastCoroutine;

        private void Start()
        {
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClicked);
                resetButton.gameObject.SetActive(false); // Only show after placed
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (placementManager != null)
            {
                placementManager.OnBoardPlaced += OnBoardPlaced;
                placementManager.OnBoardRepositioned += OnBoardRepositioned;
                placementManager.OnBoardReset += OnBoardReset;
                placementManager.OnNoSurfaceDetected += OnNoSurfaceDetected;
            }

            ShowInstruction("Scan your surroundings and tap a surface to place the timetable.");
        }

        private void OnDestroy()
        {
            if (placementManager != null)
            {
                placementManager.OnBoardPlaced -= OnBoardPlaced;
                placementManager.OnBoardRepositioned -= OnBoardRepositioned;
                placementManager.OnBoardReset -= OnBoardReset;
                placementManager.OnNoSurfaceDetected -= OnNoSurfaceDetected;
            }
        }

        private void OnBoardPlaced()
        {
            ShowInstruction("3D Timetable Anchored. Tap another surface to reposition.");
            if (resetButton != null) resetButton.gameObject.SetActive(true);
        }

        private void OnBoardRepositioned()
        {
            ShowInstruction("Timetable Repositioned. Tap anywhere to move, or tap Reset.");
        }

        private void OnBoardReset()
        {
            ShowInstruction("Scan your surroundings and tap a surface to place the timetable.");
            if (resetButton != null) resetButton.gameObject.SetActive(false);
        }

        private void OnNoSurfaceDetected()
        {
            ShowToast("No surface detected here. Slowly scan your floor, table, or wall.", 2.5f);
        }

        private void OnResetClicked()
        {
            if (placementManager != null) placementManager.ResetPlacement();
        }

        private void OnBackClicked()
        {
            // Reset placement or exit app
            if (placementManager != null && placementManager.IsPlaced)
            {
                placementManager.ResetPlacement();
            }
            else
            {
                Application.Quit();
            }
        }

        private void ShowInstruction(string text)
        {
            if (instructionText != null) instructionText.text = text;
            if (instructionCard != null) instructionCard.SetActive(true);
        }

        private void ShowToast(string message, float duration)
        {
            if (toastCard == null || toastText == null) return;

            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(ToastRoutine(message, duration));
        }

        private IEnumerator ToastRoutine(string message, float duration)
        {
            toastText.text = message;
            toastCard.SetActive(true);
            yield return new WaitForSeconds(duration);
            toastCard.SetActive(false);
            _toastCoroutine = null;
        }
    }
}
