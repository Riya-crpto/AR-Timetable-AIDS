# Faculty Demonstration & Viva Voce Defense Guide
## Project: Marker-Based 3D Augmented Reality College Timetable

---

## 1. Executive Summary & Academic Rationale

### The Problem
Physical notice boards and paper schedules in college departments are static, prone to wear and tear, easily outdated when lectures are rescheduled, and lack engaging contextual interaction. Digital displays, while dynamic, require expensive hardware installations outside every classroom door.

### The Proposed AR Solution
This project implements a **Marker-Based 3D Augmented Reality Timetable** using **Unity**, **AR Foundation**, and **Google ARCore**. By mounting a standardized, high-contrast optical marker outside each classroom or department laboratory, students and faculty can point their smartphones to instantly project a 3D futuristic, interactive schedule anchored rigidly to the wall or doorway in real time.

---

## 2. Step-by-Step Live Demonstration Script

Follow this script during your faculty evaluation for an impactful 3-minute presentation:

| Step | Action | What to Say / Demonstrate |
| :--- | :--- | :--- |
| **1. Introduction** (30 sec) | Display the printed **AR Reference Marker Sheet** on the table or wall. | *"Respected professors, our project addresses department notice board clutter and scheduling friction using Marker-Based Augmented Reality. Instead of generic plane detection, our system uses computer-vision fiducial image tracking to anchor a 3D interactive timetable to this specific physical marker."* |
| **2. Launch & Detection** (45 sec) | Open the AR application on your smartphone. Show the UI displaying **"SEARCHING FOR MARKER..."** with the targeting reticle. | *"Notice our screen HUD showing real-time tracking states. As I aim the camera at the marker, ARCore extracts ORB keypoints and computes the 6-DoF rigid pose."* |
| **3. AR Lock & Hologram** (45 sec) | Point the camera at the marker. The board springs to life with an audio chirp and neon emission pulse. Status switches to **"MARKER LOCKED [6-DOF AR ACTIVE]"**. | *"The 3D timetable board materializes with a holographic boot animation. Notice the beveled metallic frame, translucent acrylic backing, glowing cyan neon bezels, and the crisp high-resolution schedule extracted directly from our department."* |
| **4. 6-DoF Parallax & Stability** (30 sec) | Walk around the marker: move closer, tilt up, view from 45&deg; angles. | *"Notice the rigid spatial anchoring. Even as the camera pans or changes perspective, our custom low-pass LERP and SLERP filter dampens sensor jitter, preventing the board from shaking or drifting."* |
| **5. Tracking Loss & Recovery** (30 sec) | Temporarily obscure the marker with your hand, then uncover it. | *"When the marker is obstructed, the app enters a grace-period 'Limited Tracking' state rather than abruptly destroying the object. Once uncovered, it instantly re-locks without spawning duplicate boards."* |

---

## 3. Computer Vision & AR Tracking Pipeline

```
  [Physical Marker Image]
            │
            ▼
┌───────────────────────┐
│ 1. Video Frame Stream │ (Camera sensor captures YUV/RGB frames at 30-60 FPS)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 2. Feature Extraction │ (FAST Corner Detector & Multi-Scale Gaussian Pyramid)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 3. Keypoint Descriptors│ (ORB: Oriented FAST and Rotated BRIEF binary descriptors)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 4. Feature Matching   │ (Hamming distance match against XRReferenceImageLibrary)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 5. Pose Estimation    │ (Perspective-n-Point / PnP solver + RANSAC outlier filtering)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 6. Transform Alignment│ (Marker Space -> Camera Space -> Unity AR World Space)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 7. Jitter Damping     │ (Low-Pass LERP/SLERP interpolation with deadband threshold)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ 8. Holographic Render │ (3D Mesh, Emission Shader, Day Highlights & UI HUD)
└───────────────────────┘
```

---

## 4. Comprehensive Technical Viva Voce Questions & Answers

### Q1: Why did you choose Marker-Based AR instead of Markerless Plane Detection (SLAM)?
**Answer:**
Markerless SLAM (Simultaneous Localization and Mapping) detects horizontal or vertical planes (like floors or bare tables) without knowing *what* or *where* the surface is. For a college timetable:
1. **Contextual Determinism:** An image marker placed at "Room 103" guarantees that the AR engine displays Room 103's schedule, rather than requiring the user to tap on an arbitrary wall.
2. **Instant Rigid Pose:** Marker tracking provides an instantaneous origin, scale, and orientation reference with zero initialization delay, whereas plane detection requires the user to walk around sweeping their phone to estimate surfaces.
3. **Reproducibility:** Every student scanning the door marker sees the timetable at the exact same location and scale (15cm reference).

