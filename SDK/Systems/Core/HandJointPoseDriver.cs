using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Liminal.SDK.V2
{
    public class HandJointPoseDriver : MonoBehaviour
    {
        [SerializeField] Handedness m_Handedness = Handedness.Left;
        [SerializeField] XRHandJointID m_Joint = XRHandJointID.Wrist;
        [SerializeField] Vector3 m_RotationOffsetEuler;

        XRHandSubsystem m_Subsystem;
        static readonly List<XRHandSubsystem> s_SubsystemsReuse = new();

        void LateUpdate()
        {
            if (m_Subsystem == null || !m_Subsystem.running)
            {
                SubsystemManager.GetSubsystems(s_SubsystemsReuse);
                m_Subsystem = null;
                for (int i = 0; i < s_SubsystemsReuse.Count; ++i)
                {
                    if (s_SubsystemsReuse[i].running)
                    {
                        m_Subsystem = s_SubsystemsReuse[i];
                        break;
                    }
                }
                if (m_Subsystem == null)
                    return;
            }

            var hand = m_Handedness == Handedness.Left ? m_Subsystem.leftHand : m_Subsystem.rightHand;
            if (!hand.isTracked)
                return;

            if (!hand.GetJoint(m_Joint).TryGetPose(out var pose))
                return;

            transform.localPosition = pose.position;
            transform.localRotation = pose.rotation * Quaternion.Euler(m_RotationOffsetEuler);
        }
    }
}
