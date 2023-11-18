
namespace Narramancer {
	public abstract class AbstractPropertyModifierIngredient : AbstractIngredient {

		public virtual void Initialize(PropertyInstance propertyInstance, NounInstance nounInstance) {
			// This area left blank intentionally
		}

		public virtual void OnAdded(PropertyInstance propertyInstance, NounInstance nounInstance) {
			// This area left blank intentionally
		}

		public virtual void OnRemoved(PropertyInstance propertyInstance, NounInstance nounInstance) {
			// This area left blank intentionally
		}
	}

}