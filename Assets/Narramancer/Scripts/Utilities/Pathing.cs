using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Narramancer {
	public static class Pathing {


		public static bool FindPath(INodeContext context, NounInstance fromLocation, NounInstance toLocation, VerbGraph getAccessableLocations, out NounInstance nextLocation) {

			nextLocation = fromLocation;

			List<NounInstance> GetAccessableLocations(NounInstance location) {
				return getAccessableLocations.RunForValueList<NounInstance, NounInstance>(context, location);
			}

			if (fromLocation == toLocation) {
				nextLocation = toLocation;
				return true;
			}
			else
			if (GetAccessableLocations(fromLocation).Contains(toLocation)) {
				nextLocation = toLocation;
				return true;
			}


			var openList = new List<NounInstance>();

			var closedList = new List<NounInstance>();

			var costTable = new Dictionary<NounInstance, int>();

			int GetCost(NounInstance location) {
				if (costTable.TryGetValue(location, out var cost)) {
					return cost;
				}
				return 0;
			}

			var fromTable = new Dictionary<NounInstance, NounInstance>();

			openList.Add(fromLocation);

			while (openList.Any()) {

				var nextNode = openList.OrderBy(node => GetCost(node)).First();

				openList.Remove(nextNode);
				closedList.Add(nextNode);

				if (nextNode == toLocation) {

					break;
				}

				int cost = GetCost(nextNode);

				foreach (var accessibleLocation in GetAccessableLocations(nextNode)) {

					if (openList.Contains(accessibleLocation) || closedList.Contains(accessibleLocation)) {
						// update cost if its lower
						var nextCost = GetCost(accessibleLocation);
						if (nextCost > cost + 1) {
							costTable[accessibleLocation] = cost + 1;
							fromTable[accessibleLocation] = nextNode;
						}
					}
					else {

						costTable[accessibleLocation] = cost + 1;
						fromTable[accessibleLocation] = nextNode;
						openList.Add(accessibleLocation);
					}

				}
			}


			if (closedList.Contains(toLocation)) {

				var path = new List<NounInstance>();
				var currentNode = toLocation;

				while (!path.Contains(fromLocation)) {
					currentNode = fromTable[currentNode];
					path.Prepend(currentNode);
				}

				nextLocation = path.Find(node => node != fromLocation);
				return true;
			}

			return false;
		}

	}
}