---

### Q2: How does `ARTrackedImageManager` work in Unity AR Foundation?
**Answer:**
`ARTrackedImageManager` acts as an abstraction layer over the native mobile AR subsystem (`Google ARCore` on Android, `Apple ARKit` on iOS). During startup:
1. It compiles the `XRReferenceImageLibrary` into platform-specific binary descriptor databases.
2. Every frame, it checks the camera feed against these descriptors.
3. When detected, it raises the `trackedImagesChanged` event with three collections: `added`, `updated`, and `removed`.
4. Our `ARTimedMarkerManager` script listens to these events, manages the lifecycle of the 3D board, and updates position and rotation.

---

### Q3: What computer vision algorithms are used to detect the marker?
**Answer:**
ARCore uses **ORB (Oriented FAST and Rotated BRIEF)** feature detection:
- **FAST (Features from Accelerated Segment Test):** Detects corner points by comparing pixel intensities in a circle around candidate pixels.
- **Orientation Assignment:** Computes the intensity centroid of the corner patch to achieve rotation invariance.
- **rBRIEF (Rotated Binary Robust Independent Elementary Features):** Encodes feature appearance into a compact 256-bit binary string. Binary descriptor matching is performed using ultra-fast bitwise XOR (Hamming distance).

---

### Q4: Why does your marker design achieve a high trackability score?
**Answer:**
ARCore evaluates reference images on a scale of 0 to 100 based on feature density and asymmetry:
- **Asymmetric Corners:** Each corner of our marker has a completely distinct geometric shape (solid square, diagonal wedge, concentric square, crosshair dot). This prevents orientation ambiguity (the camera immediately knows which side is top vs bottom).
- **High-Frequency Gradients:** The black-and-white dashed rails along the borders produce hundreds of sharp edge-corner transitions.
- **Non-Repeating Patterns:** Concentric circles with spaced feature dots prevent local false-positive descriptor matches.

---

### Q5: How is the 3D position and rotation (6-DoF pose) calculated from a 2D image?
**Answer:**
Through the **Perspective-n-Point (PnP)** problem:
We know the 3D physical coordinates of the marker's corners in marker space (e.g., $(-0.075, 0.075, 0)$ to $(0.075, -0.075, 0)$ for a 15cm marker). The camera detects their 2D pixel projections $(u, v)$ on the image sensor.
Using the camera intrinsic matrix $K$ (calibrated focal lengths $f_x, f_y$ and principal point $c_x, c_y$):
$$
s \begin{bmatrix} u \\ v \\ 1 \end{bmatrix} = K \begin{bmatrix} R & T \end{bmatrix} \begin{bmatrix} X_w \\ Y_w \\ Z_w \\ 1 \end{bmatrix}
$$
The PnP solver calculates the rotation matrix $R$ ($3 \times 3$) and translation vector $T$ ($3 \times 1$), yielding the exact 6-Degrees-of-Freedom pose. **RANSAC (Random Sample Consensus)** filters out outlier feature points caused by glare or shadows.

---

### Q6: How do you prevent duplicate boards from spawning?
**Answer:**
A common bug in beginner AR apps is instantiating a new GameObject every time `trackedImagesChanged.added` fires. We prevent this by:
1. Implementing a **Single-Instance Object Pool**: The 3D board is pre-instantiated in a disabled state during `Awake()`.
2. Tracking the `trackableId` of the detected image.
3. Enabling and updating the transform of the existing instance rather than instantiating new GameObjects.

---

### Q7: What is LERP/SLERP damping and why is deadband filtering needed?
**Answer:**
Mobile phone cameras experience micro-hand tremors and optical sensor noise, causing raw tracking poses to jitter rapidly.
- **Linear Interpolation (LERP):** Smooths position:
  $$\vec{P}_{t} = \text{Lerp}(\vec{P}_{\text{current}}, \vec{P}_{\text{target}}, \Delta t \times \text{smoothSpeed})$$
- **Spherical Linear Interpolation (SLERP):** Smooths rotation along the quaternion hypersphere without gimbal lock:
  $$Q_t = \text{Slerp}(Q_{\text{current}}, Q_{\text{target}}, \Delta t \times \text{smoothSpeed})$$
