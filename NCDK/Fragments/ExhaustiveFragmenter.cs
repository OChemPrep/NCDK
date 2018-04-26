/* Copyright (C) 2010  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Aromaticities;
using NCDK.Graphs;
using NCDK.Smiles;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Fragments
{
    /// <summary>
    /// Generate fragments exhaustively.
    /// </summary>
    /// <remarks>
    /// This fragmentation scheme simply breaks single non-ring bonds. By default
    /// fragments smaller than 6 atoms in size are not considered, but this can be
    /// changed by the user. Side chains are retained.
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.module  fragment
    // @cdk.githash
    // @cdk.keyword fragment
    public class ExhaustiveFragmenter : IFragmenter
    {
        private const int DEFAULT_MIN_FRAG_SIZE = 6;

        IDictionary<string, IAtomContainer> fragMap;
        SmilesGenerator smilesGenerator;

        /// <summary>
        /// The minimum fragment size.
        /// </summary>
        public int MinimumFragmentSize { get; set; } = 6;

        /// <summary>
        /// Instantiate fragmenter with default minimum fragment size.
        /// </summary>
        public ExhaustiveFragmenter()
                : this(DEFAULT_MIN_FRAG_SIZE)
        { }

        /// <summary>
        /// Instantiate fragmenter with user specified minimum fragment size.
        /// </summary>
        /// <param name="minFragSize">the minimum fragment size desired</param>
        public ExhaustiveFragmenter(int minFragSize)
        {
            this.MinimumFragmentSize = minFragSize;
            fragMap = new Dictionary<string, IAtomContainer>();
            smilesGenerator = new SmilesGenerator(SmiFlavor.Unique | SmiFlavor.UseAromaticSymbols);
        }

        /// <summary>
        /// Generate fragments for the input molecule.
        /// </summary>
        /// <param name="atomContainer">The input molecule.</param>
        public void GenerateFragments(IAtomContainer atomContainer)
        {
            fragMap.Clear();
            Run(atomContainer);
        }

        private List<IAtomContainer> Run(IAtomContainer atomContainer)
        {
            List<IAtomContainer> fragments = new List<IAtomContainer>();

            if (atomContainer.Bonds.Count < 3) return fragments;
            List<IBond> splitableBonds = GetSplitableBonds(atomContainer);
            if (splitableBonds.Count == 0) return fragments;
            Debug.WriteLine("Got " + splitableBonds.Count + " splittable bonds");

            string tmpSmiles;
            foreach (var bond in splitableBonds)
            {
                List<IAtomContainer> parts = FragmentUtils.SplitMolecule(atomContainer, bond);
                // make sure we don't add the same fragment twice
                foreach (var partContainer in parts)
                {
                    AtomContainerManipulator.ClearAtomConfigurations(partContainer);
                    foreach (var atom in partContainer.Atoms)
                        atom.ImplicitHydrogenCount = null;
                    AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(partContainer);
                    CDKHydrogenAdder.GetInstance(partContainer.Builder).AddImplicitHydrogens(partContainer);
                    Aromaticity.CDKLegacy.Apply(partContainer);
                    tmpSmiles = smilesGenerator.Create(partContainer);
                    if (partContainer.Atoms.Count >= MinimumFragmentSize && !fragMap.ContainsKey(tmpSmiles))
                    {
                        fragments.Add(partContainer);
                        fragMap[tmpSmiles] = partContainer;
                    }
                }
            }

            // try and partition the fragments
            List<IAtomContainer> tmp = new List<IAtomContainer>(fragments);
            foreach (var fragment in fragments)
            {
                if (fragment.Bonds.Count < 3 || fragment.Atoms.Count < MinimumFragmentSize) continue;
                if (GetSplitableBonds(fragment).Count == 0) continue;

                List<IAtomContainer> frags = Run(fragment);
                if (frags.Count == 0) continue;

                foreach (var frag in frags)
                {
                    if (frag.Bonds.Count < 3) continue;
                    AtomContainerManipulator.ClearAtomConfigurations(frag);
                    foreach (var atom in frag.Atoms)
                        atom.ImplicitHydrogenCount = null;
                    AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(frag);
                    CDKHydrogenAdder.GetInstance(frag.Builder).AddImplicitHydrogens(frag);
                    Aromaticity.CDKLegacy.Apply(frag);
                    tmpSmiles = smilesGenerator.Create(frag);
                    if (frag.Atoms.Count >= MinimumFragmentSize && !fragMap.ContainsKey(tmpSmiles))
                    {
                        tmp.Add(frag);
                        fragMap[tmpSmiles] = frag;
                    }
                }
            }
            fragments = new List<IAtomContainer>(tmp);
            return fragments;
        }

        private List<IBond> GetSplitableBonds(IAtomContainer atomContainer)
        {
            // do ring detection
            SpanningTree spanningTree = new SpanningTree(atomContainer);
            IRingSet allRings = spanningTree.GetAllRings();

            // find the splitable bonds
            List<IBond> splitableBonds = new List<IBond>();

            foreach (var bond in atomContainer.Bonds)
            {
                bool isInRing = false;
                bool isTerminal = false;

                // lets see if it's in a ring
                var rings = allRings.GetRings(bond);
                if (rings.Any()) isInRing = true;

                // lets see if it is a terminal bond
                foreach (var atom in bond.Atoms)
                {
                    if (atomContainer.GetConnectedBonds(atom).Count() == 1)
                    {
                        isTerminal = true;
                        break;
                    }
                }

                if (!(isInRing || isTerminal)) splitableBonds.Add(bond);
            }
            return splitableBonds;
        }

        /// <summary>
        /// Get the fragments generated as SMILES strings.
        /// </summary>
        /// <returns>a string[] of the fragments.</returns>
        public IEnumerable<string> GetFragments()
        {
            return fragMap.Keys;
        }

        /// <summary>
        /// Get the fragments generated as <see cref="IAtomContainer"/> objects..
        /// </summary>
        /// <returns>a IAtomContainer[] of the fragments.</returns>
        public IEnumerable<IAtomContainer> GetFragmentsAsContainers()
        {
            return fragMap.Values;
        }
    }
}
