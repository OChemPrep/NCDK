using NCDK.Common.Primitives;
using System.Collections.Generic;
using System.Text;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// A signature for an entire graph.
    /// </summary>
    // @author maclean
    public abstract class AbstractGraphSignature
    {
        /// <summary>
        /// The separator is printed between vertex signature strings
        /// </summary>
        private readonly string separator;

        /// <summary>
        /// This is the height the signature is created with, which cannot be exceeded.
        /// </summary>
        private int height;

        private string graphSignature; // XXX

        /// <summary>
        /// Create a graph signature with a default separator.
        /// </summary>
        public AbstractGraphSignature()
            : this(" + ", -1)
        { }

        /// <summary>
        /// Create a graph signature with the given separator.
        /// </summary>
        /// <param name="separator">the separator to use</param>
        public AbstractGraphSignature(string separator)
                : this(separator, -1)
        { }

        /// <summary>
        /// Create a graph signature with a default separator and the given height.
        /// </summary>
        /// <param name="height">the height of the vertex signatures made from this graph.</param>
        public AbstractGraphSignature(int height)
                : this(" + ", height)
        { }

        /// <summary>
        /// Create a graph signature with the given separator and height.
        /// </summary>
        /// <param name="separator">the separator to use</param>
        /// <param name="height">the height of the vertex signatures made from this graph.</param>
        public AbstractGraphSignature(string separator, int height)
        {
            this.separator = separator;
            this.height = height;
        }

        /// <summary>
        /// The height that the graph signature was created with.
        /// </summary>
        public int Height => this.height;

        /// <summary>
        /// Get the vertex count of the graph that this is the signature of.
        /// </summary>
        /// <returns>the vertex count</returns>
        public abstract int GetVertexCount();

        /// <summary>
        /// Return the canonical signature string for the vertex at index 
        /// <paramref name="vertexIndex"/> in the graph.
        /// </summary>
        /// <param name="vertexIndex">the vertex index</param>
        /// <returns>the canonical signature string for this vertex</returns>
        public abstract string SignatureStringForVertex(int vertexIndex);

        /// <summary>
        /// Return the canonical signature string for the vertex at index 
        /// <paramref name="vertexIndex"/> in the graph with a height of 
        /// <paramref name="height"/>.
        /// </summary>
        /// <param name="vertexIndex">the vertex index</param>
        /// <param name="height">the maximum height of the signature</param>
        /// <returns>the signature at the given height for a vertex </returns>
        public abstract string SignatureStringForVertex(int vertexIndex, int height);

        /// <summary>
        /// Generate and return an AbstractVertexSignature rooted at the vertex with index <paramref name="vertexIndex"/>.
        /// </summary>
        /// <param name="vertexIndex">the vertex to use</param>
        /// <returns>an AbstractSignature object</returns>
        public abstract AbstractVertexSignature SignatureForVertex(int vertexIndex);

        /// <summary>
        /// Run through the vertices of the graph, generating a signature string for
        /// each vertex, and return the one that is lexicographically minimal.
        /// </summary>
        /// <returns>the lexicographically minimal vertex string</returns>
        public virtual string ToCanonicalString()
        {
            string canonicalString = null;
            for (int i = 0; i < this.GetVertexCount(); i++)
            {
                string signatureString = this.SignatureStringForVertex(i);
                if (canonicalString == null ||
                        canonicalString.CompareTo(signatureString) > 0)
                {
                    canonicalString = signatureString;
                }
            }
            if (canonicalString == null)
            {
                return "";
            }
            else
            {
                return canonicalString;
            }
        }

        /// <summary>
        /// For all the vertices in the graph, get the signature string and group the
        /// resulting list of strings into symmetry classes. All vertices in one
        /// symmetry class will have the same signature string, and therefore the
        /// same environment.
        /// </summary>
        /// <returns>a list of symmetry classes</returns>
        public List<SymmetryClass> GetSymmetryClasses()
        {
            return GetSymmetryClasses(-1);
        }

        public List<SymmetryClass> GetSymmetryClasses(int height)
        {
            List<SymmetryClass> symmetryClasses = new List<SymmetryClass>();
            for (int i = 0; i < this.GetVertexCount(); i++)
            {
                string signatureString = this.SignatureStringForVertex(i, height);
                SymmetryClass foundClass = null;
                foreach (var symmetryClass in symmetryClasses)
                {
                    if (symmetryClass.HasSignature(signatureString))
                    {
                        foundClass = symmetryClass;
                        break;
                    }
                }
                if (foundClass == null)
                {
                    foundClass = new SymmetryClass(signatureString);
                    symmetryClasses.Add(foundClass);
                }
                foundClass.AddIndex(i);
            }
            return symmetryClasses;
        }

        /// <summary>
        /// Generate signature strings for each vertex of the graph, and count up
        /// how many of each there are, printing out a final string concatenated
        /// together with the separator.
        /// </summary>
        /// <returns>a full signature string for this graph</returns>
        public string ToFullString()
        {
            IDictionary<string, int> sigmap = new Dictionary<string, int>();
            for (int i = 0; i < this.GetVertexCount(); i++)
            {
                string signatureString = this.SignatureStringForVertex(i);
                if (sigmap.ContainsKey(signatureString))
                {
                    int count = sigmap[signatureString];
                    sigmap[signatureString] = count + 1;
                }
                else
                {
                    sigmap[signatureString] = 1;
                }
            }
            List<string> keyList = new List<string>();
            keyList.AddRange(sigmap.Keys);
            keyList.Sort(Strings.OrdinalComparer.Instance);
            StringBuilder buffer = new StringBuilder();
            for (int i = 0; i < keyList.Count - 1; i++)
            {
                string signature = keyList[i];
                int count = sigmap[signature];
                buffer.Append(count).Append(signature).Append(this.separator);
            }
            {
                string finalSignature = keyList[keyList.Count - 1];
                int count = sigmap[finalSignature];
                buffer.Append(count).Append(finalSignature);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Use the lexicographically largest (or smallest) as the graph signature
        /// </summary>
        public string GetGraphSignature()
        {
            // Generates and returns a graph signature
            this.graphSignature = GetMaximalSignature();
            return this.graphSignature;
        }

        public List<string> GetSortedSignatures()
        {
            var vertexSignatures = this.GetVertexSignatureStrings();
            vertexSignatures.Sort(Strings.OrdinalComparer.Instance);
            return vertexSignatures;
        }

        public string GetMinimalSignature()
        {
            List<string> sortedSignatures = GetSortedSignatures();
            return sortedSignatures[sortedSignatures.Count - 1];
        }

        public string GetMaximalSignature()
        {
            List<string> sortedSignatures = GetSortedSignatures();
            return sortedSignatures[0];
        }

        /// <summary>
        /// Create the canonical signature strings for each vertex. They are 
        /// unsorted, so will be in the same order as the vertices.
        /// </summary>
        /// <returns>a list of canonical signature strings</returns>
        public List<string> GetVertexSignatureStrings()
        {
            List<string> vertexSignatures = new List<string>();
            for (int i = 0; i < this.GetVertexCount(); i++)
            {
                vertexSignatures.Add(this.SignatureStringForVertex(i));
            }
            return vertexSignatures;
        }

        /// <summary>
        /// Create a list of vertex signatures, one for each vertex.They are 
        /// unsorted, so will be in the same order as the vertices.
        /// </summary>
        /// <returns>a list of vertex signatures</returns>
        public List<AbstractVertexSignature> GetVertexSignatures()
        {
            List<AbstractVertexSignature> signatures =
                new List<AbstractVertexSignature>();
            for (int i = 0; i < this.GetVertexCount(); i++)
            {
                signatures.Add(this.SignatureForVertex(i));
            }
            return signatures;
        }

        /// <summary>
        /// Test the the vertices in the graph, to see if the order they are in
        /// (confusingly called the 'labelling' of the graph) is canonical. The 
        /// order that is canonical according to this method may not be the same as
        /// the canonical order from another method.
        /// </summary>
        /// <returns>true if the vertices are in a canonical order</returns>
        public bool IsCanonicallyLabelled()
        {
            int[] labels = GetCanonicalLabels();
            int previousLabel = -1;
            for (int i = 0; i < labels.Length; i++)
            {
                if (previousLabel == -1 || labels[i] > previousLabel)
                {
                    previousLabel = labels[i];
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public void ReconstructCanonicalGraph(
                AbstractVertexSignature signature, AbstractGraphBuilder builder)
        {
            string canonicalString = this.ToCanonicalString();
            ColoredTree tree = AbstractVertexSignature.Parse(canonicalString);
            builder.MakeFromColoredTree(tree);
        }

        public int[] GetCanonicalLabels()
        {
            int n = GetVertexCount();
            AbstractVertexSignature canonicalSignature = null;
            string canonicalSignatureString = null;
            for (int i = 0; i < n; i++)
            {
                AbstractVertexSignature signatureForVertexI = SignatureForVertex(i);
                string signatureString = signatureForVertexI.ToCanonicalString();
                if (canonicalSignature == null ||
                        signatureString.CompareTo(canonicalSignatureString) < 0)
                {
                    canonicalSignature = signatureForVertexI;
                    canonicalSignatureString = signatureString;
                }
            }
            return canonicalSignature.GetCanonicalLabelling(n);
        }

        public string ReconstructCanonicalEdgeString()
        {
            string canonicalString = this.ToCanonicalString();
            VirtualGraphBuilder builder = new VirtualGraphBuilder();

            builder.MakeFromColoredTree(
                    AbstractVertexSignature.Parse(canonicalString));
            return builder.ToEdgeString();
        }
    }
}
