using BepInEx.Logging;
using System;
using UnityEngine;

namespace BlackFlashCrit {
	public static class Log {
		public static ManualLogSource LogSource { get; set; }

		public static void Info (string msg) => LogSource?.LogInfo(msg);
		public static void Warn (string msg) => LogSource?.LogWarning(msg);
		public static void Error (string msg) => LogSource?.LogError(msg);

		// Debounced logging
		public class Debounced<T> {
			private readonly Action<T> _logAction;
			private readonly float _debounceSeconds;
			private float _lastChangeTime;
			private bool _dirty;
			private T _pendingValue;

			public Debounced (Action<T> logAction, float debounceSeconds) {
				_logAction = logAction;
				_debounceSeconds = debounceSeconds;
			}

			public void Set (T value) {
				_pendingValue = value;
				_dirty = true;
				_lastChangeTime = Time.realtimeSinceStartup;
			}

			public void Update () {
				if (_dirty && Time.realtimeSinceStartup - _lastChangeTime >= _debounceSeconds) {
					_logAction(_pendingValue);
					_dirty = false;
				}
			}
		}
	}
}