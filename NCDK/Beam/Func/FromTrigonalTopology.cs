using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Given a chemical graph with atom-centric double bond stereo configurations
    /// (trigonal topology) - remove the topology but add in direction up/down edge
    /// labels.
    /// </summary>
    // @author John May
    internal sealed class FromTrigonalTopology : AbstractFunction<Graph, Graph>
    {

        public override Graph Apply(Graph g)
        {
            Graph h = new Graph(g.Order);

            // copy atom/topology information this is unchanged
            for (int u = 0; u < g.Order; u++)
            {
                if (g.TopologyOf(u).Type == Configuration.Types.DoubleBond)
                {
                    h.AddAtom(ReducedAtom(g, u));
                }
                else
                {
                    h.AddAtom(g.GetAtom(u));
                    h.AddTopology(g.TopologyOf(u));
                }
            }

            IDictionary<Edge, Edge> replacements = new Traversal(g).replacement;


            // append the edges, replacing any which need to be changed
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u)
                    {
                        Edge ee = e;
                        Edge replacement;
                        if (replacements.TryGetValue(e, out replacement))
                            ee = replacement;
                        h.AddEdge(ee);
                    }
                }
            }

            return h;
        }

        private Atom ReducedAtom(Graph g, int u)
        {
            Atom a = g.GetAtom(u);

            int sum = 0;
            foreach (var e in g.GetEdges(u))
            {
                sum += e.Bond.Order;
            }

            return ToSubsetAtoms.ToSubset(g.GetAtom(u), g, u);
        }

        private sealed class Traversal
        {
            private readonly Graph g;
            private readonly bool[] visited;
            private readonly int[] ordering;
            public IDictionary<Edge, Edge> replacement = new Dictionary<Edge, Edge>();

            private static readonly Bond[] labels = new Bond[] { Bond.Down, Bond.Up };

            public Traversal(Graph g)
            {
                this.g = g;
                this.visited = new bool[g.Order];
                this.ordering = new int[g.Order];

                for (int u = 0; u < g.Order; u++)
                {
                    if (!visited[u])
                        Visit(u, u);
                }
            }

            private void Visit(int p, int u)
            {

                visited[u] = true;

                // offset - the index of the edge with a double bond label
                int offset = -1;

                IList<Edge> es = g.GetEdges(u);
                for (int i = 0; i < es.Count; i++)
                {
                    Edge e = es[i];
                    int v = e.Other(u);
                    if (!visited[v])
                        Visit(u, v);
                    ordering[v] = 2 + i;
                    if (e.Bond == Bond.Double)
                        offset = i;
                }

                ordering[p] = 0;
                ordering[u] = 1;


                Topology t = g.TopologyOf(u);

                if (t.Type == Configuration.Types.DoubleBond)
                {

                    if (offset < 0)
                        throw new ArgumentException("found atom-centric double bond" +
                                                                   "specifiation but no double bond label.");

                    // Order the topology to ensure it matches the traversal Order
                    Topology topology = t.OrderBy(ordering);

                    // labelling start depends on configuration ...
                    int j = topology.Configuration
                                    .Shorthand == Configuration.AntiClockwise ? 0
                                                                                 : 1;

                    // ... and which end of the double bond we're looking from
                    if (ordering[es[offset].Other(u)] < ordering[u])
                    {

                    }
                    else if (es.Count == 2 &&
                          ordering[u] < ordering[es[(offset + 1) % es.Count]
                                                   .Other(u)])
                    {
                        j++;
                    }

                    // now create the new labels for the non-double bond atoms
                    for (int i = 1; i < es.Count; i++)
                    {
                        Edge e = es[(offset + i) % es.Count];
                        Bond label = labels[j++ % 2];

                        Edge f = new Edge(u,
                                          e.Other(u),
                                          label);
                        Edge existing;
                        if (replacement.TryGetValue(e, out existing))
                        {
                            // check for conflict - need to rewrite existing labels
                            if (existing.GetBond(u) != label)
                            {
                                BitArray visited = new BitArray(g.Order);
                                visited.Set(u, true);
                                InvertExistingDirectionalLabels(visited, e.Other(u));
                            }
                        }
                        replacement[e] = f;
                    }
                }
            }

            private void InvertExistingDirectionalLabels(BitArray visited, int u)
            {
                visited.Set(u, true);
                if (g.TopologyOf(u) == null)
                    return;
                foreach (var e in g.GetEdges(u))
                {
                    int v = e.Other(u);
                    if (!visited[v])
                    {
                        Edge f;
                        if (replacement.TryGetValue(e, out f))
                        {
                            replacement[e] = f.Inverse();
                        }
                        InvertExistingDirectionalLabels(visited, v);
                    }
                }
            }
        }
    }
}
