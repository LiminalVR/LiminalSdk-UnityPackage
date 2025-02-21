using System.Collections;
using System.Collections.Generic;
using Liminal.Shared;
using UnityEngine;

public class LiminalSdkActionEventExample : MonoBehaviour
{

    #region Example 1 - Platform Side Register & Experince side trigger

    /// <summary>
    /// In this example, we will add an event that will be send the user to the login page from within an experience after a delay
    /// </summary>
    public class PlatformExample1
    {
        public void Start()
        {
            var goToLoginEventData = new LiminalSdkEventManager.ActionEventData("GoToLoginEvent", GoToLogin, 2);
            LiminalSdkEventManager.AddActionEventForExperience(goToLoginEventData);
        }

        public void GoToLogin()
        {
            // Code that would send the user to the login section of the platform
        }
    }

    public class ExperienceExample1 : MonoBehaviour
    {
        public void DuringExperience()
        {
            // Start a coroutine to trigger all events with the matching id
            StartCoroutine(LiminalSdkEventManager.TriggerAllExperienceActionEventsWithId("GoToLoginEvent"));
        }
    }

    #endregion

}
