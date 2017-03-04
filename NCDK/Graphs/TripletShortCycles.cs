/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */
using System;
using System.Collections.Generic;
using NCDK.Common.Collections;

namespace NCDK.Graphs
{
    /// <summary>
    /// Compute the shortest cycles through each vertex triple. This allows one to
    /// directly obtain the envelope rings of bicyclic fused system. These cycles
    /// can be thought of as the 'ESSSR' (extended smallest set of smallest rings)
    /// and 'envelope' rings as used by PubChem fingerprints (CACTVS Substructure
    /// Keys). The PubChem fingerprint documentation exclusively refers to the ESSSR
    /// and envelopes as just the ESSSR and the rest of this documentation does the
    /// same. This class provides the cycles (vertex paths) for each ring in the
    /// ESSSR.
    /// </summary>
    /// <remarks> 
    /// <para>
    /// The ESSSR should not be confused with the extended set of smallest rings
    /// (ESSR) {@cdk.cite Downs89}.
    /// </para>
    /// <h4>Algorithm</h4> 
    /// <para>
    /// To our knowledge no algorithm has been published for
    /// the ESSSR. The <a href="ftp://ftp.ncbi.nlm.nih.gov/pubchem/specifications/pubchem_fingerprints.pdf">PubChem Specifications</a>
    /// states - <i>"An ESSSR ring is any ring which does not
    /// share three consecutive atoms with any other ring in the chemical structure.
    /// For example, naphthalene has three ESSSR rings (two phenyl fragments and the
    /// 10-membered envelope), while biphenyl will yield a count of only two ESSSR
    /// rings"</i>. The name implies the use of the smallest set of smallest rings
    /// (SSSR). Not every graph has an SSSR and so the minimum cycle basis is used
    /// instead. With this modification the algorithm is outlined below. 
    /// </para>
    /// <list type="bullet">
    /// <item>Compute a minimum cycle basis (or SSSR) of the graph (may not be unique)</item> 
    /// <item>For each vertex <i>v</i> and two adjacent vertices (<i>u</i> and <i>w</i>) check if the path <i>-u-v-w-</i> belongs to any cycles already in the basis</item> 
    /// <item>If no such cycle can be found compute the shortest cycle which travels through <i>-u-v-w-</i> and add it to the basis. The shortest cycle is the shortest path from <i>u</i> to <i>w</i> which does not travel through <i>v</i></item>
    /// </list>
    /// <para>
    /// In the case of <i>naphthalene</i> the
    /// minimum cycle basis is the two phenyl rings. Taking either bridgehead atom of
    /// <i>naphthalene</i> to be <i>v</i> and choosing <i>u</i> and <i>w</i> to be in
    /// different phenyl rings it is easy to see the shortest cycle through
    /// <i>-u-v-w-</i> is the 10 member envelope ring.
    /// </para>
    /// <h4>Canonical and Non-Canonical Generation</h4>
    /// <para>
    /// The algorithm can generate a canonical or non-canonical (preferred) set of
    /// cycles. As one can see from the above description depending on the order we
    /// check each triple (-u-v-w-) and add it to basis we may end up with a
    /// different set.
    /// </para>
    /// <para>
    /// To avoid this PubChem fingerprints uses a canonical labelling ensuring the
    /// vertices are always checked in the same order. The vertex order used by this
    /// class is the natural order of the vertices as provided in the graph. To
    /// ensure the generated set is always the same vertices should be ordered
    /// beforehand or the non-canonical option should be used.
    /// </para>
    /// <para>
    /// Although this canonical sorting allows one to reliable generate the same set
    /// of cycles for a graph this is not true for subgraphs. For two graphs
    /// <i>G</i>, <i>H</i> and a canonical ordering (<i>π</i>). If <i>H</i> is a
    /// subgraph of <i>G</i> then for two vertices <i>u</i>, <i>v</i>. It follows
    /// that <i>π(u)</i> &lt; <i>π(v)</i> ∈ <i>H</i> ⇏ <i>π(u)</i> &lt; <i>π(v)</i> ∈
    /// <i>G</i>. In other words, we can canonically label a graph and inspect the
    /// ordering of vertices <i>u</i> and <i>v</i>. We now take a subgraph which
    /// contains both <i>u</i> and <i>v</i> - the ordering does not need to be the
    /// same as the full graph. This means that a subgraph may contain a ring in its
    /// ESSSR which does not belong to the ESSSR of the full graph.
    /// </para>
    /// <para>
    /// To resolve this problem you can turn off the <paramref name="canonical"/> option. This
    /// relaxes the existing condition (Step 2.) and adds all shortest cycles through
    /// each triple (-u-v-w-) to the basis. The number of cycles generated may be
    /// larger however it is now possible to ensure that if <i>H</i> is a subgraph of
    /// <i>G</i> then ESSSR of <i>H</i> will be a subset of the ESSSR or <i>G</i>.
    /// Alternatively one may consider using the <see cref="RelevantCycles"/> which is the
    /// the smallest set of short cycles which is <i>uniquely</i> defined for a
    /// graph.
    /// </para>
    /// <para>
    /// To better explain the issue with the canonical labelling several examples are
    /// shown below. The table outlining the size of rings found for each molecule
    /// when using canonical and non-canonical generation. Also shown are the sizes
    /// of rings stored in the PubChem fingerprint associated with the entry. The
    /// fingerprints were obtained directly from PubChem and decoded using the 
    /// <a href= "ftp://ftp.ncbi.nlm.nih.gov/pubchem/specifications/pubchem_fingerprints.pdf">specification</a>.
    /// Sizes underlined and coloured red represent rings which may
    /// or may not be present depending on the atom ordering. It can be seen from the
    /// PubChem fingerprint that even using a consistent canonical labelling rings
    /// may be absent which would be present if the subgraph was used.
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>PubChem CID</term>
    /// <term>Diagram</term>
    /// <term>Size of Rings in ESSSR (fingerprints only store cycles |C| &lt;=10)</term>
    /// <term>Source</term>
    /// </listheader>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=135973">135973</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=135973"/></term>
    /// <term>
    ///     <list type="table">
    ///         <item><term>{3, 3, 4}</term></item>
    ///         <item><term>{3, 3, 4}</term></item>
    ///         <item><term>{3, 3, 4}</term></item>
    ///     </list>
    /// </term>
    /// <term>
    ///     <list type="table">
    ///         <item><term>Canonical</term></item>
    ///         <item><term>Non-canonical</term></item>
    ///         <item><term>PubChem Fingerprint</term></item>
    ///     </list>
    /// </term>
    /// </item>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=9249">9249</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=9249"/></term>
    /// <term><list type="table">
    /// <item><term>{3, 3, <b style="color: #FF4444;"><u>4</u></b>, 6, 6}</term> </item>
    /// <item><term>{3, 3, 4, 6, 6}</term></item>
    /// <item><term>{3, 3, 6, 6}</term></item>
    /// </list></term>
    /// <term><list type="table">
    /// <item><term>Canonical - <i>4 member cycle only added if found before larger 6
    /// member cycles</i></term></item>
    /// <item><term>Non-canonical</term></item>
    /// <item><term>PubChem Fingerprint - <i>4 member cycle not found</i> </term></item>
    /// </list></term>
    /// </item>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=931">931</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=931"/></term>
    /// <term><list type="table">
    /// <item><term>{6, 6, 10}</term></item>
    /// <item><term>{6, 6, 10}</term></item>
    /// <item><term>{6, 6, 10}</term></item>
    /// </list></term>
    /// <term><list type="table">
    /// <item><term>Canonical</term></item>
    /// <item><term>Non-canonical</term></item>
    /// <item><term>PubChem Fingerprint</term></item>
    /// </list></term>
    /// </item>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=5702">5702</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=5702"/></term>
    /// <term><list type="table">
    /// <item><term>{6, 6, 6, 6, <b style="color: #FF4444;"><u>10</u></b>, <b
    /// style="color: #FF4444;"><u>10</u></b>, 20, 22, 22, 24, 24}</term></item>
    /// <item><term>{6, 6, 6, 6, 10, 10, 20, 22, 22, 24, 24}</term></item>
    /// <item><term>{6, 6, 6, 6}</term></item>
    /// </list></term>
    /// <term><list type="table">
    /// <item><term>Canonical - <i>10 member cycles only added if found before larger
    /// cycles</i></term></item>
    /// <item><term>Non-canonical</term></item>
    /// <item><term>PubChem Fingerprint - <i>10 member cycles not found</i> </term></item>
    /// </list></term>
    /// </item>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=1211">1211</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=1211"/></term>
    /// <term><list type="table">
    /// <item><term>{6, 6, 6, 6, 6, 6, <b style="color: #FF4444;"><u>10</u></b>, <b
    /// style="color: #FF4444;"><u>10</u></b>, 18, 18, 20, 20, 22, 22, 22}</term></item>
    /// <item><term>{6, 6, 6, 6, 6, 6, 10, 10, 18, 18, 20, 20, 22, 22, 22}</term></item>
    /// <item><term>{6, 6, 6, 6, 6, 6, 10, 10}</term></item>
    /// </list></term>
    /// <term><list type="table">
    /// <item><term>Canonical - <i>10 member cycles only added if found before larger cycles</i></term></item>
    /// <item><term>Non-canonical</term></item>
    /// <item><term>PubChem Fingerprint - <i>10 member cycles were found</i> </term></item>
    /// </list></term>
    /// </item>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=17858819">17858819</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=17858819"/></term>
    /// <term><list type="table">
    /// <item><term>{5, 6, 9}</term></item>
    /// <item><term>{5, 6, 9}</term></item>
    /// <item><term>{5, 6, 9}</term></item>
    /// </list></term>
    /// <term><list type="table">
    /// <item><term>Canonical</term></item>
    /// <item><term>Non-canonical</term></item>
    /// <item><term>PubChem Fingerprint</term></item>
    /// </list></term>
    /// </item>
    /// <item>
    /// <term>CID <a href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=1909">1909</a></term>
    /// <term><img src="http://pubchem.ncbi.nlm.nih.gov/image/imgsrv.fcgi?cid=1909"/></term>
    /// <term><list type="table">
    /// <item><term>{5, 5, 5, 6, <b style="color: #FF4444;"><u>9</u></b>, 16, 17, 17, 17, 18}</term></item>
    /// <item><term>{5, 5, 5, 6, 9, 16, 17, 17, 17, 18}</term></item>
    /// <item><term>{5, 5, 5, 6}</term></item>
    /// </list></term>
    /// <term><list type="table">
    /// <item><term>Canonical - <i>9 member cycle only added if found before larger cycles</i></term></item>
    /// <item><term>Non-canonical</term></item>
    /// <item><term>PubChem Fingerprint - <i>9 member cycle not found</i></term></item>
    /// </list></term>
    /// </item>
    /// </list>
    /// </remarks>
    // @author John May
    // @cdk.module core
    // @cdk.keyword ESSSR
    // @cdk.keyword ring
    // @cdk.keyword cycle
    // @see MinimumCycleBasis
    // @see RelevantCycles
    // @cdk.githash
    public sealed class TripletShortCycles
    {
        /// <summary>Adjacency list representation of the graph.</summary>
        private readonly int[][] graph;

