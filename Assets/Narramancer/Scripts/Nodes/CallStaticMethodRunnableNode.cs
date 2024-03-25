using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class CallStaticMethodRunnableNode : AbstractDynamicMethodRunnableNode {

		protected override void Init() {
			base.Init();
			method.LookupTypes = AssemblyUtilities.GetAllStaticTypes(true, true, false).ToArray();
		}

		protected override object GetTargetObject(INodeContext context) {
			return null;
		}

	}

	public static class StaticClassWrappers {

		public static string ToHtmlStringRGB(Color color) {
			return ColorUtility.ToHtmlStringRGB(color);
		}
	}
}