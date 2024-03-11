using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer
{
    /// <summary> Add terms or descriptions that will cause this node to show up in the NodeSearchModalWindow</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeSearchTermsAttribute : Attribute {
        public string[] searchTerms;

        public NodeSearchTermsAttribute(params string[] searchTerms) {
            this.searchTerms = searchTerms;
        }

    }
}
