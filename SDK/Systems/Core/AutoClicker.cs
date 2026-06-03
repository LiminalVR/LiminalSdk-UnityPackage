using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace Liminal.SDK.V2
{
public class AutoClicker : MonoBehaviour
{
    public enum ClickMode
    {
        // Press for 1 frame, release. Each pulse interval = one click.
        Pulse,
        // Stay pressed continuously. Each pulse interval briefly releases and re-presses
        // so the UI fires a click event while the press remains the resting state.
        Hold,
        // Latch onto whatever UI element the ray is currently hovering: click-then-hold it.
        // Releases automatically when the target changes or disappears. While no target,
        // pulse-clicks at ClickRate trying to land on something.
        Latch,
    }

    public ExperienceAppManager ExperienceAppManager;
    public XRRigReferences RigReferences =>
        ExperienceAppManager == null ? null : ExperienceAppManager.SpawnedRig;

    public RayReference LeftRay;
    public RayReference RightRay;

    public ClickMode Mode = ClickMode.Pulse;
    public float ClickRate = 0.2f;

    private float _clickTimer;
    private PulseBypass _leftBypass;
    private PulseBypass _rightBypass;
    private GameObject _leftLatched;
    private GameObject _rightLatched;
    private XRRigReferences _wiredRig;

    private void OnDisable()
    {
        _leftBypass?.ReleaseHold();
        _rightBypass?.ReleaseHold();
        _leftLatched = null;
        _rightLatched = null;
    }

    private void OnDestroy()
    {
        Unwire();
    }

    private void Update()
    {
        EnsureWired();

        _clickTimer += Time.deltaTime;
        bool fireRate = _clickTimer >= ClickRate;
        if (fireRate) _clickTimer = 0f;

        switch (Mode)
        {
            case ClickMode.Pulse:
                ApplySimple(_leftBypass, hold: false, fireRate);
                ApplySimple(_rightBypass, hold: false, fireRate);
                break;
            case ClickMode.Hold:
                ApplySimple(_leftBypass, hold: true, fireRate);
                ApplySimple(_rightBypass, hold: true, fireRate);
                break;
            case ClickMode.Latch:
                ApplyLatch(LeftRay, _leftBypass, ref _leftLatched, fireRate);
                ApplyLatch(RightRay, _rightBypass, ref _rightLatched, fireRate);
                break;
        }
    }

    private static void ApplySimple(PulseBypass bypass, bool hold, bool fireClick)
    {
        if (bypass == null) return;

        if (fireClick) bypass.PulseClick(hold);
        bypass.Tick();
        if (hold) bypass.MaintainHold();
        else bypass.ReleaseHold();
    }

    private static void ApplyLatch(RayReference rayRef, PulseBypass bypass, ref GameObject latched, bool fireRate)
    {
        if (bypass == null) return;

        GameObject current = GetCurrentUITarget(rayRef);

        if (current != latched)
        {
            // Target changed (or appeared/disappeared). Drop old hold, click-and-hold the new one.
            if (latched != null)
                bypass.ReleaseHold();

            if (current != null)
                bypass.ClickAndHold();

            latched = current;
        }
        else if (current != null)
        {
            // Same target — keep holding. (No clicks fired on ClickRate while latched.)
            bypass.MaintainHold();
        }
        else if (fireRate)
        {
            // No target and no latch — fire search pulses trying to land on something.
            bypass.PulseClick(holdAfter: false);
        }

        bypass.Tick();
    }

    private void EnsureWired()
    {
        var rig = RigReferences;
        if (ReferenceEquals(rig, _wiredRig)) return;

        Unwire();
        if (rig != null) Wire(rig);
        _wiredRig = rig;
    }

    private void Wire(XRRigReferences rig)
    {
        LeftRay ??= new RayReference();
        RightRay ??= new RayReference();
        LeftRay.Ray = rig.LeftHandRayInteractor;
        RightRay.Ray = rig.RightHandRayInteractor;
        _leftBypass = Install(LeftRay);
        _rightBypass = Install(RightRay);
    }

    private void Unwire()
    {
        Uninstall(LeftRay, _leftBypass);
        Uninstall(RightRay, _rightBypass);
        _leftBypass = null;
        _rightBypass = null;
        _leftLatched = null;
        _rightLatched = null;
    }

    private static GameObject GetCurrentUITarget(RayReference rayRef)
    {
        if (rayRef?.Ray == null) return null;
        if (rayRef.Ray.TryGetCurrentUIRaycastResult(out var result))
            return result.gameObject;
        return null;
    }

    private static PulseBypass Install(RayReference rayRef)
    {
        if (rayRef?.Ray == null) return null;
        var bypass = new PulseBypass(rayRef.Ray.uiPressInput);
        rayRef.Ray.uiPressInput.bypass = bypass;
        return bypass;
    }

    private static void Uninstall(RayReference rayRef, PulseBypass bypass)
    {
        if (bypass == null || rayRef?.Ray == null) return;
        if (ReferenceEquals(rayRef.Ray.uiPressInput.bypass, bypass))
            rayRef.Ray.uiPressInput.bypass = null;
    }

    private sealed class PulseBypass : IXRInputButtonReader
    {
        private readonly XRInputButtonReader _original;
        private int _pressFrame = -1;
        private int _releaseFrame = -1;
        private bool _pressed;
        private bool _hold;
        private bool _pressNextFrame;
        private bool _holdAfterRepress;

        public PulseBypass(XRInputButtonReader original) { _original = original; }

        // Fires a click. If holdAfter is true, the press resumes next frame (Hold mode).
        // If false, the press releases on the next Tick (Pulse mode).
        public void PulseClick(bool holdAfter)
        {
            if (_pressed)
            {
                // Already held — release this frame to trigger PointerUp/click.
                _pressed = false;
                _releaseFrame = Time.frameCount;
                if (holdAfter) _pressNextFrame = true;
            }
            else
            {
                // Idle — start a fresh press.
                _pressed = true;
                _pressFrame = Time.frameCount;
                _hold = holdAfter;
            }
        }

        // Press, release on next Tick (firing the click), then re-press the frame after for a sustained hold.
        // Produces PointerDown -> PointerUp(click) -> PointerDown(held) over 3 frames.
        public void ClickAndHold()
        {
            _pressed = true;
            _pressFrame = Time.frameCount;
            _hold = false;
            _holdAfterRepress = true;
        }

        // Called every frame. Auto-releases pulses and re-presses scheduled clicks.
        public void Tick()
        {
            if (_pressed && Time.frameCount > _pressFrame && !_hold)
            {
                _pressed = false;
                _releaseFrame = Time.frameCount;
                if (_holdAfterRepress)
                {
                    _pressNextFrame = true;
                    _hold = true;
                    _holdAfterRepress = false;
                }
            }

            if (_pressNextFrame && !_pressed && Time.frameCount > _releaseFrame)
            {
                _pressed = true;
                _pressFrame = Time.frameCount;
                _pressNextFrame = false;
            }
        }

        // Hold mode: ensure the press is on. No-op if we're mid-click-cycle.
        public void MaintainHold()
        {
            _hold = true;
            if (!_pressed && !_pressNextFrame)
            {
                _pressed = true;
                _pressFrame = Time.frameCount;
            }
        }

        // Releases the continuous hold and any active press.
        public void ReleaseHold()
        {
            _hold = false;
            _pressNextFrame = false;
            if (_pressed)
            {
                _pressed = false;
                _releaseFrame = Time.frameCount;
            }
        }

        public bool ReadIsPerformed() => _pressed || _original.ReadIsPerformed();

        public bool ReadWasPerformedThisFrame() =>
            _pressFrame == Time.frameCount || _original.ReadWasPerformedThisFrame();

        public bool ReadWasCompletedThisFrame() =>
            _releaseFrame == Time.frameCount || _original.ReadWasCompletedThisFrame();

        public float ReadValue() => _pressed ? 1f : _original.ReadValue();

        public bool TryReadValue(out float value)
        {
            if (_pressed) { value = 1f; return true; }
            return _original.TryReadValue(out value);
        }
    }
}
}