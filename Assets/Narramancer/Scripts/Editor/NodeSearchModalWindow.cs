
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
			var createNodeMenuAttribute = element.GetCustomAttributes(typeof(Node.CreateNodeMenuAttribute), false).FirstOrDefault(attribute => attribute is Node.CreateNodeMenuAttribute) as Node.CreateNodeMenuAttribute;
			if ( createNodeMenuAttribute != null) {
				var menuName = createNodeMenuAttribute.menuName.Replace('/', ' ').ToLower();
				if (searchTerms.All(term => menuName.Contains(term))) {
					return true;
				}
			}
			var nodeSearchTermsAttribute = element.GetCustomAttributes(typeof(NodeSearchTermsAttribute), false).FirstOrDefault(attribute => attribute is NodeSearchTermsAttribute) as NodeSearchTermsAttribute;
			if (nodeSearchTermsAttribute != null) {
				foreach( var value in nodeSearchTermsAttribute.searchTerms) {
					if (searchTerms.All(term => value.ToLower().Contains(term))) {
						return true;
					}
				}
			}
			return false;
		}

	}

}