        /// <summary>
        /// Whether the basis should be canonical. By definition a canonical set
        /// depends on the atom order.
        /// </summary>
        private readonly bool canonical;

        /// <summary>The current cycle basis.</summary>
        private readonly ICollection<Path> basis = new SortedSet<Path>();

        /// <summary>
        /// Compute the cycles of the extended smallest set of smallest rings (ESSSR)
        /// for an existing minimum cycle basis. Choosing the set to be canonical
        /// means the set depends on the order of the vertices and may <b>not</b> be
        /// consistent in subgraphs. Given a different order of vertices the same
        /// cycles may not be found.
        /// </summary>
        /// <param name="mcb">minimum cycle basis</param>
        /// <param name="canonical">should the set be canonical (non-unique)</param>
        public TripletShortCycles(MinimumCycleBasis mcb, bool canonical)
        {
            // don't reorder neighbors as the MCB was already done on this ordering
            this.graph = Copy(mcb.graph);
            this.canonical = canonical;

            // all minimum cycle basis paths belong to the set
            foreach (var path in mcb.GetPaths())
                basis.Add(new Path(Arrays.CopyOf(path, path.Length - 1)));

            // count the number of cycles each vertex belongs to and try to find a
            // cycle though the triple of 'v' and two of it's neighbors
            int ord = graph.Length;
            int[] nCycles = NCycles(basis, ord);
            for (int v = 0; v < ord; v++)
            {
                if (nCycles[v] > 1) FindTriple(v);
            }
        }

