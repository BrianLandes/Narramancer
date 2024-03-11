using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Narramancer {

	public class CallStaticMethodRunnableNode : AbstractDynamicMethodRunnableNode {

		protected override void Init() {
			base.Init();
			method.LookupTypes = AssemblyUtilities.GetAllStaticTypes(true, true, true).ToArray();
		}

		protected override object GetTargetObject(INodeContext context) {
			return null;
		}

	}
}