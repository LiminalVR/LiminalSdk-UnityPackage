using Liminal.SDK.V2;
using UnityEngine;
using UnityEngine.Playables;

public class XRInputTimelineManager : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public float HandTrackingInitialTime = 0;

    private void Start()
    {
        if(LiminalControllerManager.IsHandTracking)
        {
            playableDirector.initialTime = HandTrackingInitialTime;
        }
    }
}
