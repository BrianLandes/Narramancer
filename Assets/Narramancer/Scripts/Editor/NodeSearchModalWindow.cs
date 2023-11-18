
using System;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class NodeSearchModalWindow : AbstractSearchModalWindow<Type> {

		public void ShowForValues(Vector2 position, bool allowRunnableNodes, Action<Type> onSelect) {
			var nodeTypes = AssemblyUtilities.GetAllTypes<Node>().ToArray();
			if (!allowRunnableNodes) {
				nodeTypes = nodeTypes.Where(node => !typeof(RunnableNode).IsAssignableFrom(node)).ToArray();
			}
			ShowForValues(position, nodeTypes, onSelect);
		}

		protected override string GetName(Type element) {
			return element.Name.Nicify();
		}

		protected override string GetTooltip(Type element) {
			return element.FullName.Nicify();
		}

		protected override bool ContainsAnySearchTerms(Type element, string[] searchTerms) {
			var fullName = element.FullName.ToLower();
			if (searchTerms.All(term => fullName.Contains(term))) {
				return true;
			}
			if (element.Namespace.IsNotNullOrEmpty()) {
				var @namespace = element.Namespace.ToLower();
				if (searchTerms.All(term => @namespace.Contains(term))) {
					return true;
				}
			}
			return false;
		}

	}

}