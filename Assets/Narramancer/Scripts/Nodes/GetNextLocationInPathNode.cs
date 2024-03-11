using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;

namespace Narramancer {

	public class GetNextLocationInPathNode : Node {

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override)]
		private NounInstance fromLocation = default;

		[SerializeField]
		[VerbRequired, HideLabelInNode]
		[RequireInput(typeof(NounInstance), "element")]
		[RequireOutput(typeof(List<NounInstance>), "element")]
		private ValueVerb getAccessableLocations = default;

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override)]
		private NounInstance toLocation = default;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple)]
		private bool pathExists = false;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple)]
		private NounInstance selectedLocation = default;


		private bool FindPath(INodeContext context, out NounInstance nextLocation) {
			var fromLocation = GetInputValue(context, nameof(this.fromLocation), this.fromLocation);
			Assert.IsNotNull(fromLocation);

			var toLocation = GetInputValue(context, nameof(this.toLocation), this.toLocation);
			Assert.IsNotNull(toLocation);

			return Pathing.FindPath(context, fromLocation, toLocation, getAccessableLocations, out nextLocation);
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(nameof(pathExists))) {
					return FindPath(context, out _);
				}
				else
				if (port.fieldName.Equals(nameof(selectedLocation))) {

					if (FindPath(context, out var nextLocation)) {
						return nextLocation;
					}
				}
			}
			return null;
		}
	}
}