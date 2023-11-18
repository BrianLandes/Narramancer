using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {
    public static class TransformExtensions {

        public static string FullPath( this Transform @this) {
            var path = "/" + @this.name;
            var transform = @this;
            while (transform.parent != null) {
                transform = transform.parent;
                path = "/" + transform.name + path;
			}
            return path;
		}
    }
}