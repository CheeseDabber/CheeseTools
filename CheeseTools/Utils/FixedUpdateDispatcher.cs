using System;
using System.Collections.Generic;

namespace CheeseTools.Utils {
	public static class FixedUpdateDispatcher {
		private class ScheduledAction(Action action, int ticks) {
			public Action action = action;
			public int remainingTicks = ticks;
		}

		private static List<ScheduledAction> _actions = new List<ScheduledAction>();		

		public static void FireAfterFixedUpdate(Action action) {
			_actions.Add(new ScheduledAction(action, 1));
		}

		public static void FireAfterNFixedUpdates(Action action, int n) {
			_actions.Add(new ScheduledAction(action, n));
		}

		public static void FixedUpdate() {
			for (int i = 0; i < _actions.Count; ++i) {
				var scheduledAction = _actions[i];
				scheduledAction.remainingTicks -= 1;
				if (scheduledAction.remainingTicks < 1) {
					_actions.RemoveAt(i);
					CheeseTools.instance.ModHelper.Events.Unity.FireOnNextUpdate(scheduledAction.action);
				}
			}
		}
	}
}