        /// <summary>
        /// Access the vertex paths for all cycles of the basis.
        /// </summary>
        /// <returns>paths of the basis</returns>
        public int[][] GetPaths()
        {
            int i = 0;
            int[][] paths = new int[Count][];

            foreach (var path in basis)
                paths[i++] = path.ToArray();

            return paths;
        }

        /// <summary>
        /// Size of the cycle basis, cardinality of the ESSSR.
        /// </summary>
        public int Count => basis.Count;

        /// <summary>
        /// Try and find cycles through the triple formed from <paramref name="v"/> and any two
        /// of it's neighbours.
        /// </summary>
        /// <param name="v">a vertex in the graph</param>
        private void FindTriple(int v)
        {
            int[] ws = graph[v];
            int deg = ws.Length;

            // disconnect 'v' from its neighbors 'ws'
            Disconnect(ws, v);

            // for every pair of neighbors (u,w) connected to v try and find the
            // shortest path that doesn't travel through 'v'. If a path can be found
            // this is the shortest cycle through the three vertices '-u-v-w-'
            // where u = ws[i] and w = ws[j]
            for (int i = 0; i < deg; i++)
            {
                ShortestPaths sp = new ShortestPaths(graph, null, ws[i]);

                for (int j = i + 1; j < deg; j++)
                {
                    // ignore if there is an exciting cycle through the the triple
                    if (canonical && Exists(ws[i], v, ws[j])) continue;

                    // if there is a path between u and w, form a cycle by appending
                    // v and storing in the basis
                    if (sp.GetNPathsTo(ws[j]) > 0)
                    {
                        // canonic, use the a shortest path (dependant on vertex
                        // order) - non-canonic, use all possible shortest paths
                        int[][] paths = canonical ? new int[][] { sp.GetPathTo(ws[j]) } : sp.GetPathsTo(ws[j]);
                        foreach (var path in paths)
                            basis.Add(new Path(Append(path, v)));
                    }
                }
            }

            Reconnect(ws, v);
        }

