using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Liminal.Shared
{
    public static class LiminalSdkEventManager
    {

        private static List<ActionEventData> _platformActionDataList = new List<ActionEventData>();
        private static List<ActionEventData> _experienceActionDataList = new List<ActionEventData>();

        /// <summary>
        /// Add actionEvent to list from the experience / liminal SDK side that can be triggered by the platform when necessary
        /// </summary>
        /// <param name="eventData"></param>
        public static void AddActionEventForPlatform(ActionEventData eventData)
        {
            _platformActionDataList.Add(eventData);
        }

        /// <summary>
        /// Add actionEvent to list from platform side that can be triggered by an experience when necessary
        /// </summary>
        /// <param name="evnt"></param>
        public static void AddActionEventForExperience(ActionEventData eventData)
        {
            _experienceActionDataList.Add(eventData);
        }

        /// <summary>
        /// Triggers the first actionEvent in the platformActionEvent list
        /// </summary>
        /// <returns></returns>
        public static IEnumerator TriggerFirstPlatformActionEvent()
        {
            yield return TriggerFirstActionEvent(_platformActionDataList);
        }

        /// <summary>
        /// Triggers the first actionEvent in the experienceActionEvent list
        /// </summary>
        /// <returns></returns>
        public static IEnumerator TriggerFirstExperienceActionEvent()
        {
            yield return TriggerFirstActionEvent(_experienceActionDataList);
        }


        private static IEnumerator TriggerFirstActionEvent(List<ActionEventData> list)
        {
            var eventData = list.FirstOrDefault();
            yield return TriggerEvent(eventData);
            list.Remove(eventData);
        }

        /// <summary>
        /// Triggers all actionEvents within the platformActionEventList with matching Id
        /// </summary>
        /// <param name="id">Id of the actionEvent</param>
        /// <returns></returns>
        public static IEnumerator TriggerAllPlatformActionEventsWithId(string id)
        {
            yield return TriggerAllActionEventsWithId(id, _platformActionDataList);
        }

        /// <summary>
        /// Triggers all actionEvents within the experienceActionEventList with matching Id
        /// </summary>
        /// <param name="id">Id of the actionEvent</param>
        /// <returns></returns>
        public static IEnumerator TriggerAllExperienceActionEventsWithId(string id)
        {
            yield return TriggerAllActionEventsWithId(id, _experienceActionDataList);
        }

        private static IEnumerator TriggerAllActionEventsWithId(string id, List<ActionEventData> list)
        {
            // Get all actionEvents with matching id, trigger each event and then remove from list
            var matchingEvents = list.FindAll(x => x.Id.Equals(id));

            foreach (var eventData in matchingEvents)
            {
                yield return TriggerEvent(eventData);
            }

            matchingEvents.ForEach(eventData => list.Remove(eventData));
        }

        private static IEnumerator TriggerEvent(ActionEventData eventData)
        {
            // Get all actionEvents with matching id, trigger each event and then remove from list
            yield return new WaitForSeconds(eventData.Delay);
            eventData.Action?.Invoke();
        }

        public class ActionEventData
        {
            public string Id;
            public float Delay;
            public Action Action;

            public ActionEventData(Action action)
                : this(Guid.NewGuid().ToString(), action) { }

            public ActionEventData(string id, Action action, float delay = 0)
            {
                Id = id;
                Action = action;
                Delay = delay;
            }

            public void Trigger()
            {
                Action?.Invoke();
            }
        }
    }

}