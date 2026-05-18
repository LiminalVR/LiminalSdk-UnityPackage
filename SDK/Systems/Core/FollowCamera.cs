using System;
using UnityEngine;

namespace Liminal.SDK.V2
{
    public class FollowCamera: MonoBehaviour
{
    public Camera Camera;

    public float Distance;
    public float AngleThreshold = 20;
    public float Timeout;
    public float SmoothTime = 5;

    private Vector3 _posTarget;
    private Vector3 _rotTarget;
    private DateTime _lastTime;

    private void Start()
    {
        if (Camera == null)
            Camera = Camera.main;

        AlignToCameraImmediately();
    }

    private void OnEnable()
    {
        AlignToCameraImmediately();
    }

    public bool UseFixedHeight = true;
    public float FixedHeight = 1.65f;

    private void Update()
    {
        var angle = Vector3.Angle(Camera.transform.forward, transform.forward);
        var timeoutComplete = (DateTime.Now - _lastTime).TotalSeconds > Timeout;
        if (angle > AngleThreshold && timeoutComplete)
            UpdateTransform();
    }

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, _posTarget, Time.unscaledDeltaTime * SmoothTime);
        transform.eulerAngles = Quaternion.Lerp(transform.rotation, Quaternion.Euler(_rotTarget), Time.unscaledDeltaTime * SmoothTime).eulerAngles;
    }

    private void UpdateTransform()
    {
        var followVertical = Vector3.Dot(Vector3.up, Camera.transform.forward) > 0.8f;
        var cameraForward = followVertical ? Camera.transform.forward : CameraForwardXZ();
        var pos = Camera.transform.position + cameraForward * Distance;
        var lerp = Vector3.Lerp(transform.position, pos, Time.unscaledDeltaTime * SmoothTime);

        if (UseFixedHeight && !followVertical)
            pos.y = Camera.transform.position.y + FixedHeight;

        _posTarget = pos;
        _rotTarget = followVertical
            ? Camera.transform.eulerAngles
            : Quaternion.LookRotation(cameraForward).eulerAngles;

        _lastTime = DateTime.Now;
    }

    private Vector3 CameraForwardXZ()
    {
        if (Camera == null)
            return Vector3.zero;

        var dir = Camera.transform.forward;
        dir.y = 0;
        return dir.normalized;
    }

    public void AlignToCameraImmediately()
    {
        var followVertical = Vector3.Dot(Vector3.up, Camera.transform.forward) > 0.8f;
        var cameraForward = followVertical ? Camera.transform.forward : CameraForwardXZ();
        var pos = Camera.transform.position + cameraForward * Distance;

        if (UseFixedHeight && !followVertical)
            pos.y = Camera.transform.position.y + FixedHeight;

        transform.position = pos;
        transform.eulerAngles = followVertical
            ? Camera.transform.eulerAngles
            : Quaternion.LookRotation(cameraForward).eulerAngles;
        
        _posTarget = pos;
        _rotTarget = transform.eulerAngles;
    }
}
}
