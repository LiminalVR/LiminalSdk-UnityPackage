using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[Serializable]
public class RayReference
{
    public XRRayInteractor Ray;
}

[RequireComponent(typeof(XRRayInteractor))]
public class KeyboardUIClick : MonoBehaviour
{
    [SerializeField] KeyCode m_Key = KeyCode.B;

    XRRayInteractor m_Ray;
    KeyboardOrBypass m_Bypass;

    void Awake()
    {
        m_Ray = GetComponent<XRRayInteractor>();
        m_Bypass = new KeyboardOrBypass(m_Ray.uiPressInput, () => Input.GetKey(m_Key));
        m_Ray.uiPressInput.bypass = m_Bypass;
    }

    void OnDestroy()
    {
        if (m_Ray != null && ReferenceEquals(m_Ray.uiPressInput.bypass, m_Bypass))
            m_Ray.uiPressInput.bypass = null;
    }

    sealed class KeyboardOrBypass : IXRInputButtonReader
    {
        readonly XRInputButtonReader m_Original;
        readonly Func<bool> m_KeyHeld;

        int m_LastEdgeFrame = -1;
        bool m_LastHeld;
        int m_PressedFrame = -1;
        int m_ReleasedFrame = -1;

        public KeyboardOrBypass(XRInputButtonReader original, Func<bool> keyHeld)
        {
            m_Original = original;
            m_KeyHeld = keyHeld;
        }

        void TickEdges()
        {
            if (m_LastEdgeFrame == Time.frameCount) return;
            m_LastEdgeFrame = Time.frameCount;

            bool held = m_KeyHeld();
            if (held && !m_LastHeld) m_PressedFrame = Time.frameCount;
            else if (!held && m_LastHeld) m_ReleasedFrame = Time.frameCount;
            m_LastHeld = held;
        }

        public bool ReadIsPerformed()
        {
            TickEdges();
            return m_KeyHeld() || m_Original.ReadIsPerformed();
        }

        public bool ReadWasPerformedThisFrame()
        {
            TickEdges();
            return m_PressedFrame == Time.frameCount || m_Original.ReadWasPerformedThisFrame();
        }

        public bool ReadWasCompletedThisFrame()
        {
            TickEdges();
            return m_ReleasedFrame == Time.frameCount || m_Original.ReadWasCompletedThisFrame();
        }

        public float ReadValue() => m_KeyHeld() ? 1f : m_Original.ReadValue();

        public bool TryReadValue(out float value)
        {
            if (m_KeyHeld()) { value = 1f; return true; }
            return m_Original.TryReadValue(out value);
        }
    }
}