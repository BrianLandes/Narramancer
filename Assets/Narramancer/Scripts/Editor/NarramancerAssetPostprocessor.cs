using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	public class NarramancerAssetPostprocessor : AssetPostprocessor {

		private static void OnPostprocessAllAssets(
				string[] importedAssets,
				string[] deletedAssets,
				string[] movedAssets,
				string[] movedFromAssetPaths) {

			var narramancer = Resources.LoadAll<NarramancerSingleton>(string.Empty).FirstOrDefault();
			if (narramancer == null) {
				narramancer = NarramancerSingletonEditor.CreateSingletonInResources();
			}
		}
	}
}