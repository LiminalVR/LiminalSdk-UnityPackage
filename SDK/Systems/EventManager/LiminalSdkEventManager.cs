using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Liminal.SDK.EventManager
{
    /// <summary>
    /// Manages event registration and execution between the Platform and the SDK (or vice versa).  
    /// This allows events to be pre-registered before starting an experience and triggered when needed.  
    /// Example: The Platform can queue up events that the experience will trigger at the right moment.
    /// </summary>
    public static class LiminalSdkEventManager
    {

        private static List<LiminalSdkActionEventData> _platformActionDataList = new List<LiminalSdkActionEventData>();
        private static List<LiminalSdkActionEventData> _experienceActionDataList = new List<LiminalSdkActionEventData>();

        /// <summary>
        /// Add actionEvent to list from the experience / liminal SDK side that can be triggered by the platform when necessary
        /// </summary>
        /// <param name="eventData">EventData to be added to list</param>
        public static void AddActionEventForPlatform(LiminalSdkActionEventData eventData)
        {
            _platformActionDataList.Add(eventData);
        }

        /// <summary>
        /// Add actionEvent to list from platform side that can be triggered by an experience when necessary
        /// </summary>
        /// <param name="eventData">EventData to be added to list</param>
        public static void AddActionEventForExperience(LiminalSdkActionEventData eventData)
        {
            _experienceActionDataList.Add(eventData);
        }

        /// <summary>
        /// Triggers the first actionEvent in the platformActionEvent list
        /// </summary>
        /// <param name="removeEventAfterInvoking">If true, removes event after it has been invoked</param>
        /// <returns></returns>
        public static IEnumerator TriggerFirstPlatformActionEventRoutine(bool removeEventAfterInvoking = true)
        {
            yield return TriggerFirstActionEventRoutine(_platformActionDataList);
        }

        /// <summary>
        /// Triggers the first actionEvent in the experienceActionEvent list
        /// </summary>
        /// <param name="removeEventAfterInvoking">If true, removes event after it has been invoked</param>
        /// <returns></returns>
        public static IEnumerator TriggerFirstExperienceActionEventRoutine(bool removeEventAfterInvoking = true)
        {
            yield return TriggerFirstActionEventRoutine(_experienceActionDataList);
        }


        private static IEnumerator TriggerFirstActionEventRoutine(List<LiminalSdkActionEventData> list, bool removeEventAfterInvoking = true)
        {
            var eventData = list.FirstOrDefault();
            yield return TriggerEventRoutine(eventData);
            if (removeEventAfterInvoking)
            {
                list.Remove(eventData);
            }
        }

        /// <summary>
        /// Triggers all actionEvents within the platformActionEventList with matching Id
        /// </summary>
        /// <param name="id">Id of the actionEvent</param>
        /// <param name="removeEventAfterInvoking">If true, removes event after it has been invoked</param>
        /// <returns></returns>
        public static IEnumerator TriggerAllPlatformActionEventsWithIdRoutine(string id, bool removeEventAfterInvoking = true)
        {
            yield return TriggerAllActionEventsWithIdRoutine(id, _platformActionDataList, removeEventAfterInvoking);
        }

        /// <summary>
        /// Triggers all actionEvents within the experienceActionEventList with matching Id
        /// </summary>
        /// <param name="id">Id of the actionEvent</param>
        /// <param name="removeEventAfterInvoking">If true, removes event after it has been invoked</param>
        /// <returns></returns>
        public static IEnumerator TriggerAllExperienceActionEventsWithIdRoutine(string id, bool removeEventAfterInvoking = true)
        {
            yield return TriggerAllActionEventsWithIdRoutine(id, _experienceActionDataList, removeEventAfterInvoking);
        }

        private static IEnumerator TriggerAllActionEventsWithIdRoutine(string id, List<LiminalSdkActionEventData> list, bool removeEventAfterInvoking)
        {
            // Get all actionEvents with matching id, trigger each event and then remove from list
            var matchingEvents = list.FindAll(x => x.Id.Equals(id));

            foreach (var eventData in matchingEvents)
            {
                yield return TriggerEventRoutine(eventData);
            }

            if (removeEventAfterInvoking)
            {
                matchingEvents.ForEach(eventData => list.Remove(eventData));
            }
        }

        /// <summary>
        /// Remove all ActionEventData from platformActionEventList with matching id
        /// </summary>
        /// <param name="id">Id of the ActionEventData</param>
        public static void RemoveAllPlatformEventDataWithId(string id)
        {
            RemoveAllEventDataWithId(id, _platformActionDataList);
        }

        /// <summary>
        /// Remove all ActionEventData from experienceActionEventList with matching id
        /// </summary>
        /// <param name="id">Id of the ActionEventData</param>
        public static void RemoveAllExperienceEventDataWithId(string id)
        {
            RemoveAllEventDataWithId(id, _experienceActionDataList);
        }

        private static void RemoveAllEventDataWithId(string id, List<LiminalSdkActionEventData> list)
        {
            list.RemoveAll(x => x.Id.Equals(id));
        }

        private static IEnumerator TriggerEventRoutine(LiminalSdkActionEventData eventData)
        {
            // Get all actionEvents with matching id, trigger each event and then remove from list
            yield return new WaitForSeconds(eventData.Delay);
            eventData.Action?.Invoke();
        }

        
    }

}