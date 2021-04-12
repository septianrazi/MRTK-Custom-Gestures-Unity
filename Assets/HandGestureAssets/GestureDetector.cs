using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using Microsoft.MixedReality.Toolkit;

/// <summary>
/// Component to allow custom gesture detection
/// <remarks>
/// Recommended to be attached to the PalmPrefab
/// </remarks>
/// </summary>
public class GestureDetector : MonoBehaviour
{
    private IMixedRealityHandJointService HandJointService => CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();

    // List of gestures that have been saved and can be recognised later on in script
    [SerializeField] private List<Gesture> gestures;

    private void Start()
    {
        if (gestures.Count > 0)
            Debug.Log(gestures[0].name);

    }

    /// <summary>
    /// Saves the current gesture to a new Gesture Struct into the list of gestures
    /// <remarks>
    /// We use save both lists and Dictionaries because only Lists can be serializable in the Unity Editor, while dictionaries will allow for more efficient lookups.
    /// However, Dictionaries are not serialisable by Unity
    /// </remarks>
    /// </summary>
    [ContextMenu("Save Gesture")]
    private void SaveGesture()
    {
        // Save joint data for this gesture to a newGesture Gesture struct
        Gesture newGesture = InitialiseNewGesture();

        // loop through all joints and save the data to newGesture
        foreach (TrackedHandJoint joint in Enum.GetValues(typeof(TrackedHandJoint)))
        {
            if (HandJointUtils.TryGetJointPose(joint, Handedness.Left, out MixedRealityPose pose))
            {
                newGesture.joints.Add(joint);
                newGesture.jointPoses.Add(pose);
                newGesture.gestureJointData.Add(joint, pose);

                Vector3 jointPosFromPalm = transform.InverseTransformPoint(pose.Position);
                newGesture.positionsFromPalm.Add(jointPosFromPalm);  // Add to positions list the current joint's position relative to this gameObject (palm joint prefab)
                newGesture.gestureJointPositionData.Add(joint, jointPosFromPalm);  // Add to positions list the current joint's position relative to this gameObject (palm joint prefab)
            }
        }

        gestures.Add(newGesture);
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

    // custom events to trigger when recognising / losing gesture
    public UnityEvent onRecognise;
    public UnityEvent onDerecognise;
}