using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.SDK.EventManager
{
    public class LiminalSdkActionEventData
    {
        public string Id;
        public float Delay;
        public Action Action;

        public LiminalSdkActionEventData(Action action)
            : this(Guid.NewGuid().ToString(), action) { }

        public LiminalSdkActionEventData(string id, Action action, float delay = 0)
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
