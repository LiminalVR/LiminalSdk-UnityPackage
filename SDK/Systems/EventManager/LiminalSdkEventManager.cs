using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Shared
{
    public static class LiminalSdkEventManager
    {

        private static Queue<Action> _queuedPlatformActions = new Queue<Action>();
        private static Queue<Action> _queuedExperienceActions = new Queue<Action>();

        /// <summary>
        /// Queues an event from the experience / liminal SDK side that can be triggered by the platform when necessary
        /// </summary>
        /// <param name="evnt"></param>
        public static void QueueEventForPlatform(Action evnt)
        {
            if (!_queuedPlatformActions.Contains(evnt))
            {
                _queuedPlatformActions.Enqueue(evnt);
            }
        }

        /// <summary>
        /// Queues an event from the platform side that can be triggered by an experience when necessary
        /// </summary>
        /// <param name="evnt"></param>
        public static void QueueEventForExperience(Action evnt)
        {
            if (!_queuedExperienceActions.Contains(evnt))
            {
                _queuedExperienceActions.Enqueue(evnt);
            }
        }

        /// <summary>
        /// Dequeue and trigger the next event in the platformEvent queue.
        /// </summary>
        public static void TriggerPlatformEvent()
        {
            var action = _queuedPlatformActions.Dequeue();
            action?.Invoke();
        }

        /// <summary>
        /// Dequeue and trigger the next event in the experienceEvent queue.
        /// </summary>
        public static void TriggerExperienceEvent()
        {
            var action = _queuedExperienceActions.Dequeue();
            action?.Invoke();
        }
    }

}