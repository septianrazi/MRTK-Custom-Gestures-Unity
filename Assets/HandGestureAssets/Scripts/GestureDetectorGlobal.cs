using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using Microsoft.MixedReality.Toolkit;

namespace CustomGestureDetector
{
    /// <summary>
    /// Component to allow custom gesture detection
    /// <remarks>
    /// Recommended to be attached to the PalmPrefab
    /// </remarks>
    /// </summary>
    public class GestureDetectorGlobal : MonoBehaviour
    {
        [SerializeField] private float gestureRecogThresh = 0.25f;              // Allowed margin of error of the cumulative distance between a gesture and current pose, value of 0.25 seems quite optimal

        [SerializeField] private Handedness defaultHandedness = Handedness.Any; // The handedness of the palm joint this component is attached to

        [SerializeField] private bool DEBUGMODE = false;                        // Whether currently in debug mode or not

        private IMixedRealityHandJointService HandJointService => CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();

        // List of gestures that have been saved and can be recognised later on in script
        [SerializeField] private List<Gesture> gestures;

        private Gesture prevGesture;                                             // the most recent gesture in the previous frame, kept track to trigger Change events




        private void Start()
        {
            //transform.parent.TryGetComponent(out BaseHandVisualizer baseHandVisualizer);
            //handedness = baseHandVisualizer.Handedness;
            //if (handedness == Handedness.None)
            //    handedness = Handedness.Both;

            //if (gestures.Count > 0)
            //    Debug.Log(gestures[0].name);
        }

        private void Update()
        {
            // Checking Key Presses
            if (DEBUGMODE)
                CheckKeyPresses();
        }

        private void FixedUpdate()
        {
            //-------------------------------------------------------------------
            //Handles the Detection of Gestures and their corresponding functions
            //-------------------------------------------------------------------
            Gesture currentDetectedGesture = TryRecogniseGesture();

            bool hasRecognisedGesture = !currentDetectedGesture.Equals(new Gesture());      // Set flag whether gesture has been detected

            if (hasRecognisedGesture && !currentDetectedGesture.Equals(prevGesture))        // if gesture is recognised and is different to before
            {
                if (!prevGesture.Equals(new Gesture()))                                     // if gesture transitioned from another saved gesture, invoke the onderecognised event
                    prevGesture.onDerecognise.Invoke();

                prevGesture = currentDetectedGesture;
                currentDetectedGesture.onRecognise.Invoke();
                //Debug.Log("TRIGGERED " + currentDetectedGesture.name);
            }
            else if (!hasRecognisedGesture && !currentDetectedGesture.Equals(prevGesture))  // gesture is not recognised anymore and different to before
            {
                prevGesture.onDerecognise.Invoke();
                prevGesture = currentDetectedGesture;
            }
        }

