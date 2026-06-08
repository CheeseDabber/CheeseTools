using System;
using System.Reflection;
using UnityEngine;

namespace CheeseTools {
    public static class CheeseExtensions {
        public static void ForceExit(this EntrywayTrigger entryTrigger, GameObject obj) {
            var field = entryTrigger.GetType().GetField("OnExit", BindingFlags.Instance | BindingFlags.NonPublic);
            var del = (MulticastDelegate)field.GetValue(entryTrigger);
            ((EntrywayTrigger.EntrywayEvent)del).Invoke(obj);
        }
    }
}
