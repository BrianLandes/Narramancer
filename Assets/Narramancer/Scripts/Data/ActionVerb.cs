
using System;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[CreateAssetMenu(menuName = "Narramancer/Action Verb", fileName = "New Action Verb")]
	[RequireNode(typeof(RootNode))]
	public class ActionVerb : VerbGraph {

		#region Get Node Convenience Methods

		public bool TryGetNode(Type node, out RunnableNode resultNode) {

			resultNode = GetNode(node) as RunnableNode;
			return resultNode != null;
		}

		public RootNode GetRootNode() {
			return GetNode<RootNode>();
		}

		public RunnableNode GetFirstRunnableNode() {
			var rootNode = GetRootNode();
			if (rootNode == null) {
				return null;
			}
			return rootNode.GetNextNode();

		}

		#endregion

		#region Root and First Node

		public bool TryGetFirstRunnableNodeAfterRootNode<T>(out RunnableNode runnableNode) where T : RootNode {

			var rootNode = GetNode<T>();
			if (rootNode == null) {
				runnableNode = null;
				return false;
			}

			runnableNode = rootNode.GetNextNode();
			return runnableNode != null;

		}

		public bool TryGetFirstRunnableNode(out RunnableNode runnableNode) {

			var rootNode = GetNode<RootNode>();
			if (rootNode == null) {
				runnableNode = null;
				return false;
			}

			runnableNode = rootNode.GetNextNode();
			return runnableNode != null;

		}

		public bool TryGetFirstRunnableNodeAfterRootNode(Type T, out RunnableNode runnableNode) {

			var rootNode = GetNode(T) as RootNode;
			if (rootNode == null) {
				runnableNode = null;
				return false;
			}
			runnableNode = rootNode.GetNextNode();
			return runnableNode != null;

		}

		public bool TryGetFirstRunnableNodeAfterRootNode(out RunnableNode runnableNode) {

			var rootNode = GetNode<RootNode>();
			if (rootNode == null) {
				runnableNode = null;
				return false;
			}

			runnableNode = rootNode.GetNextNode();
			return runnableNode != null;

		}

		#endregion

		#region Clean and Correct Methods

		public bool HasAnyNullNodes() {
			return nodes.Any(x => x == null);
		}

		public void RemoveNullNodes() {
			nodes.RemoveAll(x => x == null); //Remove null items
		}

		#endregion

	}
}