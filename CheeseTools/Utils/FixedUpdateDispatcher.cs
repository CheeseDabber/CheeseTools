using System;
using System.Collections.Generic;

namespace CheeseTools.Utils {
	public static class FixedUpdateDispatcher {
		private static Queue<Action> _actions = new Queue<Action>();		

		public static void FireAfterFixedUpdate(Action action) {
			_actions.Enqueue(action);
		}

		public static void FixedUpdate() {
			while (_actions.Count > 0)
				CheeseTools.instance.ModHelper.Events.Unity.FireOnNextUpdate(_actions.Dequeue());
		}
	}
}
