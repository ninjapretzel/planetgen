using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Util {

	public static class Res {

		public static T SafeLoad<T>(string path, string safe) where T : UnityEngine.Object {
			T loaded = Resources.Load<T>(path);
			if (loaded != null) {
				return loaded;
			}
			Debug.LogWarning($"SafeLoad<{typeof(T)}>: Failed to load {path}.");
			return Resources.Load<T>(safe);
		}
		public static T SafeLoad<T>(string path, T safe) where T : UnityEngine.Object {
			T loaded = Resources.Load<T>(path);
			if (loaded != null) {
				return loaded;
			}
			Debug.LogWarning($"SafeLoad<{typeof(T)}>: Failed to load {path}.");
			return safe;
		}


	}
}
