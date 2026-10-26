using System;
using System.Collections;
using UnityEngine;

namespace CollegeAR.Timetable
{
    /// <summary>
    /// Controls the 3D futuristic Timetable Board:
    /// - Holographic boot-up animation (spring scaling)
    /// - Idle levitation floating effect
    /// - Glowing bezel pulse shader modulation
    /// - Dynamic timetable texture swapping
    /// - Maintains clean, upright orientation on surfaces
    /// </summary>
    public class TimetableBoardController : MonoBehaviour
    {
        [Header("Display Elements")]
        [Tooltip("MeshRenderer displaying the college timetable texture.")]
        [SerializeField] private MeshRenderer timetableQuadRenderer;

        [Tooltip("MeshRenderers for the glowing neon frame and cyber accents.")]
        [SerializeField] private MeshRenderer[] glowingFrameRenderers;

        [Tooltip("Root transform for applying local idle floating animation.")]
        [SerializeField] private Transform animatedContainer;

        [Header("Hologram Animation")]
        [SerializeField] private float bootAnimationDuration = 0.45f;
        [SerializeField] private float bobbingAmplitude = 0.012f; // 1.2 cm micro-bob
        [SerializeField] private float bobbingFrequency = 1.8f;
        [SerializeField] private float emissionPulseSpeed = 2.5f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip placementSound;

        private Vector3 _originalScale;
        private Vector3 _initialLocalPosition;
        private Coroutine _bootCoroutine;
        private MaterialPropertyBlock _propBlock;
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private Color _baseEmission = new Color(0f, 0.7f, 1f, 1f);

        private void Awake()
        {
            if (animatedContainer == null) animatedContainer = transform;
            _originalScale = animatedContainer.localScale;
            _initialLocalPosition = animatedContainer.localPosition;
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            // Subtle holographic floating levitation
            if (animatedContainer != null)
            {
                float offset = Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
                animatedContainer.localPosition = _initialLocalPosition + new Vector3(0f, offset, 0f);
            }

            // Pulsing neon emission glow on frame
            if (glowingFrameRenderers != null && glowingFrameRenderers.Length > 0)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * emissionPulseSpeed);
                Color pulseColor = _baseEmission * pulse;

                foreach (var r in glowingFrameRenderers)
                {
                    if (r != null)
                    {
                        r.GetPropertyBlock(_propBlock);
                        _propBlock.SetColor(EmissionColorProp, pulseColor);
                        r.SetPropertyBlock(_propBlock);
                    }
                }
            }
        }

        /// <summary>
        /// Triggered when the board is placed or repositioned on a surface.
        /// </summary>
        public void OnMarkerDetected()
        {
            if (_bootCoroutine != null) StopCoroutine(_bootCoroutine);
            _bootCoroutine = StartCoroutine(AnimateBootUp());

            if (audioSource != null && placementSound != null)
            {
                audioSource.PlayOneShot(placementSound);
            }
        }

        /// <summary>
        /// Called when board placement is reset/hidden.
        /// </summary>
        public void OnMarkerLost()
        {
            if (_bootCoroutine != null) StopCoroutine(_bootCoroutine);
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateBootUp()
        {
            float elapsed = 0f;
            animatedContainer.localScale = Vector3.zero;

            while (elapsed < bootAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bootAnimationDuration);
                
                // Overshoot spring easing curve
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                if (t > 0.8f) ease += Mathf.Sin((t - 0.8f) * Mathf.PI * 5f) * 0.05f;

                animatedContainer.localScale = _originalScale * ease;
                yield return null;
            }

            animatedContainer.localScale = _originalScale;
            _bootCoroutine = null;
        }

        /// <summary>
        /// Allows dynamically swapping the timetable image texture at runtime.
        /// </summary>
        public void SetTimetableTexture(Texture2D newTexture)
        {
            if (timetableQuadRenderer != null && newTexture != null)
            {
                timetableQuadRenderer.material.mainTexture = newTexture;
            }
        }
    }
}
