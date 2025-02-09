﻿using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace SSVis
{
    [System.Serializable]
    public class ExtrusionDistanceChangedEvent : UnityEvent<ExtrusionEventData>
    {
    }

    [System.Serializable]
    public struct ExtrusionEventData
    {
        public float distance;
        public Vector3? extrusionPointLeft;
        public Vector3? extrusionPointRight;
        public Quaternion? extrusionRotationLeft;
        public Quaternion? extrusionRotationRight;
    }

    [System.Serializable]
    public class ExtrusionCloneDistanceReachedEvent : UnityEvent<GameObject>
    {
    }

    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(NearInteractionGrabbable))]
    public class ExtrusionHandle : MonoBehaviour, IMixedRealityPointerHandler
    {
        public ExtrusionDistanceChangedEvent OnExtrusionDistanceChanged = new ExtrusionDistanceChangedEvent();
        public ExtrusionCloneDistanceReachedEvent OnExtrusionCloneDistanceReached = new ExtrusionCloneDistanceReachedEvent();

        public DataVisualisation ExtrudingVisualisation;
        public AxisDirection ExtrusionDirection;
        public float InitialHandleThickness = 0.1f;
        public float InitialHandleWidth = 0.1f;
        public float InitialHandleHeight = 0.1f;
        public bool CloneOnMaxDistance = true;
        public float ExtrusionCloneDistance = 0.25f;
        public bool ExtrusionPersists = true;
        public float ExtrusionResetDistance = 0.05f;
        public bool FlipExtrusionCollider = false;
        public bool DisableNegativeExtrusion = false;

        private bool isInitalised = false;

        private BoxCollider boxCollider;
        private bool isExtruding = false;
        private IMixedRealityHand extrudingLeftHand;
        private IMixedRealityHand extrudingRightHand;

        private ExtrusionEventData extrusionData;

        private Transform leftHandStartPoint;
        private Transform rightHandStartPoint;
        private float leftHandStartDistance;
        private float rightHandStartDistance;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            extrusionData = new ExtrusionEventData();
        }

        public void Initialise(DataVisualisation extrudingVisualisation, AxisDirection extrusionDirection, Vector3 position, Vector3 scale,
                               float initialHandleThickness = 0.1f, float initialHandleWidth = 1f, float initialHandleHeight = 1f,          // Initial width and height are only used for diagonal extrusions at the moment
                               bool cloneOnMaxDistance = true, float extrusionCloneDistance = 0.25f, bool extrusionPersists = true, float extrusionResetDistance = 0.05f,
                               bool flipExtrusionCollider = false, bool disableNegativeExtrusion = false, string layer = "Front Trigger Layer")
        {
            transform.SetParent(extrudingVisualisation.transform);
            transform.localRotation = Quaternion.identity;
            this.ExtrudingVisualisation = extrudingVisualisation;
            this.ExtrusionDirection = extrusionDirection;
            this.InitialHandleThickness = initialHandleThickness;
            this.InitialHandleWidth = initialHandleWidth;
            this.InitialHandleHeight = initialHandleHeight;
            this.CloneOnMaxDistance = cloneOnMaxDistance;
            this.ExtrusionCloneDistance = extrusionCloneDistance;
            this.ExtrusionPersists = extrusionPersists;
            this.ExtrusionResetDistance = extrusionResetDistance;
            this.FlipExtrusionCollider = flipExtrusionCollider;
            this.DisableNegativeExtrusion = disableNegativeExtrusion;
            gameObject.layer = LayerMask.NameToLayer(layer);
            isInitalised = true;

            UpdateHandlePositionAndScale(position, scale);
        }

        public void UpdateHandlePositionAndScale(Vector3 position, Vector3 scale)
        {
            transform.localPosition = position;
            boxCollider.size = scale;
            UpdateColliderThickness();
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (!isInitalised)
                Debug.LogError("Extrusion Handle not yet initialised!");

            isExtruding = true;
            var hand = eventData.Pointer.Controller as IMixedRealityHand;
            if (hand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
            {
                if (hand.ControllerHandedness == Handedness.Left)
                {
                    extrudingLeftHand = hand;
                    extrusionData.extrusionPointLeft = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position);
                    extrusionData.extrusionRotationLeft = jointPose.Rotation;

                    if (leftHandStartPoint == null)
                        leftHandStartPoint = new GameObject("Left Extrusion Start Point").transform;
                    leftHandStartPoint.position = jointPose.Position;
                    leftHandStartPoint.rotation = ExtrudingVisualisation.Visualisation.transform.rotation;
                    leftHandStartPoint.localScale = ExtrudingVisualisation.Visualisation.transform.localScale;
                    leftHandStartDistance = extrusionData.distance;
                }
                else
                {
                    extrudingRightHand = hand;
                    extrusionData.extrusionPointRight = ExtrudingVisualisation.Visualisation.transform.InverseTransformPoint(jointPose.Position);
                    extrusionData.extrusionRotationRight = jointPose.Rotation;

                    if (rightHandStartPoint == null)
                        rightHandStartPoint = new GameObject("Right Extrusion Start Point").transform;
                    rightHandStartPoint.position = jointPose.Position;
                    rightHandStartPoint.rotation = ExtrudingVisualisation.Visualisation.transform.rotation;
                    rightHandStartPoint.localScale = ExtrudingVisualisation.Visualisation.transform.localScale;
                    rightHandStartDistance = extrusionData.distance;
                }
            }

            UpdateExtrusion();
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (!isInitalised)
                Debug.LogError("Extrusion Handle not yet initialised!");

            UpdateExtrusion();
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (!isInitalised)
                Debug.LogError("Extrusion Handle not yet initialised!");

            var hand = eventData.Pointer.Controller as IMixedRealityHand;
            if (hand.ControllerHandedness == Handedness.Left)
            {
                extrudingLeftHand = null;
                extrusionData.extrusionPointLeft = null;
                extrusionData.extrusionRotationLeft = null;

                if (leftHandStartPoint != null)
                    Destroy(leftHandStartPoint.gameObject);
            }
            else
            {
                extrudingRightHand = null;
                extrusionData.extrusionPointRight = null;
                extrusionData.extrusionRotationRight = null;

                if (rightHandStartPoint != null)
                    Destroy(rightHandStartPoint.gameObject);
            }

            if (extrudingLeftHand == null && extrudingRightHand == null)
            {
                isExtruding = false;
                // If this extrusion has fully ended, and is either small enough or does not persist, then send an update
                // to reset its extrusion distance down to 0
                if (Mathf.Abs(extrusionData.distance) < ExtrusionResetDistance || !ExtrusionPersists)
                {
                    extrusionData.distance = 0;
                    OnExtrusionDistanceChanged.Invoke(extrusionData);
                }
            }
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        private void UpdateExtrusion()
        {
            if (isExtruding)
            {
                // First, we calculate the extrusion distance
                // We need to get the joint pose positions first
                Vector3? point1 = null;
                Vector3? point2 = null;
                if (extrudingLeftHand != null && extrudingLeftHand.TryGetJoint(TrackedHandJoint.IndexTip, out MixedRealityPose jointPose))
                {
                    point1 = jointPose.Position;
                }
                if (extrudingRightHand != null && extrudingRightHand.TryGetJoint(TrackedHandJoint.IndexTip, out jointPose))
                {
                    point2 = jointPose.Position;
                }
                // Then we determine their extrusion distances based on the extruding direction (x, y, or z)
                float dist1 = 0;
                float dist2 = 0;
                switch (ExtrusionDirection)
                {
                    case AxisDirection.X:
                        if (point1 != null)
                            dist1 = leftHandStartPoint.InverseTransformPoint((Vector3)point1).x - leftHandStartDistance;
                        if (point2 != null)
                            dist2 = rightHandStartPoint.InverseTransformPoint((Vector3)point2).x - rightHandStartDistance;
                        break;
                    case AxisDirection.Y:
                        if (point1 != null)
                            dist1 = leftHandStartPoint.InverseTransformPoint((Vector3)point1).y - leftHandStartDistance;
                        if (point2 != null)
                            dist2 = rightHandStartPoint.InverseTransformPoint((Vector3)point2).y - rightHandStartDistance;
                        break;
                    case AxisDirection.Z:
                        if (point1 != null)
                            dist1 = leftHandStartPoint.InverseTransformPoint((Vector3)point1).z - leftHandStartDistance;
                        if (point2 != null)
                            dist2 = rightHandStartDistance + rightHandStartPoint.InverseTransformPoint((Vector3)point2).z;
                        break;
                    case AxisDirection.X | AxisDirection.Y:
                        if (point1 != null)
                        {
                            Vector3 point = leftHandStartPoint.InverseTransformPoint((Vector3)point1);
                            dist1 = Mathf.Min(point.x, point.y) + leftHandStartDistance;
                        }
                        if (point2 != null)
                        {
                            Vector3 point = rightHandStartPoint.InverseTransformPoint((Vector3)point2);
                            dist2 = Mathf.Min(point.x, point.y) + rightHandStartDistance;
                        }
                        break;
                }

                // Take the one with the larger absolute value
                if (Mathf.Abs(dist1) > Mathf.Abs(dist2))
                    extrusionData.distance = dist1;
                else
                    extrusionData.distance = dist2;


                // If the handle is set to ignore negatives, we emit a distance of 0
                if (DisableNegativeExtrusion && extrusionData.distance < 0)
                {
                    extrusionData.distance = 0;
                    OnExtrusionDistanceChanged.Invoke(extrusionData);
                    // Update the thickness and position of the collider as well
                    UpdateColliderThickness();
                }

                // Second, we check if this distance exceeds the extrusion clone distance threshold. If it does, we clone it (which consequently halts all other extrusions)
                if (CloneOnMaxDistance && Mathf.Abs(extrusionData.distance) >= ExtrusionCloneDistance)
                {
                    isExtruding = false;
                    // Pass the index pointer which is the furthest away
                    if (Mathf.Abs(dist1) > Mathf.Abs(dist2))
                    {
                        GameObject idxTip = Microsoft.MixedReality.Toolkit.CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>().RequestJointTransform(TrackedHandJoint.IndexTip, Handedness.Left).gameObject;
                        OnExtrusionCloneDistanceReached.Invoke(idxTip);
                    }
                    else
                    {
                        GameObject idxTip = Microsoft.MixedReality.Toolkit.CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>().RequestJointTransform(TrackedHandJoint.IndexTip, Handedness.Right).gameObject;
                        OnExtrusionCloneDistanceReached.Invoke(idxTip);
                    }
                }
                // Otherwise, we emit the data
                else
                {
                    OnExtrusionDistanceChanged.Invoke(extrusionData);

                    // Update the thickness and position of the collider as well
                    UpdateColliderThickness();
                }
            }
        }

        private void UpdateColliderThickness()
        {
            float thickness = InitialHandleThickness + Mathf.Abs(extrusionData.distance);
            float offset = extrusionData.distance / 2;

            if (FlipExtrusionCollider)
                offset = -offset;

            // Apply changes to the collider itself based on the extrusion direction
            Vector3 size = boxCollider.size;
            Vector3 centre = boxCollider.center;
            switch (ExtrusionDirection)
            {
                case AxisDirection.X:
                    size.x = thickness;
                    centre.x = offset;
                    break;
                case AxisDirection.Y:
                    size.y = thickness;
                    centre.y = offset;
                    break;
                case AxisDirection.Z:
                    size.z = thickness;
                    centre.z = offset;
                    break;
                case AxisDirection.X | AxisDirection.Y:
                    size.x = InitialHandleWidth + Mathf.Abs(extrusionData.distance);
                    size.y = InitialHandleHeight + Mathf.Abs(extrusionData.distance);
                    centre.x = offset;
                    centre.y = offset;
                    break;
            }
            boxCollider.size = size;
            boxCollider.center = centre;
        }

        private void OnDestroy()
        {
            if (leftHandStartPoint != null)
                Destroy(leftHandStartPoint.gameObject);

            if (rightHandStartPoint != null)
                Destroy(rightHandStartPoint.gameObject);
        }
    }
}