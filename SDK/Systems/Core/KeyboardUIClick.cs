using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRRayInteractor))]
public class KeyboardUIClick : MonoBehaviour
{
    XRRayInteractor m_Ray;
    bool m_WasHeld;

    void Awake()
    {
        m_Ray = GetComponent<XRRayInteractor>();
        m_Ray.uiPressInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
        Debug.Log($"[KeyboardUIClick] Awake on {name}. Mode now = {m_Ray.uiPressInput.inputSourceMode}");
    }

    void Update()
    {
        bool held = Input.GetKey(KeyCode.B);
        m_Ray.uiPressInput.QueueManualState(held, held ? 1f : 0f);

        if (held != m_WasHeld)
        {
            Debug.Log($"[KeyboardUIClick] B edge -> held={held}. " +
                      $"Mode={m_Ray.uiPressInput.inputSourceMode}, " +
                      $"ReadIsPerformed={m_Ray.uiPressInput.ReadIsPerformed()}, " +
                      $"hasHover={m_Ray.hasHover}");
            m_WasHeld = held;
        }
    }
}