- **Deadband Filter:** If the Euclidean distance between current and target position is below 2 millimeters ($0.002\text{ m}$), we ignore the update. This eliminates stationary hovering jitter completely.

---

### Q8: How does the application handle temporary tracking loss?
**Answer:**
When lighting shifts or a student walks past the camera, ARCore changes state from `TrackingState.Tracking` to `TrackingState.Limited`.
Instead of immediately destroying or hiding the board (which causes disorienting blinking), our script activates a **1.2-second grace-period timer**. The board remains anchored at its last known reliable pose. If tracking is restored within 1.2 seconds, tracking resumes seamlessly; only if the marker remains lost does the board deactivate cleanly.

---

### Q9: What are the coordinate system transformations involved?
**Answer:**
1. **Marker Space $(M)$:** Local origin at the center of the printed paper marker $(0, 0, 0)$, $Z$ normal to the page.
2. **Camera Space $(C)$:** Origin at the smartphone camera sensor lens.
3. **World AR Space $(W)$:** Unity world coordinate system established when the AR Session initializes.
The transformation is expressed as:
$$T_{W \to \text{Board}} = T_{W \to C} \times T_{C \to M} \times T_{M \to \text{Board}}$$

---

### Q10: What are the minimum requirements for Google ARCore on Android?
**Answer:**
- **OS Version:** Android 7.0 (API Level 24) or higher.
- **Hardware Prerequisites:** Rear camera with calibrated intrinsic parameters, integrated IMU (accelerometer and gyroscope) with hardware timestamp synchronization, and a certified chipset listed on Google's ARCore Supported Devices registry.
- **Google Play Services for AR:** Must be installed on the device.

---

### Q11: How is the timetable graphic kept replaceable?
**Answer:**
We decoupled the data/graphics layer from the AR tracking engine:
- The timetable image is stored as an independent texture (`Timetable_College.png`) mapped onto a dedicated Quad mesh renderer.
- The `TimetableBoardController.cs` exposes a `SetTimetableTexture(Texture2D newTexture)` method and Inspector slot.
- Any new timetable image can be dropped in to instantly replace the display without altering tracking code, shaders, or transforms.

---

### Q12: What is the difference between AR Foundation and ARCore?
**Answer:**
- **Google ARCore:** Google's proprietary native Android SDK that performs feature tracking, visual-inertial odometry (VIO), and environmental understanding.
- **Unity AR Foundation:** Unity's cross-platform framework that provides a unified C# API (`ARSession`, `ARTrackedImageManager`, `ARCameraManager`) abstracting both Google ARCore (Android) and Apple ARKit (iOS). Code written in AR Foundation runs on both ecosystems without rewriting.

---

### Q13: How is the futuristic holographic aesthetic implemented?
**Answer:**
1. **Depth Layering:** Rather than a flat image, the board is composed of multiple 3D spatial layers: a dark cybernetic metallic backplate, an emissive neon rail layer, a semi-transparent acrylic frosted glass layer, and the timetable quad.
2. **Dynamic Emission Pulse:** A C# coroutine modulates the material's `_EmissionColor` using a sinusoidal wave $I = I_{\text{base}} \times (0.7 + 0.3 \sin(\omega t))$.
3. **Idle Levitation:** A gentle harmonic oscillation $y(t) = y_0 + A \sin(\omega t)$ creates a holographic floating effect.

---

### Q14: How does the application preserve battery and thermal limits?
**Answer:**
1. Single-instance GameObject pooling avoids garbage collection spikes and memory allocations.
2. Deadband filtering halts transform updates when the user and marker are stationary.
3. Textures are pre-compressed and mipmapped to conserve mobile GPU bandwidth.
4. Target frame rate is synchronized with camera refresh rate (30/60 FPS) to prevent runaway CPU loops.

---

### Q15: What are future real-world extensions for this project?
**Answer:**
1. **Cloud Database Sync:** Integrate Cloud Firestore or REST APIs so faculty can reschedule a class from their web portal, instantly updating the AR timetable across campus.
2. **Voice & Audio Announcements:** Text-to-Speech (TTS) integration announcing the next upcoming lecture when scanned.
3. **Multi-Marker Campus Routing:** Place markers at corridor intersections to show 3D AR directional arrows guiding students to their respective classrooms.
