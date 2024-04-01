using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
    public static class TransformExtensions {

        public static string FullPath(this Transform @this) {
            var path = "/" + @this.name;
            var transform = @this;
            while (transform.parent != null) {
                transform = transform.parent;
                path = "/" + transform.name + path;
            }
            return path;
        }

        public static int IndexOfComponent(this Transform @this, Component component) {
            var allComponents = @this.GetComponents<Component>().ToList();
            return allComponents.IndexOf(component);
        }
    }
}