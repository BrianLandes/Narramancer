using System.Linq;

namespace Narramancer {

	public class CallStaticMethodRunnableNode : AbstractDynamicMethodRunnableNode {

		protected override void Init() {
			base.Init();
			method.LookupTypes = AssemblyUtilities.GetAllStaticTypes(true, true, true).ToArray();
		}

		protected override object GetTargetObject(object context) {
			return null;
		}

	}
}