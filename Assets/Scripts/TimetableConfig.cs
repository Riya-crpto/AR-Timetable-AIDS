using System;
using UnityEngine;

namespace CollegeAR.Timetable
{
    /// <summary>
    /// Configuration asset holding academic metadata and replaceable textures.
    /// Allows students and faculty to easily customize course info, semester, and timetable textures.
    /// </summary>
    [CreateAssetMenu(fileName = "TimetableConfig", menuName = "College AR/Timetable Config")]
    public class TimetableConfig : ScriptableObject
    {
        [Header("Academic Metadata")]
        public string departmentName = "Department of Computer Engineering";
        public string academicYear = "2026 - 2027";
        public string semester = "Semester VI";
        public string divisionOrBatch = "TY - Division B";
        public string classroomRoomNo = "Room 103 / ARVR Lab 609";

        [Header("Visual Assets")]
        [Tooltip("The high-resolution timetable graphic to render on the 3D board.")]
        public Texture2D timetableTexture;

        [Tooltip("Target physical marker width in meters (e.g. 0.15 = 15cm).")]
        public float physicalMarkerSizeMeters = 0.15f;

        [Header("Key Faculty Contacts")]
        public string hodName = "Dr. Department Head";
        public string classTeacher = "Prof. Faculty Coordinator";
    }
}
