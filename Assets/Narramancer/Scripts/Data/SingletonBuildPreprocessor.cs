#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Narramancer {

	public class SingletonBuildPreprocessor : IPreprocessBuildWithReport {
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report) {
			var singletons = Resources.LoadAll<SingletonBase>(string.Empty);

			foreach( var singleton in singletons) {
				singleton.OnPreprocessBuild();
			}
		}
	}
}
#endif