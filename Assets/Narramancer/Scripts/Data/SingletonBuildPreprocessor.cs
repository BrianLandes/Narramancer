#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Narramancer {

	public class SingletonBuildPreprocessor : IPreprocessBuildWithReport {
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report) {

			EditorUtility.DisplayDialog("Upgrade Narramancer", "Please upgrade to the full version of Narramancer in order to build a release.", "OK");

			Application.OpenURL("https://assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301");

			Debug.LogException(new BuildFailedException("Please upgrade to the full version of Narramancer (https://assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301) in order to build a release."));

			var singletons = Resources.LoadAll<SingletonBase>(string.Empty);

			foreach( var singleton in singletons) {
				singleton.OnPreprocessBuild();
			}
		}
	}
}
#endif