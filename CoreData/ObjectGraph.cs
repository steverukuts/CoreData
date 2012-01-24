using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CoreData
{
    /// <summary>
    /// Provides methods to interrogate an object graph.
    /// </summary>
    public class ObjectGraph
    {
        /// <summary>
        /// This structure allows us to easily determine backward references.
        /// </summary>
        class GraphNode
        {
            public object Object { get; set; }

            public object BackReference { get; set; }
        }

        /// <summary>
        /// The types that will be ignored when walking an object graph.
        /// </summary>
        public List<Type> IgnoredTypes { get; set; }

        /// <summary>
        /// Should the collapser walk reference types (structs)?
        /// </summary>
        public bool IncludeReferenceTypes { get; set; }

        /// <summary>
        /// Include strings? Remember, strings are enumerable types, so if <see cref="IncludeReferenceTypes"/>
        /// is enabled, you will get a list of chars with your object graph. Defaults to false.
        /// </summary>
        public bool IncludeStrings { get; set; }

        /// <summary>
        /// Ignore the root when walking the object graph? The root object is the object you pass to the
        /// constructor.
        /// </summary>
        public bool IgnoreRoot { get; set; }

        /// <summary>
        /// The object graph that this instance represents. This is the object instance passed to the 
        /// constructor.
        /// </summary>
        public object Graph { get; private set; }

        public ObjectGraph(object graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException("graph");
            }

            this.IgnoredTypes = new List<Type>();
            this.Graph = graph;
        }

        /// <summary>
        /// Recursively collapses the object graph and returns a list of every object involved, in no particular
        /// order.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<object> Collapse()
        {
            return this.WalkNode(this.Graph, null, null).Select(node => node.Object);
        }

        /// <summary>
        /// Returns true if the given object exists in the ObjectGraph.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool ContainsNode(object node)
        {
            return this.Collapse().Any(collapsedNode => Object.ReferenceEquals(collapsedNode, node));
        }

        /// <summary>
        /// When we encounter a property with a [BackReference] attribute, we need to trawl through the 
        /// object graph to see if we can find the object that this refers to.
        /// </summary>
        /// <param name="searchNode"></param>
        /// <returns></returns>
        public object ResolveBackReference(object searchNode)
        {
            IEnumerable<GraphNode> foundNodes = this.WalkNode(this.Graph, null, null)
                .Where(node => ReferenceEquals(searchNode, node.Object)).ToList();

            if (foundNodes.Count() == 0)
            {
                throw new ArgumentException("Could not find the given node in the graph", "searchNode");
            }
            else if (foundNodes.Count() == 1)
            {
                return foundNodes.First().BackReference;
            }
            else
            {
                throw new ArgumentException("Found more than one matching node in the graph", "searchNode");
            }
        }

        /// <summary>
        /// Recursively walks through the objects in the graph and retrieves a list of unique nodes.
        /// </summary>
        /// <param name="node">The current node that we are investigating</param>
        /// <param name="parent">The parent of the current node, or null if this is the root node</param>
        /// <param name="seenObjects">A list of objects that have already been seen to prevent infinite recursion</param>
        /// <returns></returns>
        private IEnumerable<GraphNode> WalkNode(object node, object parent, HashSet<object> seenObjects)
        {
            HashSet<GraphNode> output = new HashSet<GraphNode>();
            bool rootIteration = false;

            if (seenObjects == null)
            {
                rootIteration = true;
                seenObjects = new HashSet<object>();
            }

            Type nodeType = node.GetType();
            IEnumerable values;

            if (node is IEnumerable)
            {
                values = (IEnumerable)node;
            }
            else
            {
                values = from property in nodeType.GetProperties()
                         where !property.GetGetMethod().IsStatic
                         select property.GetValue(node, null);

                if (!rootIteration || !this.IgnoreRoot)
                {
                    seenObjects.Add(node);
                    output.Add(new GraphNode{Object = node, BackReference = parent});
                }
            }

            foreach (object value in values)
            {
                if (value == null || seenObjects.Contains(value))
                {
                    continue;
                }

                Type valueType = value.GetType();

                if (IgnoredTypes.Contains(valueType) || (!this.IncludeReferenceTypes && valueType.IsValueType))
                {
                    continue;
                }

                object childNodeParent = (node is IEnumerable) ? parent : node;
                
                foreach (GraphNode childNode in WalkNode(value, childNodeParent, seenObjects))
                {
                    seenObjects.Add(childNode);
                    output.Add(childNode);
                }
            }

            return output;
        }
    }
}
