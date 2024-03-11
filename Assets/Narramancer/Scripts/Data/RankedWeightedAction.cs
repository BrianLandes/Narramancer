
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateAssetMenu(menuName = "Narramancer/Ranked Weighted Action", fileName = "New Ranked Weighted Action", order = 12)]
	public class RankedWeightedAction : ScriptableObject {

		#region Rank

		[SerializeField]
		ToggleableInt staticRank = new ToggleableInt(1);

		[SerializeField]
		[VerbRequired]
		[RequireInput(typeof(NounInstance), "instance")]
		[RequireOutput(typeof(int), "rank")]
		private ValueVerb rankGraph = default;

		public int GetRank(INodeContext context, NounInstance instance) {
			if (staticRank.activated) {
				return staticRank.value;
			}
			var rank = rankGraph.RunForValue<int, NounInstance>(context, instance);
			return rank;
		}

		#endregion

		#region Weight

		[SerializeField]
		ToggleableFloat staticWeight = new ToggleableFloat(1f);

		[SerializeField]
		[VerbRequired]
		[RequireInput(typeof(NounInstance), "instance")]
		[RequireOutput(typeof(float), "weight")]
		private ValueVerb weightGraph = default;

		public float GetWeight(INodeContext context, NounInstance instance) {
			if (staticWeight.activated) {
				return staticWeight.value;
			}
			var weight = weightGraph.RunForValue<float, NounInstance>(context, instance);
			return weight;
		}


		#endregion

		#region Effect

		[SerializeField]
		[VerbRequired]
		[RequireInput(typeof(NounInstance), "instance")]
		private ActionVerb effectGraph = default;

		public ActionVerb GetEffectGraph() {
			return effectGraph;
		}

		#endregion

		#region Choose

		public static RankedWeightedAction Choose(INodeContext context, NounInstance instance, IList<RankedWeightedAction> actions, ref string log) {
			if (!actions.Any()) {
				return null;
			}

			var stringBuilder = new StringBuilder();

			var rankings = new Dictionary<RankedWeightedAction, int>();

			foreach (var action in actions) {
				var ranking = action.GetRank(context, instance);
				stringBuilder.AppendLine($"Rank of {action.name} was {ranking}");
				rankings[action] = ranking;
			}

			var highestRank = rankings.Values.OrderByDescending(rank => rank).FirstOrDefault();
			stringBuilder.AppendLine($"=> Highest Rank: {highestRank}");

			var highestRankingActions = rankings.Where(pair => pair.Value >= highestRank).Select(pair => pair.Key).ToList();

			var weights = new Dictionary<RankedWeightedAction, float>();

			foreach (var action in highestRankingActions) {
				var weight = action.GetWeight(context, instance);
				stringBuilder.AppendLine($"Weight of {action.name} was {weight}");

				if (weight > 0f) {
					weights[action] = weight;
				}
			}

			// TODO: eliminate small weights (anything lower than %10 of highest remaining weight)

			var chosenAction = Probabilititties.ChooseOneWeighted(weights);

			stringBuilder.AppendLine($"Choose {chosenAction.name}, Weight: {weights[chosenAction]}, Rank: {rankings[chosenAction]}");

			log = stringBuilder.ToString();

			return chosenAction;

		}


		#endregion

		#region References

		[SerializeField]
		private ReferenceList references = new ReferenceList();

		#endregion
	}
}