using System.Linq;
using UnityEditor;

namespace Narramancer {

	public class VerbSearchModalWindow : AbstractSearchModalWindow<VerbGraph> {

		protected override string GetName(VerbGraph element) {
			var mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(element));
			if (mainAsset != null && mainAsset != element) {
				return $"{element.name}  ({mainAsset.name})";
			}
			return element.name;
		}

		protected override string GetTooltip(VerbGraph element) {
			return AssetDatabase.GetAssetPath(element);
		}

		protected override bool ContainsAnySearchTerms(VerbGraph element, string[] searchTerms) {
			var text = $"{element.name} {AssetDatabase.GetAssetPath(element)}".ToLower();
			return searchTerms.All(term => text.Contains(term));
		}
	}

}