        /// <summary>
        /// Checks the keypresses in debug mode
        /// </summary>
        void CheckKeyPresses()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SaveGestureDefault();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SaveGestureLeft();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SaveGestureRight();
            }
        }

        [ContextMenu("Save Left Gesture")]
        private void SaveGestureLeft()
        {
            SaveGesture(Handedness.Left);
        }

        [ContextMenu("Save Right Gesture")]
        private void SaveGestureRight()
        {
            SaveGesture(Handedness.Right);
        }

        [ContextMenu("Save Default Gesture")]
        private void SaveGestureDefault()
        {
            SaveGesture(defaultHandedness);
        }

        /// <summary>
        /// Saves the current gesture to a new Gesture Struct into the list of gestures
        /// </summary>
        /// <param name="handedness">Which hand to save gesture from</param>
        /// <remarks>
        /// We use save both lists and Dictionaries because only Lists can be serializable in the Unity Editor, while dictionaries will allow for more efficient lookups.
        /// However, Dictionaries are not serialisable by Unity
        /// </remarks>
        private void SaveGesture(Handedness handedness)
        {
            // Save joint data for this gesture to a newGesture Gesture struct
            Gesture newGesture = InitialiseNewGesture();

            Vector3 palmPos = Vector3.zero;
            Quaternion palmRot = Quaternion.identity;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, handedness, out MixedRealityPose palmPose))
            {
                palmPos = palmPose.Position;
                palmRot = palmPose.Rotation;
            }


            // loop through all joints and save the data to newGesture
            foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            {
                if (HandJointUtils.TryGetJointPose(joint, handedness, out MixedRealityPose pose))
                {
                    newGesture.handedness = handedness;
                    newGesture.joints.Add(joint);
                    newGesture.jointPoses.Add(pose);
                    newGesture.gestureJointData.Add(joint, pose);

                    Vector3 jointPosFromPalm = InverseTransformPoint(palmPos, palmRot, Vector3.one, pose.Position);

                    newGesture.positionsFromPalm.Add(jointPosFromPalm);  // Add to positions list the current joint's position relative to this gameObject (palm joint prefab)
                    newGesture.gestureJointPositionData.Add(joint, jointPosFromPalm);  // Add to positions list the current joint's position relative to this gameObject (palm joint prefab)
                }
            }

            gestures.Add(newGesture);
        }

        /// <summary>
        /// Converts a position in world space into local space relative to reference. InverseTransformPoint() function without having to include the transform. 
        /// </summary>
        /// <remarks>
        /// Obtained from PraetorBlue https://forum.unity.com/threads/transform-inversetransformpoint-without-transform.954939/
        /// </remarks>
        /// <param name="refPos">Position of the reference</param>
        /// <param name="refRot">Rotation of the reference</param>
        /// <param name="refScale">Scale of the reference</param>
        /// <param name="pos">Position of object to be converted to local space</param>
        /// <returns> Position of given object within local space </returns>
        Vector3 InverseTransformPoint(Vector3 refPos, Quaternion refRot, Vector3 refScale, Vector3 pos)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(refPos, refRot, refScale);
            Matrix4x4 inverse = matrix.inverse;
            return inverse.MultiplyPoint3x4(pos);
        }

        /// <summary>
        /// Function to initialise all the lists and dictionaries within our Gestures struct
        /// </summary>
        /// <returns> Gesture object with initialised attributes </returns>
        private Gesture InitialiseNewGesture()
        {
            Gesture newGesture = new Gesture
            {
                name = "New Gesture",
                gestureJointData = new Dictionary<TrackedHandJoint, MixedRealityPose>(),
                gestureJointPositionData = new Dictionary<TrackedHandJoint, Vector3>(),

                joints = new List<TrackedHandJoint>(),
                jointPoses = new List<MixedRealityPose>(),
                positionsFromPalm = new List<Vector3>(),
            };

            return newGesture;
        }


        /// <summary>
        /// Debug Function to print palm gestures
        /// </summary>
        [ContextMenu("Print Palm Gesture")]
        private void PrintPalmGestureData()
        {
            string toPrint = "";
            if (HandJointService.Equals(null))
                Debug.LogError("NO HANDS DETECTED WHILE ATTEMPTING TO PRINT PALM GESTURE");

            foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            {
                if (HandJointUtils.TryGetJointPose(joint, Handedness.Right, out MixedRealityPose rightPose))
                    toPrint += (joint + " " + rightPose.Position + "\n");

                if (HandJointUtils.TryGetJointPose(joint, Handedness.Left, out MixedRealityPose leftPose))
                    toPrint += (joint + " " + leftPose.Position + "\n");
            }
            Debug.Log(toPrint);
        }



        /// <summary>
        /// Polling function to find the gesture that matches current pose
        /// </summary>
        /// <returns> Returns the saved gesture that is most likely, or an empty Gesture object if there is none </returns>
        Gesture TryRecogniseGesture()
        {
            Gesture bestGesture = new Gesture();

            float bestDistance = Mathf.Infinity;
            foreach (Gesture gesture in gestures)
            {
                float thisDistance = CompareGestureDistancesToCurrent(gesture);

                if (thisDistance > gestureRecogThresh)
                    continue;

                if (thisDistance < bestDistance)
                {
                    bestDistance = thisDistance;
                    bestGesture = gesture;
                }
            }
            return bestGesture;
        }

        /// <summary>
        /// Helper function to compare the current palm pose and a given gesture pose
        /// </summary>
        /// <param name="gesture"> Gesture struct to match the current pose with </param>
        /// <returns> Cumulative distance of the current joints to the joints within the saved gesture </returns>
        float CompareGestureDistancesToCurrent(Gesture gesture)
        {

            Handedness thisHandedness = gesture.handedness;
            float distanceSum = 0;

            Vector3 palmPos = Vector3.zero;
            Quaternion palmRot = Quaternion.identity;

            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, thisHandedness, out MixedRealityPose palmPose))
            {
                palmPos = palmPose.Position;
                palmRot = palmPose.Rotation;
            }
            else
            {
                return Mathf.Infinity;
            }

            foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
            {
                if (HandJointUtils.TryGetJointPose(joint, thisHandedness, out MixedRealityPose currentPose))
                {
                    int thisGesturePoseIndex = gesture.joints.IndexOf(joint);
                    Vector3 thisSavedPosition = Vector3.zero;
                    try
                    {
                        thisSavedPosition = gesture.positionsFromPalm[thisGesturePoseIndex];
                    }
                    catch (Exception _)
                    {
                        continue;
                    }
                    //MixedRealityPose thisGesturePose = gesture.jointPoses[thisGesturePoseIndex];

                    Vector3 thisCurrentPosition = InverseTransformPoint(palmPos, palmRot, Vector3.one, currentPose.Position);
                    distanceSum += Vector3.Distance(thisSavedPosition, thisCurrentPosition);

                    if (distanceSum > gestureRecogThresh)
                        return Mathf.Infinity;
                }
            }

            if (distanceSum == 0)
                return Mathf.Infinity;
            return distanceSum;
        }

    }

    /// <summary>
    /// Struct to store information regarding the custom gestures
    /// </summary>
    [System.Serializable]
    public struct Gesture
    {
        public string name;

        // Gesture Data stored
        public Dictionary<TrackedHandJoint, MixedRealityPose> gestureJointData;
        public Dictionary<TrackedHandJoint, Vector3> gestureJointPositionData;

        public List<TrackedHandJoint> joints;
        public List<MixedRealityPose> jointPoses;
        public List<Vector3> positionsFromPalm;      // List of joint's positions from the palm

        public Handedness handedness;

        // custom events to trigger when recognising / losing gesture
        public UnityEvent onRecognise;
        public UnityEvent onDerecognise;
    }
}