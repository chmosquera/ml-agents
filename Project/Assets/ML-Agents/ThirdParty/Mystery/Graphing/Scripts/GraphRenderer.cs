using UnityEngine;
using System.Collections;
using Mystery.Graphing;
using System.Collections.Generic;

namespace Mystery.Graphing
{
    [AddComponentMenu("Squiggle/Graph Renderer")]
    public class GraphRenderer : SRPGraphRenderer
    {
        //TODO: Make Graph Console Asset? (needs to be Serializable)
        public GraphConsole GraphConsole;

        protected override IGraphConsole LoadGraph()
        {
            return GraphConsole;
        }
    }
}