        /// <summary>
        /// Is there a cycle already in the basis in which vertices <paramref name="u"/>,
        /// <paramref name="v"/> and <paramref name="w"/> can be found in succession.
        /// </summary>
        /// <param name="u">a vertex adjacent to <paramref name="v"/></param>
        /// <param name="v">a vertex adjacent to <paramref name="u"/> and <paramref name="w"/></param>
        /// <param name="w">a vertex adjacent to <paramref name="v"/></param>
        /// <returns>whether a member of basis contains -u-v-w- in succession</returns>
        private bool Exists(int u, int v, int w)
        {
            foreach (var path in basis)
            {
                if (path.Contains(u, v, w)) return true;
            }
            return false;
        }

        /// <summary>
        /// Temporarily disconnect <paramref name="v"/> from the <paramref name="graph"/> by forming loops
        /// for each of it's neighbours, <paramref name="ws"/>. A loop is an edge in which both
        /// end points are the. Technically <paramref name="v"/> is never removed but we can't
        /// reach <paramref name="v"/> from any other vertex which is sufficient to trace the
        /// triple cycles using <see cref="ShortestPaths"/>.
        /// </summary>
        /// <param name="ws">vertices adjacent to <paramref name="v"/></param>
        /// <param name="v">a vertex <paramref name="v"/></param>
        /// <seealso cref="Reconnect(int[], int)"/>
        private void Disconnect(int[] ws, int v)
        {
            foreach (var w in ws)
            {
                int deg = graph[w].Length;
                for (int i = 0; i < deg; i++)
                {
                    if (graph[w][i] == v) graph[w][i] = w;
                }
            }
        }

        /// <summary>
        /// Reconnect <paramref name="v"/> with the <paramref name="graph"/> by un-looping each of it's
        /// neighbours, <paramref name="ws"/>.
        /// </summary>
        /// <param name="ws">vertices adjacent to <paramref name="v"/></param>
        /// <param name="v">a vertex <paramref name="v"/></param>
        /// <seealso cref="Disconnect(int[], int)"/>
        private void Reconnect(int[] ws, int v)
        {
            foreach (var w in ws)
            {
                int deg = graph[w].Length;
                for (int i = 0; i < deg; i++)
                {
                    if (graph[w][i] == w) graph[w][i] = v;
                }
            }
        }

        /// <summary>
        /// Append the vertex <paramref name="v"/> to the end of the path <paramref name="p"/>.
        /// </summary>
        /// <param name="p">a path</param>
        /// <param name="v">a vertex to append</param>
        /// <returns>the path with <paramref name="v"/> appended</returns>
        private static int[] Append(int[] p, int v)
        {
            int[] q = Arrays.CopyOf(p, p.Length + 1);
            q[p.Length] = v;
            return q;
        }

        /// <summary>
        /// Count how many cycles each vertex belongs to in the given basis.
        /// </summary>
        /// <param name="basis">current basis</param>
        /// <param name="ord">order of the graph</param>
        /// <returns></returns>
        private static int[] NCycles(IEnumerable<Path> basis, int ord)
        {
            int[] nCycles = new int[ord];

            foreach (var path in basis)
                foreach (var v in path.vertices)
                    nCycles[v]++;

            return nCycles;
        }

        /// <summary>
        /// Transform the cycle to that of lowest lexicographic rank. For example the
        /// paths {3,2,1,0} , {3,0,1,2} and {2,1,0,3} are all the same and in the
        /// lexicographic order are {0,1,2,3}.
        /// </summary>
        /// <param name="p">path forming a simple cycle</param>
        /// <returns>path of lowest rank</returns>
        internal static int[] Lexicographic(int[] p)
        {
            // find min value (new start vertex)
            int off = Min(p);
            int len = p.Length;

            // if proceeding value in cycle > preceding value in cycle... reverse
            bool rev = p[(off + 1) % len] > p[(len + off - 1) % len];

            int[] q = new int[len];

            // copy data offset by the min into 'q', reverse if needed
            if (rev)
            {
                for (int i = 0; i < len; i++)
                    q[(len - i) % len] = p[(off + i) % len];
            }
            else
            {
                for (int i = 0; i < len; i++)
                    q[i] = p[(off + i) % len];
            }

            return q;
        }

        /// <summary>
        /// Find the index of lowest value in array.
        /// </summary>
        /// <param name="xs">array of integers</param>
        /// <returns>minimum value</returns>
        private static int Min(int[] xs)
        {
            int min = 0;
            for (int i = 0; i < xs.Length; i++)
            {
                if (xs[i] < xs[min]) min = i;
            }
            return min;
        }

        /// <summary>
        /// Copy the graph <paramref name="g"/>.
        /// </summary>
        /// <param name="g">graph</param>
        /// <returns>copy of the graph</returns>
        private static int[][] Copy(int[][] g)
        {
            int ord = g.Length;
            int[][] h = new int[ord][];

            for (int v = 0; v < ord; v++)
                h[v] = Arrays.CopyOf(g[v], g[v].Length);

            return h;
        }

        /// <summary>
        /// Simple wrapper class for a path of vertices (specified as an int[]). The
        /// class provides comparison with other paths. This is required as the
        /// algorithm can generate the same cycle more then once, and so the cycles
        /// must be stored in a <see cref="ISet{T}"/>.
        /// </summary>
        private class Path
            : IComparable<Path>
        {
            /// <summary>Path of vertices.</summary>
            internal int[] vertices;

            /// <summary>
            /// Create a new path from the given vertices.
            /// </summary>
            /// <param name="vertices">vertices</param>
            internal Path(int[] vertices)
            {
                this.vertices = Lexicographic(vertices);
            }

            /// <summary>
            /// Does this path contain the vertices <paramref name="u"/>, <paramref name="v"/> and <paramref name="w"/> in succession.
            /// </summary>
            /// <param name="u">a vertex connected to <paramref name="v"/></param>
            /// <param name="v">a vertex connected to <paramref name="u"/> and <paramref name="w"/></param>
            /// <param name="w">a vertex connected to <paramref name="v"/></param>
            /// <returns>whether the path contains the triple in succession</returns>
            internal bool Contains(int u, int v, int w)
            {
                int len = vertices.Length;
                for (int i = 0; i < len; i++)
                {
                    if (vertices[i] == v)
                    {
                        // check the next and previous vertices
                        int next = vertices[(i + 1) % len];
                        int prev = vertices[(len + i - 1) % len];
                        return (prev == u && next == w) || (prev == w && next == u);
                    }
                }
                return false;
            }

            /// <summary>
            /// Length of the path.
            /// </summary>
            private int Length => vertices.Length;

            /// <summary>
            /// The path as an array of vertices.
            /// </summary>
            /// <returns>array of vertices</returns>
            internal int[] ToArray()
            {
                int[] p = Arrays.CopyOf(vertices, Length + 1);
                p[Length] = p[0]; // closed walk
                return p;
            }

            public int CompareTo(Path that)
            {
                if (this.Length > that.Length) return +1;
                if (this.Length < that.Length) return -1;
                for (int i = 0; i < Length; i++)
                {
                    if (this.vertices[i] > that.vertices[i]) return +1;
                    if (this.vertices[i] < that.vertices[i]) return -1;
                }
                return 0;
            }
        }
    }
}
