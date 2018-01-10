/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 */
using NCDK.Common.Collections;
using NCDK.Common.Mathematics;
using NCDK.Config;
using NCDK.Geometries;
using NCDK.Graphs;
using NCDK.Isomorphisms;
using NCDK.Numerics;
using NCDK.RingSearches;
using NCDK.SGroups;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Layout
{
    /// <summary>
    /// Generates 2D coordinates for a molecule for which only connectivity is known
    /// or the coordinates have been discarded for some reason.
    /// </summary>
    /// <example>
    /// Usage: Create an instance of this class, thereby assigning a molecule,
    /// call <see cref="GenerateCoordinates()"/> and get your molecule back:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Layout.StructureDiagramGenerator_Example.cs"]/*' />
    /// </example>
    /// <remarks>
    /// The method will fail if the molecule is disconnected. The 
    /// <see cref="ConnectivityChecker.PartitionIntoMolecules(IAtomContainer)"/> can help here.
    /// </remarks>
    /// <seealso cref="ConnectivityChecker.PartitionIntoMolecules(IAtomContainer)"/>
    // @author steinbeck
    // @cdk.created 2004-02-02
    // @cdk.keyword Layout
    // @cdk.keyword Structure Diagram Generation (SDG)
    // @cdk.keyword 2D-coordinates
    // @cdk.keyword Coordinate generation, 2D
    // @cdk.dictref blue-obelisk:layoutMolecule
    // @cdk.module sdg
    // @cdk.githash
    // @cdk.bug 1536561
    // @cdk.bug 1788686
    public class StructureDiagramGenerator
    {
        public static readonly double RAD_30 = Vectors.DegreeToRadian(-30);
        public const double DefaultBondLength = 1.5;

        private IAtomContainer molecule;
        private IRingSet sssr;
        /// <summary>
        /// The bond length used for laying out the molecule. 
        /// The default value is 1.5.
        /// </summary>
        public double BondLength { get; set; } = DefaultBondLength;
        private Vector2 firstBondVector;
        private RingPlacer ringPlacer = new RingPlacer();
        private AtomPlacer atomPlacer = new AtomPlacer();
        private MacroCycleLayout macroPlacer = null;
        private List<IRingSet> ringSystems = null;
        private ISet<IAtom> afix = null;
        private ISet<IBond> bfix = null;
        /// <summary>
        /// Whether identity templates are used. Identity templates use an exact match
        /// are are very fast. They are used for layout of the 'primary' ring system
        /// in de facto orientation.
        /// </summary>
        public bool UseIdentityTemplates { private get; set; } = true;
        private bool alignMappedReaction = true;

        /// <summary>
        /// show we orient the structure (false: keep de facto ring systems drawn the right way up)
        /// </summary>
        private bool selectOrientation = true;

        /// <summary>
        /// Identity templates - for laying out primary ring system.
        /// </summary>
        private IdentityTemplateLibrary identityLibrary;

        public static Vector2 DefaultBondVector = new Vector2(0, 1);
        private static IdentityTemplateLibrary DefaultTempleteLibrary =
            IdentityTemplateLibrary.LoadFromResource("custom-templates.smi")
                .Add(IdentityTemplateLibrary.LoadFromResource("chebi-ring-templates.smi"));

        public StructureDiagramGenerator()
                : this(DefaultTempleteLibrary)
        { }

        private StructureDiagramGenerator(IdentityTemplateLibrary identityLibrary)
        {
            this.identityLibrary = identityLibrary;
        }

        /// <summary>
        /// Creates an instance of this class while assigning a molecule to be layed out.
        /// </summary>
        /// <param name="molecule">The molecule to be layed out.</param>
        public StructureDiagramGenerator(IAtomContainer molecule)
                : this()
        {
            SetMolecule(molecule, false);
        }

        /// <summary>
        /// Convenience method for generating 2D coordinates.
        /// </summary>
        /// <example>
        /// The method is short-hand for calling:
        /// <code>
        /// sdg.setMolecule(mol, false);
        /// sdg.generateCoordinates();
        /// </code></example>
        /// <param name="mol">molecule to layout</param>
        /// <exception cref="CDKException">problem with layout</exception>
        public void GenerateCoordinates(IAtomContainer mol)
        {
            SetMolecule(mol, false);
            GenerateCoordinates();
        }

        /// <summary>
        /// <para>Convenience method to generate 2D coordinates for a reaction. If atom-atom
        /// maps are present on a reaction, the substructures are automatically aligned.</para>
        /// <para>This feature can be disabled by changing the <see cref="SetAlignMappedReaction(bool)"/>.</para>
        /// </summary>
        /// <param name="reaction">reaction to layout</param>
        /// <exception cref="CDKException">problem with layout</exception>
        public void GenerateCoordinates(IReaction reaction)
        {
            // layout products and agents
            foreach (IAtomContainer mol in reaction.Products)
                GenerateCoordinates(mol);
            foreach (IAtomContainer mol in reaction.Agents)
                GenerateCoordinates(mol);

            // do not align = simple layout of reactants
            if (alignMappedReaction)
            {
                ISet<IBond> mapped = ReactionManipulator.FindMappedBonds(reaction);

                var refmap = new MultiDictionary<int, IDictionary<int, IAtom>>();

                foreach (IAtomContainer mol in reaction.Products)
                {
                    Cycles.MarkRingAtomsAndBonds(mol);
                    ConnectedComponents cc = new ConnectedComponents(GraphUtil.ToAdjListSubgraph(mol, mapped));
                    var parts = ConnectivityChecker.PartitionIntoMolecules(mol, cc.Components());
                    foreach (IAtomContainer part in parts)
                    {
                        // skip single atoms (unmapped)
                        if (part.Atoms.Count == 1)
                            continue;
                        IDictionary<int, IAtom> map = new Dictionary<int, IAtom>();
                        foreach (IAtom atom in part.Atoms)
                        {
                            // safe as substructure should only be mapped bonds and therefore atoms!
                            int idx = atom.GetProperty<int>(CDKPropertyName.AtomAtomMapping);
                            if (map.ContainsKey(idx))
                            {
                                map[idx] = atom;
                            }
                            else
                            {
                                map[idx] = atom;
                                refmap.Add(idx, map);
                            }
                        }
                    }
                }

                IDictionary<IAtom, IAtom> afix = new Dictionary<IAtom, IAtom>();
                ISet<IBond> bfix = new HashSet<IBond>();

                foreach (IAtomContainer mol in reaction.Reactants)
                {
                    Cycles.MarkRingAtomsAndBonds(mol);
                    ConnectedComponents cc = new ConnectedComponents(GraphUtil.ToAdjListSubgraph(mol, mapped));
                    var parts = ConnectivityChecker.PartitionIntoMolecules(mol, cc.Components());

                    // we only aligned the largest part
                    IAtomContainer largest = null;
                    foreach (IAtomContainer part in parts)
                    {
                        if (largest == null || part.Bonds.Count > largest.Bonds.Count)
                            largest = part;
                    }

                    afix.Clear();
                    bfix.Clear();

                    if (largest != null && largest.Atoms.Count > 1)
                    {
                        int idx = largest.Atoms[0].GetProperty<int>(CDKPropertyName.AtomAtomMapping);

                        // select the largest and use those coordinates
                        IDictionary<int, IAtom> reference = Select(refmap[idx]);
                        foreach (IAtom atom in largest.Atoms)
                        {
                            idx = atom.GetProperty<int>(CDKPropertyName.AtomAtomMapping);
                            IAtom src = reference[idx];
                            if (src == null) continue;
                            atom.Point2D = src.Point2D;
                            afix[atom] = src;
                        }
                    }

                    if (afix.Any())
                    {
                        foreach (IBond bond in mol.Bonds)
                        {
                            if (afix.ContainsKey(bond.Begin) && afix.ContainsKey(bond.End))
                            {
                                // only fix acyclic bonds if the source atoms were also acyclic
                                if (!bond.IsInRing)
                                {
                                    IAtom srcBeg = afix[bond.Begin];
                                    IAtom srcEnd = afix[bond.End];
                                    foreach (IAtomContainer product in reaction.Products)
                                    {
                                        IBond srcBond = product.GetBond(srcBeg, srcEnd);
                                        if (srcBond != null)
                                        {
                                            if (!srcBond.IsInRing)
                                                bfix.Add(bond); // safe to add
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    bfix.Add(bond);
                                }
                            }
                        }
                    }

                    SetMolecule(mol, false, Sets.Wrap(afix.Keys), bfix);
                    GenerateCoordinates();
                }

                // reorder reactants such that they are in the same order they appear on the right
                reaction.Reactants.Sort(centerComparer);

            }
            else
            {
                foreach (IAtomContainer mol in reaction.Reactants)
                    GenerateCoordinates(mol);
            }
        }

        static readonly CenterComparer centerComparer = new CenterComparer();
        class CenterComparer : IComparer<IAtomContainer>
        {
            public int Compare(IAtomContainer a, IAtomContainer b)
            {
                var aCenter = GeometryUtil.Get2DCenter(a);
                var bCenter = GeometryUtil.Get2DCenter(b);
                return aCenter.X.CompareTo(bCenter.X);
            }
        }

        private IDictionary<int, IAtom> Select(IEnumerable<IDictionary<int, IAtom>> refs)
        {
            IDictionary<int, IAtom> largest = Dictionaries.Empty<int, IAtom>();
            foreach (IDictionary<int, IAtom> ref_ in refs)
            {
                if (ref_.Count > largest.Count)
                    largest = ref_;
            }
            return largest;
        }

        public void SetMolecule(IAtomContainer mol, bool clone)
        {
            SetMolecule(mol, clone, Sets.Empty<IAtom>(), Sets.Empty<IBond>());
        }

        /// <summary>
        /// Assigns a molecule to be laid out. After, setting the molecule call generateCoordinates() to assign
        /// 2D coordinates. An optional set of atoms/bonds can be parsed in to allow partial layout, these will
        /// be 'fixed' in place. This only applies to non-cloned molecules, and only atoms with coordinates can
        /// be fixed.
        /// </summary>
        /// <param name="mol">the molecule for which coordinates are to be generated.</param>
        /// <param name="clone">Should the whole process be performed with a cloned copy?</param>
        /// <param name="afix">Atoms that should be fixed in place, coordinates are not changed.</param>
        /// <param name="bfix">Bonds that should be fixed in place, they will not be flipped, bent, or streched.</param>
        public void SetMolecule(IAtomContainer mol, bool clone, ISet<IAtom> afix, ISet<IBond> bfix)
        {
            if (clone)
            {
                if (afix.Any() || bfix.Any())
                    throw new ArgumentException("Laying out a cloned molecule, can't fix atom or bonds.");
                this.molecule = (IAtomContainer)mol.Clone();
            }
            else
            {
                this.molecule = mol;
            }
            this.afix = afix;
            this.bfix = bfix;
            foreach (IAtom atom in molecule.Atoms)
            {
                bool afixed = afix.Contains(atom);

                if (afixed && atom.Point2D == null)
                {
                    afixed = false;
                    afix.Remove(atom);
                }

                if (afixed)
                {
                    atom.IsPlaced = true;
                    atom.IsVisited = true;
                }
                else
                {
                    atom.Point2D = null;
                    atom.IsPlaced = false;
                    atom.IsVisited = false;
                    atom.IsInRing = false;
                    atom.IsAliphatic = false;
                }
            }
            atomPlacer.Molecule = this.molecule;
            ringPlacer.Molecule = this.molecule;
            ringPlacer.AtomPlacer = this.atomPlacer;
            macroPlacer = new MacroCycleLayout(mol);
            selectOrientation = !afix.Any();
        }

        /// <summary>
        /// whether the use of templates is enabled or disabled.
        /// true, when the use of templates is enables, false otherwise
        /// </summary>
        /// <remarks>always false, substructure templates are not used anymore</remarks>
        [Obsolete]
        public bool UseTemplates
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// The templateHandler attribute of the <see cref="StructureDiagramGenerator"/>  object.
        /// </summary>
        /// <remarks>Always <see langword="null"/>, substructure templates are not used anymore
        /// substructure templates are no longer used for layout but those provided here
        /// will be converted to identity templates</remarks>
        [Obsolete]
        public TemplateHandler TemplateHandler
        {
            get { return null; }
            set
            {
                IdentityTemplateLibrary lib = value.ToIdentityTemplateLibrary();
                lib.Add(identityLibrary);
                identityLibrary = lib; // new ones take priority
            }
        }

        /// <summary>
        /// Set whether reaction reactants should be allignned to their product.
        /// </summary>
        /// <param name="align">align setting</param>
        public void SetAlignMappedReaction(bool align)
        {
            this.alignMappedReaction = align;
        }

        /// <summary>
        /// The molecule with new coordinates (if <see cref="GenerateCoordinates()"/> had been called)
        /// </summary>
        public IAtomContainer Molecule
        {
            get
            {
                return molecule;
            }
            set
            {
                SetMolecule(value, true);
            }
        }

        /// <summary>
        /// This method uses <see cref="GenerateCoordinates()"/>, but it removes the hydrogens first,
        /// lays out the structure and then adds them again.
        /// </summary>
        /// <exception cref="CDKException">if an error occurs</exception>
        /// <seealso cref="GenerateCoordinates()"/>
        [Obsolete("Use " + nameof(GenerateCoordinates) + "()")]
        public void GenerateExperimentalCoordinates()
        {
            GenerateExperimentalCoordinates(DefaultBondVector);
        }

        /// <summary>
        /// Generates 2D coordinates on the non-hydrogen skeleton, after which
        /// coordinates for the hydrogens are calculated.
        /// </summary>
        /// <param name="firstBondVector">the vector of the first bond to lay out</param>
        /// <exception cref="CDKException">if an error occurs</exception>
        [Obsolete("Use " + nameof(GenerateCoordinates) + "()")]
        public void GenerateExperimentalCoordinates(Vector2 firstBondVector)
        {
            // first make a shallow copy: Atom/Bond references are kept
            IAtomContainer original = molecule;
            IAtomContainer shallowCopy = molecule.Builder.NewAtomContainer(molecule);
            // delete single-bonded H's from
            //IAtom[] atoms = shallowCopy.Atoms;
            foreach (var curAtom in shallowCopy.Atoms)
            {
                if (curAtom.Symbol.Equals("H"))
                {
                    if (shallowCopy.GetConnectedBonds(curAtom).Count() < 2)
                    {
                        shallowCopy.RemoveAtom(curAtom);
                        curAtom.Point2D = null;
                    }
                }
            }
            // do layout on the shallow copy
            molecule = shallowCopy;
            GenerateCoordinates(firstBondVector);
            double BondLength = GeometryUtil.GetBondLengthAverage(molecule);
            // ok, now create the coordinates for the hydrogens
            HydrogenPlacer hPlacer = new HydrogenPlacer();
            molecule = original;
            hPlacer.PlaceHydrogens2D(molecule, BondLength);
        }

        /// <summary>
        /// The main method of this StructurDiagramGenerator. Assign a molecule to the
        /// StructurDiagramGenerator, call the GenerateCoordinates() method and get
        /// your molecule back.
        /// </summary>
        /// <param name="firstBondVector">The vector of the first bond to layout</param>
        /// <exception cref="CDKException">if an error occurs</exception>
        public void GenerateCoordinates(Vector2 firstBondVector)
        {
            GenerateCoordinates(firstBondVector, false, false);
        }

        /// <summary>
        /// The main method of this StructureDiagramGenerator. Assign a molecule to the
        /// StructureDiagramGenerator, call the generateCoordinates() method and get
        /// your molecule back.
        /// </summary>
        /// <param name="firstBondVector">the vector of the first bond to lay out</param>
        /// <param name="isConnected">the 'molecule' attribute is guaranteed to be connected (we have checked)</param>
        /// <param name="isSubLayout">the 'molecule' is being laid out as part of a large collection of fragments</param>
        /// <exception cref="CDKException">problem occurred during layout</exception>
        private void GenerateCoordinates(Vector2 firstBondVector, bool isConnected, bool isSubLayout)
        {
            int numAtoms = molecule.Atoms.Count;
            int numBonds = molecule.Bonds.Count;
            this.firstBondVector = firstBondVector;

            // if molecule contains only one Atom, don't fail, simply set
            // coordinates to simplest: 0,0. See bug #780545
            Debug.WriteLine("Entry point of generateCoordinates()");
            Debug.WriteLine("We have a molecules with " + numAtoms + " atoms.");
            if (numAtoms == 0)
            {
                return;
            }
            if (numAtoms == 1)
            {
                molecule.Atoms[0].Point2D = new Vector2(0, 0);
                return;
            }
            else if (molecule.Bonds.Count == 1 && molecule.Atoms.Count == 2)
            {
                double xOffset = 0;
                foreach (IAtom atom in molecule.Atoms)
                {
                    atom.Point2D = new Vector2(xOffset, 0);
                    xOffset += BondLength;
                }
                return;
            }

            // intercept fragment molecules and lay them out in a grid
            if (!isConnected)
            {
                var frags = ConnectivityChecker.PartitionIntoMolecules(molecule);
                if (frags.Count > 1)
                {
                    IAtomContainer rollback = molecule;
                    GenerateFragmentCoordinates(molecule, frags);
                    // don't call set molecule as it wipes x,y coordinates!
                    // this looks like a self assignment but actually the fragment
                    // method changes this.molecule
                    this.molecule = rollback;
                    atomPlacer.Molecule = this.molecule;
                    ringPlacer.Molecule = this.molecule;
                    macroPlacer = new MacroCycleLayout(this.molecule);
                    return;
                }
            }

            // initial layout seeding either from a ring system of longest chain
            SeedLayout();

            // Now, do the layout of the rest of the molecule
            int iter = 0;
            for (; !AtomPlacer.AllPlaced(molecule) && iter < numAtoms; iter++)
            {
                Debug.WriteLine("*** Start of handling the rest of the molecule. ***");
                // layout for all acyclic parts of the molecule which are
                // connected to the parts which have already been laid out.
                LayoutAcyclicParts();
                // layout cyclic parts of the molecule which
                // are connected to the parts which have already been laid out.
                LayoutCyclicParts();
            }

            // display reasonable error on failed layout, otherwise we'll have a NPE somewhere
            if (iter == numAtoms && !AtomPlacer.AllPlaced(molecule))
                throw new CDKException("Could not generate layout? If a set of 'fixed' atoms were provided"
                                           + " try removing these and regenerating the layout.");

            if (!isSubLayout)
            {
                // correct double-bond stereo, this changes the layout and in reality
                // should be done during the initial placement
                if (molecule.StereoElements.Any())
                    CorrectGeometricConfiguration.Correct(molecule);
            }

            RefinePlacement(molecule);
            FinalizeLayout(molecule);

            if (!isSubLayout)
                AssignStereochem(molecule);
        }

        /// <summary>
        /// Determine if any atoms in a connected molecule are fixed (i.e. already have coordinates/
        /// have been placed).
        /// </summary>
        /// <param name="mol">the moleucle to check</param>
        /// <returns>atoms are fixed</returns>
        private bool HasFixedPart(IAtomContainer mol)
        {
            if (!afix.Any()) return false;
            foreach (IAtom atom in mol.Atoms)
                if (afix.Contains(atom))
                    return true;
            return false;
        }

        private void SeedLayout()
        {
            int numAtoms = this.molecule.Atoms.Count;
            int numBonds = this.molecule.Bonds.Count;
            if (HasFixedPart(molecule))
            {
                if (PrepareRingSystems() > 0)
                {
                    PrepareRingSystems();
                    foreach (IRingSet rset in ringSystems)
                    {
                        if (rset.IsPlaced)
                        {
                            ringPlacer.PlaceRingSubstituents(rset, BondLength);
                        }
                        else
                        {
                            List<IRing> placed = new List<IRing>();
                            List<IRing> unplaced = new List<IRing>();

                            foreach (IAtomContainer ring in rset)
                            {
                                if (ring.IsPlaced)
                                    placed.Add((IRing)ring);
                                else
                                    unplaced.Add((IRing)ring);
                            }

                            // partially laid out rings
                            if (!placed.Any())
                            {
                                foreach (IRing ring in unplaced)
                                {
                                    if (ringPlacer.CompletePartiallyPlacedRing(rset, ring, BondLength))
                                        placed.Add(ring);
                                }
                                foreach (var ring in placed)
                                    unplaced.Remove(ring);
                            }

                            while (unplaced.Any() && placed.Any())
                            {
                                foreach (IAtomContainer ring in placed)
                                {
                                    ringPlacer.PlaceConnectedRings(rset, (IRing)ring, RingPlacer.Fused, BondLength);
                                    ringPlacer.PlaceConnectedRings(rset, (IRing)ring, RingPlacer.Bridged, BondLength);
                                    ringPlacer.PlaceConnectedRings(rset, (IRing)ring, RingPlacer.Spiro, BondLength);
                                }
                                placed.Clear();
                                foreach (var ring in unplaced)
                                {
                                    if (ring.IsPlaced)
                                    {
                                        placed.Add(ring);
                                    }
                                }
                                foreach (var ring in placed)
                                    unplaced.Remove(ring);
                            }

                            if (AllPlaced(rset))
                            {
                                rset.IsPlaced = true;
                                ringPlacer.PlaceRingSubstituents(rset, BondLength);
                            }
                        }
                    }
                }
            }
            else if (PrepareRingSystems() > 0)
            {
                Debug.WriteLine("*** Start of handling rings. ***");
                PrepareRingSystems();

                // We got our ring systems now choose the best one based on size and
                // number of heteroatoms
                RingPlacer.CountHetero(ringSystems);
                ringSystems.Sort(RingPlacer.RING_COMPARATOR);

                int respect = LayoutRingSet(firstBondVector, ringSystems[0]);

                // rotate monocyclic and when >= 4 polycyclic
                if (respect == 1)
                {
                    if (ringSystems[0].Count == 1)
                    {
                        respect = 0;
                    }
                    else if (ringSystems.Count >= 4)
                    {
                        int numPoly = 0;
                        foreach (IRingSet rset in ringSystems)
                            if (rset.Count > 1)
                                numPoly++;
                        if (numPoly >= 4)
                            respect = 0;
                    }
                }

                if (respect == 1 || respect == 2)
                    selectOrientation = false;

                Debug.WriteLine("First RingSet placed");

                // place of all the directly connected atoms of this ring system
                ringPlacer.PlaceRingSubstituents(ringSystems[0], BondLength);
            }
            else
            {
                Debug.WriteLine("*** Start of handling purely aliphatic molecules. ***");

                // We are here because there are no rings in the molecule so we get the longest chain in the molecule
                // and placed in on a horizontal axis
                Debug.WriteLine("Searching initialLongestChain for this purely aliphatic molecule");
                IAtomContainer longestChain = AtomPlacer.GetInitialLongestChain(molecule);
                Debug.WriteLine("Found linear chain of length " + longestChain.Atoms.Count);
                Debug.WriteLine("Setting coordinated of first atom to 0,0");
                longestChain.Atoms[0].Point2D = new Vector2(0, 0);
                longestChain.Atoms[0].IsPlaced = true;

                // place the first bond such that the whole chain will be horizontally alligned on the x axis
                Debug.WriteLine("Attempting to place the first bond such that the whole chain will be horizontally alligned on the x axis");
                if (firstBondVector != null && firstBondVector != DefaultBondVector)
                    atomPlacer.PlaceLinearChain(longestChain, firstBondVector, BondLength);
                else
                    atomPlacer.PlaceLinearChain(longestChain, new Vector2(Math.Cos(RAD_30), Math.Sin(RAD_30)), BondLength);
                Debug.WriteLine("Placed longest aliphatic chain");
            }
        }

        private int PrepareRingSystems()
        {
            int numRings = Cycles.MarkRingAtomsAndBonds(molecule);
            // compute SSSR/MCB
            if (numRings > 0)
            {
                sssr = Cycles.FindSSSR(molecule).ToRingSet();

                if (sssr.Count < 1)
                    throw new InvalidOperationException("Molecule expected to have rings, but had none?");

                // Give a handle of our molecule to the ringPlacer
                ringPlacer.Molecule = molecule;
                ringPlacer.CheckAndMarkPlaced(sssr);

                // Partition the smallest set of smallest rings into disconnected
                // ring system. The RingPartioner returns a Vector containing
                // RingSets. Each of the RingSets contains rings that are connected
                // to each other either as bridged ringsystems, fused rings or via
                // spiro connections.
                ringSystems = RingPartitioner.PartitionRings(sssr).ToList();
            }
            else
            {
                sssr = molecule.Builder.NewRingSet();
                ringSystems = new List<IRingSet>();
            }
            return numRings;
        }

        private void AssignStereochem(IAtomContainer molecule)
        {
            // XXX: can't check this unless we store 'unspecified' double bonds
            // if (!molecule.stereoElements().iterator().hasNext())
            //     return;

            // assign up/down labels, this doesn't not alter layout and could be
            // done on-demand (e.g. when writing a MDL Molfile)
            NonplanarBonds.Assign(molecule);
        }

        private void RefinePlacement(IAtomContainer molecule)
        {
            AtomPlacer.Prioritise(molecule);

            // refine the layout by rotating, bending, and stretching bonds
            LayoutRefiner refiner = new LayoutRefiner(molecule, afix, bfix);
            refiner.Refine();

            // choose the orientation in which to display the structure
            if (selectOrientation)
            {
                // check for attachment points, these override the direction which we rorate structures
                IAtom begAttach = null;
                foreach (var atom in molecule.Atoms)
                {
                    if (atom is IPseudoAtom && ((IPseudoAtom)atom).AttachPointNum == 1)
                    {
                        begAttach = atom;
                        break;
                    }
                }

                // no attachment point, rorate to maximise horizontal spread etc.
                if (begAttach == null)
                {
                    SelectOrientation(molecule, 2 * DefaultBondLength, 1);
                }
                // use attachment point bond to rotate
                else
                {
                    var attachBonds = molecule.GetConnectedBonds(begAttach);
                    if (attachBonds.Count() == 1)
                    {
                        IAtom end = attachBonds.First().GetOther(begAttach);
                        Vector2 xyBeg = begAttach.Point2D.Value;
                        Vector2 xyEnd = end.Point2D.Value;

                        // snap to horizontal '*-(end)-{rest of molecule}'
                        GeometryUtil.Rotate(molecule,
                                            GeometryUtil.Get2DCenter(molecule),
                                            -Math.Atan2(xyEnd.Y - xyBeg.Y, xyEnd.X - xyBeg.X));

                        // put the larger part of the structure is above the bond so fragments are drawn
                        // semi-consistently
                        double ylo = 0;
                        double yhi = 0;
                        foreach (var atom in molecule.Atoms)
                        {
                            double yDelta = xyBeg.Y - atom.Point2D.Value.Y;
                            if (yDelta > 0 && yDelta > yhi)
                            {
                                yhi = yDelta;
                            }
                            else if (yDelta < 0 && yDelta < ylo)
                            {
                                ylo = yDelta;
                            }
                        }

                        // mirror points if larger part is below
                        if (Math.Abs(ylo) < yhi)
                            foreach (var atom in molecule.Atoms)
                                atom.Point2D = new Vector2(atom.Point2D.Value.X, -atom.Point2D.Value.Y);

                        // rotate pointing downwards 30-degrees
                        GeometryUtil.Rotate(molecule,
                                            GeometryUtil.Get2DCenter(molecule),
                                            -Vectors.DegreeToRadian(30));
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the molecule layout, primarily updating Sgroups.
        /// </summary>
        /// <param name="mol">molecule being laid out</param>
        private void FinalizeLayout(IAtomContainer mol)
        {
            PlaceMultipleGroups(mol);
            PlacePositionalVariation(mol);
            PlaceSgroupBrackets(mol);
        }

        /// <summary>
        /// Select the global orientation of the layout. We click round at 30 degree increments
        /// and select the orientation that a) is the widest or b) has the most bonds aligned to
        /// +/- 30 degrees <token>cdk-cite-Clark06</token>.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="widthDiff">parameter at which to consider orientations equally good (wide select)</param>
        /// <param name="alignDiff">parameter at which we consider orientations equally good (bond align select)</param>
        private static void SelectOrientation(IAtomContainer mol, double widthDiff, int alignDiff)
        {
            double[] minmax = GeometryUtil.GetMinMax(mol);
            Vector2 pivot = new Vector2(minmax[0] + ((minmax[2] - minmax[0]) / 2), minmax[1] + ((minmax[3] - minmax[1]) / 2));

            double maxWidth = minmax[2] - minmax[0];
            int maxAligned = CountAlignedBonds(mol);

            Vector2[] coords = new Vector2[mol.Atoms.Count];
            for (int i = 0; i < mol.Atoms.Count; i++)
                coords[i] = mol.Atoms[i].Point2D.Value;

            double step = Vectors.DegreeToRadian(30);
            int numSteps = (360 / 30) - 1;
            for (int i = 0; i < numSteps; i++)
            {
                GeometryUtil.Rotate(mol, pivot, step);
                minmax = GeometryUtil.GetMinMax(mol);

                double width = minmax[2] - minmax[0];
                double delta = Math.Abs(width - maxWidth);

                // if this orientation is significantly wider than the
                // best so far select it
                if (delta > widthDiff && width > maxWidth)
                {
                    maxWidth = width;
                    for (int j = 0; j < mol.Atoms.Count; j++)
                        coords[j] = mol.Atoms[j].Point2D.Value;
                }
                // width is not significantly better or worse so check
                // the number of bonds aligned to 30 deg (aesthetics)
                else if (delta <= widthDiff)
                {
                    int aligned = CountAlignedBonds(mol);
                    int alignDelta = aligned - maxAligned;
                    if (alignDelta > alignDiff || (alignDelta == 0 && width > maxWidth))
                    {
                        maxAligned = aligned;
                        maxWidth = width;
                        for (int j = 0; j < mol.Atoms.Count; j++)
                            coords[j] = mol.Atoms[j].Point2D.Value;
                    }
                }
            }

            // set the best coordinates we found
            for (int i = 0; i < mol.Atoms.Count; i++)
                mol.Atoms[i].Point2D = coords[i];
        }

        /// <summary>
        /// Count the number of bonds aligned to 30 degrees.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>number of aligned bonds</returns>
        private static int CountAlignedBonds(IAtomContainer mol)
        {
            double ref_ = Vectors.DegreeToRadian(30);
            double diff = Vectors.DegreeToRadian(1);
            int count = 0;
            foreach (var bond in mol.Bonds)
            {
                Vector2 beg = bond.Begin.Point2D.Value;
                Vector2 end = bond.End.Point2D.Value;
                if (beg.X > end.X)
                {
                    Vector2 tmp = beg;
                    beg = end;
                    end = tmp;
                }
                Vector2 vec = new Vector2(end.X - beg.X, end.Y - beg.Y);
                double angle = Math.Atan2(vec.Y, vec.X);

                if (Math.Abs(angle) - ref_ < diff)
                {
                    count++;
                }
            }
            return count;
        }
        private double AdjustForHydrogen(IAtom atom, IAtomContainer mol)
        {
            var hcnt = atom.ImplicitHydrogenCount;
            if (hcnt == null || hcnt == 0)
                return 0;
            var bonds = mol.GetConnectedBonds(atom);

            int pos = 0; // right

            // isolated atoms, HCl vs NH4+ etc
            if (!bonds.Any())
            {
                Elements elem = Elements.OfNumber(atom.AtomicNumber.Value);
                // see HydrogenPosition for canonical list
                switch (elem.AtomicNumber)
                {
                    case Elements.O.Oxygen:
                    case Elements.O.Sulfur:
                    case Elements.O.Selenium:
                    case Elements.O.Tellurium:
                    case Elements.O.Fluorine:
                    case Elements.O.Chlorine:
                    case Elements.O.Bromine:
                    case Elements.O.Iodine:
                        pos = -1; // left
                        break;
                    default:
                        pos = +1; // right
                        break;
                }
            }
            else if (bonds.Count() == 1)
            {
                IAtom other = bonds.First().GetOther(atom);
                double deltaX = atom.Point2D.Value.X - other.Point2D.Value.X;
                if (Math.Abs(deltaX) > 0.05)
                    pos = (int)Math.Sign(deltaX);
            }
            return pos * (BondLength / 2);
        }

        /// <summary>
        /// Similar to the method <see cref="GeometryUtil.GetMinMax(IAtomContainer)"/> but considers
        /// heteroatoms with hydrogens.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>the min/max x and y bounds</returns>
        private double[] GetAprxBounds(IAtomContainer mol)
        {
            double maxX = -double.MaxValue;
            double maxY = -double.MaxValue;
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            IAtom[] boundedAtoms = new IAtom[4];
            for (int i = 0; i < mol.Atoms.Count; i++)
            {
                IAtom atom = mol.Atoms[i];
                if (atom.Point2D != null)
                {
                    if (atom.Point2D.Value.X < minX)
                    {
                        minX = atom.Point2D.Value.X;
                        boundedAtoms[0] = atom;
                    }
                    if (atom.Point2D.Value.Y < minY)
                    {
                        minY = atom.Point2D.Value.Y;
                        boundedAtoms[1] = atom;
                    }
                    if (atom.Point2D.Value.X > maxX)
                    {
                        maxX = atom.Point2D.Value.X;
                        boundedAtoms[2] = atom;
                    }
                    if (atom.Point2D.Value.Y > maxY)
                    {
                        maxY = atom.Point2D.Value.Y;
                        boundedAtoms[3] = atom;
                    }
                }
            }
            double[] minmax = new double[4];
            minmax[0] = minX;
            minmax[1] = minY;
            minmax[2] = maxX;
            minmax[3] = maxY;
            double minXAdjust = AdjustForHydrogen(boundedAtoms[0], mol);
            double maxXAdjust = AdjustForHydrogen(boundedAtoms[1], mol);
            if (minXAdjust < 0) minmax[0] += minXAdjust;
            if (maxXAdjust > 0) minmax[1] += maxXAdjust;
            return minmax;
        }

        private void GenerateFragmentCoordinates(IAtomContainer mol, IList<IAtomContainer> frags)
        {
            var ionicBonds = MakeIonicBonds(frags);

            if (ionicBonds.Any())
            {
                // add tmp bonds and re-fragment
                int rollback = mol.Bonds.Count;
                foreach (var bond in ionicBonds)
                    mol.Bonds.Add(bond);
                frags = ConnectivityChecker.PartitionIntoMolecules(mol);

                // rollback temporary bonds
                int numBonds = mol.Bonds.Count;
                while (numBonds-- > rollback)
                    mol.Bonds.RemoveAt(numBonds);
            }

            var limits = new List<double[]>();
            int numFragments = frags.Count;

            // avoid overwriting our state
            ISet<IAtom> afixbackup = new HashSet<IAtom>(afix);
            ISet<IBond> bfixbackup = new HashSet<IBond>(bfix);

            // generate the sub-layouts
            foreach (IAtomContainer fragment in frags)
            {
                SetMolecule(fragment, false, afix, bfix);
                GenerateCoordinates(DefaultBondVector, true, true);
                LengthenIonicBonds(ionicBonds, fragment);
                limits.Add(GetAprxBounds(fragment));
            }

            // restore
            afix = afixbackup;
            bfix = bfixbackup;

            int nRow = (int)Math.Floor(Math.Sqrt(numFragments));
            int nCol = (int)Math.Ceiling(numFragments / (double)nRow);

            double[] xOffsets = new double[nCol + 1];
            double[] yOffsets = new double[nRow + 1];

            // calc the max widths/height of each row, we also add some
            // spacing
            double spacing = BondLength;
            for (int i = 0; i < numFragments; i++)
            {
                // +1 because first offset is always 0
                int col = 1 + i % nCol;
                int row = 1 + i / nCol;

                double[] minmax = limits[i];
                double width = spacing + (minmax[2] - minmax[0]);
                double height = spacing + (minmax[3] - minmax[1]);

                if (width > xOffsets[col])
                    xOffsets[col] = width;
                if (height > yOffsets[row])
                    yOffsets[row] = height;
            }

            // cumulative counts
            for (int i = 1; i < xOffsets.Length; i++)
                xOffsets[i] += xOffsets[i - 1];
            for (int i = 1; i < yOffsets.Length; i++)
                yOffsets[i] += yOffsets[i - 1];

            // translate the molecules, note need to flip y axis
            var fragsEnumerator = frags.GetEnumerator();
            for (int i = 0; i < limits.Count; i++)
            {
                fragsEnumerator.MoveNext();
                int row = nRow - (i / nCol) - 1;
                int col = i % nCol;
                Vector2 dest = new Vector2((xOffsets[col] + xOffsets[col + 1]) / 2,
                                           (yOffsets[row] + yOffsets[row + 1]) / 2);
                double[] minmax = limits[i];
                Vector2 curr = new Vector2((minmax[0] + minmax[2]) / 2, (minmax[1] + minmax[3]) / 2);
                GeometryUtil.Translate2D(fragsEnumerator.Current,
                                         dest.X - curr.X, dest.Y - curr.Y);
            }

            // correct double-bond stereo, this changes the layout and in reality
            // should be done during the initial placement
            if (mol.StereoElements.Any())
                CorrectGeometricConfiguration.Correct(mol);

            // finalize
            AssignStereochem(mol);
            FinalizeLayout(mol);
        }

        private void LengthenIonicBonds(IList<IBond> ionicBonds, IAtomContainer fragment)
        {
            IChemObjectBuilder bldr = fragment.Builder;

            if (!ionicBonds.Any())
                return;

            IAtomContainer newfrag = bldr.NewAtomContainer();
            IAtom[] atoms = new IAtom[fragment.Atoms.Count];
            for (int i = 0; i < atoms.Length; i++)
                atoms[i] = fragment.Atoms[i];
            newfrag.SetAtoms(atoms);

            foreach (IBond bond in fragment.Bonds)
            {
                if (!ionicBonds.Contains(bond))
                {
                    newfrag.Bonds.Add(bond);
                }
                else
                {
                    int numBegIonic = bond.Begin.GetProperty<int>("ionicDegree", 0);
                    int numEndIonic = bond.End.GetProperty<int>("ionicDegree", 0);
                    numBegIonic++;
                    numEndIonic++;
                    bond.Begin.SetProperty("ionicDegree", numBegIonic);
                    bond.End.SetProperty("ionicDegree", numEndIonic);
                }
            }

            if (newfrag.Bonds.Count == fragment.Bonds.Count)
                return;

            var subfragments = ConnectivityChecker.PartitionIntoMolecules(newfrag);
            IDictionary<IAtom, IAtomContainer> atomToFrag = new Dictionary<IAtom, IAtomContainer>();

            // index atom->fragment
            foreach (IAtomContainer subfragment in subfragments)
                foreach (IAtom atom in subfragment.Atoms)
                    atomToFrag[atom] = subfragment;

            foreach (IBond bond in ionicBonds)
            {
                IAtom beg = bond.Begin;
                IAtom end = bond.End;

                // select which bond to stretch from
                int? numBegIonic = bond.Begin.GetProperty<int?>("ionicDegree");
                int? numEndIonic = bond.End.GetProperty<int?>("ionicDegree");
                if (numBegIonic == null || numEndIonic == null)
                    continue;
                if (numBegIonic > numEndIonic)
                {
                    IAtom tmp = beg;
                    beg = end;
                    end = tmp;
                }
                else if (numBegIonic.Equals(numEndIonic) && numBegIonic > 1)
                {
                    // can't stretch these
                    continue;
                }

                IAtomContainer endFrags = bldr.NewAtomContainer();
                IAtomContainer begFrag;
                if (!atomToFrag.TryGetValue(beg, out begFrag))
                    continue;
                foreach (IAtomContainer mol in subfragments)
                {
                    if (mol != begFrag)
                        endFrags.Add(mol);
                }
                double dx = end.Point2D.Value.X - beg.Point2D.Value.X;
                double dy = end.Point2D.Value.Y - beg.Point2D.Value.Y;
                Vector2 bondVec = new Vector2(dx, dy);
                bondVec = Vector2.Normalize(bondVec);
                bondVec *= BondLength / 2; // 1.5 bond length
                GeometryUtil.Translate2D(endFrags, bondVec);
            }
        }

        /// <summary>
        /// Property to cache the charge of a fragment.
        /// </summary>
        private const string FragmentCharge = "FragmentCharge";

        /// <summary>
        /// Merge fragments with duplicate atomic ions (e.g. [Na+].[Na+].[Na+]) into
        /// single fragments.
        /// </summary>
        /// <param name="frags">input fragments (all connected)</param>
        /// <returns>the merge ions</returns>
        private List<IAtomContainer> MergeAtomicIons(IEnumerable<IAtomContainer> frags)
        {
            var res = new List<IAtomContainer>();
            foreach (var frag in frags)
            {
                IChemObjectBuilder bldr = frag.Builder;

                if (frag.Bonds.Count > 0 || res.Count == 0)
                {
                    res.Add(bldr.NewAtomContainer(frag));
                }
                else
                {
                    // try to find matching atomic ion
                    int i = 0;
                    while (i < res.Count)
                    {
                        IAtom iAtm = frag.Atoms[0];
                        if (res[i].Bonds.Count == 0)
                        {
                            IAtom jAtm = res[i].Atoms[0];
                            if (NullAsZero(iAtm.FormalCharge) == NullAsZero(jAtm.FormalCharge) &&
                                NullAsZero(iAtm.AtomicNumber) == NullAsZero(jAtm.AtomicNumber) &&
                                NullAsZero(iAtm.ImplicitHydrogenCount) == NullAsZero(jAtm.ImplicitHydrogenCount))
                            {
                                break;
                            }
                        }
                        i++;
                    }

                    if (i < res.Count)
                    {
                        res[i].Add(frag);
                    }
                    else
                    {
                        res.Add(bldr.NewAtomContainer(frag));
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Select ions from a charged fragment. Ions not in charge separated
        /// bonds are favoured but select if needed. If an atom has lost or
        /// gained more than one electron it is added mutliple times to the
        /// output list
        /// </summary>
        /// <param name="frag">charged fragment</param>
        /// <param name="sign">the charge sign to select (+1 : cation, -1: anion)</param>
        /// <returns>the select atoms (includes duplicates)</returns>
        private List<IAtom> SelectIons(IAtomContainer frag, int sign)
        {
            int fragChg = frag.GetProperty<int?>(FragmentCharge).Value;
            Trace.Assert(Math.Sign(fragChg) == sign);
            var atoms = new List<IAtom>();

            FIRST_PASS:
            foreach (var atom in frag.Atoms)
            {
                if (fragChg == 0)
                    break;
                int atmChg = NullAsZero(atom.FormalCharge);
                if (Math.Sign(atmChg) == sign)
                {

                    // skip in first pass if charge separated
                    foreach (var bond in frag.GetConnectedBonds(atom))
                    {
                        if (Math.Sign(NullAsZero(bond.GetOther(atom).FormalCharge)) + sign == 0)
                            goto continue_FIRST_PASS;
                    }

                    while (fragChg != 0 && atmChg != 0)
                    {
                        atoms.Add(atom);
                        atmChg -= sign;
                        fragChg -= sign;
                    }
                }
                continue_FIRST_PASS:
                ;
            }

            if (fragChg == 0)
                return atoms;

            foreach (var atom in frag.Atoms)
            {
                if (fragChg == 0)
                    break;
                int atmChg = NullAsZero(atom.FormalCharge);
                if (Math.Sign(atmChg) == sign)
                {
                    while (fragChg != 0 && atmChg != 0)
                    {
                        atoms.Add(atom);
                        atmChg -= sign;
                        fragChg -= sign;
                    }
                }
            }

            return atoms;
        }

        /// <summary>
        /// Alternative method name "Humpty Dumpty" (a la. R Sayle).
        /// </summary>
        /// <remarks>
        /// (Re)bonding of ionic fragments for improved layout. This method takes a list
        /// of two or more fragments and creates zero or more bonds (return value) that
        /// should be temporarily used for layout generation. In general this problem is
        /// difficult but since molecules will be laid out in a grid by default - any
        /// positioning is an improvement. Heuristics could be added if bad (re)bonds
        /// are seen.
        /// </remarks>
        /// <param name="frags">connected fragments</param>
        /// <returns>ionic bonds to make</returns>
        private IList<IBond> MakeIonicBonds(IEnumerable<IAtomContainer> frags)
        {
            Trace.Assert(frags.Count() > 1);

            // merge duplicates together, e.g. [H-].[H-].[H-].[Na+].[Na+].[Na+]
            // would be two needsMerge fragments. We currently only do single
            // atoms but in theory could also do larger ones
            var mergedFrags = MergeAtomicIons(frags);
            var posFrags = new List<IAtomContainer>();
            var negFrags = new List<IAtomContainer>();

            int chgSum = 0;
            foreach (var frag in mergedFrags)
            {
                int chg = 0;
                foreach (var atom in frag.Atoms)
                    chg += NullAsZero(atom.FormalCharge);
                chgSum += chg;
                frag.SetProperty(FragmentCharge, chg);
                if (chg < 0)
                    negFrags.Add(frag);
                else if (chg > 0)
                    posFrags.Add(frag);
            }

            // non-neutral or we only have one needsMerge fragment?
            if (chgSum != 0 || mergedFrags.Count == 1)
                return new IBond[0];

            List<IAtom> cations = new List<IAtom>();
            List<IAtom> anions = new List<IAtom>();

            // trivial case
            if (posFrags.Count == 1 && negFrags.Count == 1)
            {
                cations.AddRange(SelectIons(posFrags[0], +1));
                anions.AddRange(SelectIons(negFrags[0], -1));
            }
            else
            {
                // greedy selection
                posFrags.Sort(AFragmentChargeComparer);
                negFrags.Sort(AFragmentChargeComparer);

                foreach (var posFrag in posFrags)
                    cations.AddRange(SelectIons(posFrag, +1));
                foreach (var negFrag in negFrags)
                    anions.AddRange(SelectIons(negFrag, -1));
            }

            if (cations.Count != anions.Count && cations.Count == 0)
                return new IBond[0];

            IChemObjectBuilder bldr = frags.First().Builder;

            // make the bonds
            var ionicBonds = new List<IBond>(cations.Count);
            for (int i = 0; i < cations.Count; i++)
            {
                IAtom beg = cations[i];
                IAtom end = anions[i];

                bool unique = true;
                foreach (var bond in ionicBonds)
                    if (bond.Begin.Equals(beg) && bond.End.Equals(end) ||
                        bond.End.Equals(beg) && bond.Begin.Equals(end))
                        unique = false;

                if (unique)
                    ionicBonds.Add(bldr.NewBond(beg, end));
            }

            // we could merge the fragments here using union-find structures
            // but it's much simpler (and probably more efficient) to return
            // the new bonds and re-fragment the molecule with these bonds added.

            return ionicBonds;
        }

        static FragmentChargeComparer AFragmentChargeComparer { get; } = new FragmentChargeComparer();
        // sort hi->lo fragment charge, if same charge then we put smaller
        // fragments (bond count) before in cations and after in anions
        class FragmentChargeComparer : IComparer<IAtomContainer>
        {
            public int Compare(IAtomContainer a, IAtomContainer b)
            {
                int qA = a.GetProperty<int>(FragmentCharge);
                int qB = b.GetProperty<int>(FragmentCharge);
                int cmp = Math.Abs(qA).CompareTo(Math.Abs(qB));
                if (cmp != 0) return cmp;
                int sign = Math.Sign(qA);
                return (sign * a.Bonds.Count).CompareTo(sign * b.Bonds.Count);
            }
        }

        /// <summary>
        /// Utility - safely access Object Integers as primitives, when we want the
        /// default value of null to be zero.
        /// </summary>
        /// <param name="x">number</param>
        /// <returns>the number primitive or zero if null</returns>
        private static int NullAsZero(int? x) => x ?? 0;

        /// <summary>
        /// The main method of this StructurDiagramGenerator. Assign a molecule to the
        /// StructurDiagramGenerator, call the GenerateCoordinates() method and get
        /// your molecule back.
        /// </summary>
        /// <exception cref="CDKException">if an error occurs</exception>
        public void GenerateCoordinates()
        {
            GenerateCoordinates(DefaultBondVector);
        }

        /// <summary>
        /// Using a fast identity template library, lookup the the ring system and assign coordinates.
        /// The method indicates whether a match was found and coordinates were assigned.
        /// </summary>
        /// <param name="rs">the ring set</param>
        /// <param name="molecule">the rest of the compound</param>
        /// <param name="anon">check for anonmised templates</param>
        /// <returns>coordinates were assigned</returns>
        private bool LookupRingSystem(IRingSet rs, IAtomContainer molecule, bool anon)
        {
            // identity templates are disabled
            if (!UseIdentityTemplates) return false;

            IChemObjectBuilder bldr = molecule.Builder;

            IAtomContainer ringSystem = bldr.NewAtomContainer();
            foreach (var container in rs)
                ringSystem.Add(container);

            ISet<IAtom> ringAtoms = new HashSet<IAtom>();
            foreach (var atom in ringSystem.Atoms)
                ringAtoms.Add(atom);

            // a temporary molecule of the ring system and 'stubs' of the attached substituents
            IAtomContainer ringWithStubs = bldr.NewAtomContainer();
            ringWithStubs.Add(ringSystem);
            foreach (var bond in molecule.Bonds)
            {
                IAtom atom1 = bond.Begin;
                IAtom atom2 = bond.End;
                if (IsHydrogen(atom1) || IsHydrogen(atom2)) continue;
                if (ringAtoms.Contains(atom1) ^ ringAtoms.Contains(atom2))
                {
                    ringWithStubs.Bonds.Add(bond);
                    ringWithStubs.Atoms.Add(atom1);
                    ringWithStubs.Atoms.Add(atom2);
                }
            }

            // Three levels of identity to check are as follows:
            //   Level 1 - check for a skeleton ring system and attached substituents
            //   Level 2 - check for a skeleton ring system
            //   Level 3 - check for an anonymous ring system
            // skeleton = all single bonds connecting different elements
            // anonymous = all single bonds connecting carbon
            IAtomContainer skeletonStub = ClearHydrogenCounts(AtomContainerManipulator.Skeleton(ringWithStubs));
            IAtomContainer skeleton = ClearHydrogenCounts(AtomContainerManipulator.Skeleton(ringSystem));
            IAtomContainer anonymous = ClearHydrogenCounts(AtomContainerManipulator.Anonymise(ringSystem));

            foreach (var container in new[] { skeletonStub, skeleton, anonymous })
            {
                if (!anon && container == anonymous)
                    continue;

                // assign the atoms 0 to |ring|, the stubs are added at the end of the container
                // and are not placed here (since the index of each stub atom is > |ring|)
                if (identityLibrary.AssignLayout(container))
                {
                    for (int i = 0; i < ringSystem.Atoms.Count; i++)
                    {
                        IAtom atom = ringSystem.Atoms[i];
                        atom.Point2D = container.Atoms[i].Point2D;
                        atom.IsPlaced = true;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is an atom a hydrogen atom.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <returns>the atom is a hydrogen</returns>
        private static bool IsHydrogen(IAtom atom)
        {
            if (atom.AtomicNumber != null) return atom.AtomicNumber == 1;
            return "H".Equals(atom.Symbol);
        }

        /// <summary>
        /// Simple helper function that sets all hydrogen counts to 0.
        /// </summary>
        /// <param name="container">a structure representation</param>
        /// <returns>the input container</returns>
        private static IAtomContainer ClearHydrogenCounts(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
                atom.ImplicitHydrogenCount = 0;
            return container;
        }

        /// <summary>
        /// Layout a set of connected rings (ring set/ring system). 
        /// <para>
        /// Current Scheme:
        ///   1. Lookup the entire ring system for a known template.
        ///   2. If first (most complex) ring is macrocycle,
        ///      2a. Assign coordinates from macro cycle templates
        ///   3. If first is not-macrocycle (or currently doesn't match out templates)
        ///      3a. Layout as regular polygon
        ///   4. Sequentially connected layout rings <see cref="RingPlacer"/>
        ///  </para>
        /// </summary>
        /// <param name="firstBondVector">A vector giving the placement for the first bond</param>
        /// <param name="rs">The connected RingSet to layout</param>
        private int LayoutRingSet(Vector2 firstBondVector, IRingSet rs)
        {
            // sort small -> large
            // Get the most complex ring in this RingSet (largest prioritized)
            RingSetManipulator.Sort(rs);
            IRing first = RingSetManipulator.GetMostComplexRing(rs);

            bool macro = IsMacroCycle(first, rs);
            int result = 0;

            // Check for an exact match (identity) on the entire ring system
            // XXX: should avoid if we have db stereo in macrocycle!
            if (LookupRingSystem(rs, molecule, rs.Count > 1))
            {
                foreach (var container in rs)
                    container.IsPlaced = true;
                rs.IsPlaced = true;
                return macro ? 2 : 1;
            }
            else
            {
                // attempt ring peeling and retemplate
                IRingSet core = GetRingSetCore(rs);
                if (core.Count > 0 &&
                  core.Count < rs.Count &&
                  LookupRingSystem(core, molecule, !macro || rs.Count > 1))
                {
                    foreach (var container in core)
                        container.IsPlaced = true;
                }
            }

            // Place the most complex ring at the origin of the coordinate system
            if (!first.IsPlaced)
            {
                IAtomContainer sharedAtoms = PlaceFirstBond(first.Bonds[0], firstBondVector);
                if (!macro || !macroPlacer.Layout(first, rs))
                {
                    // de novo layout of ring as a regular polygon
                    Vector2 ringCenterVector = ringPlacer.GetRingCenterOfFirstRing(first, firstBondVector, BondLength);
                    ringPlacer.PlaceRing(first, sharedAtoms, GeometryUtil.Get2DCenter(sharedAtoms), ringCenterVector, BondLength);
                }
                else
                {
                    result = 2;
                }
                first.IsPlaced = true;
            }

            // hint to RingPlacer
            if (macro)
            {
                foreach (var ring in rs)
                    ring.SetProperty(RingPlacer.SnapHint, true);
            }

            {
                // Place all connected rings start with those connected to first
                int thisRing = 0;
                IRing ring = first;
                do
                {
                    if (ring.IsPlaced)
                    {
                        ringPlacer.PlaceConnectedRings(rs, ring, RingPlacer.Fused, BondLength);
                        ringPlacer.PlaceConnectedRings(rs, ring, RingPlacer.Bridged, BondLength);
                        ringPlacer.PlaceConnectedRings(rs, ring, RingPlacer.Spiro, BondLength);
                    }
                    thisRing++;
                    if (thisRing == rs.Count)
                    {
                        thisRing = 0;
                    }
                    ring = (IRing)rs[thisRing];
                } while (!AllPlaced(rs));
            }

            return result;
        }

        /// <summary>
        /// Peel back terminal rings to the complex 'core': <token>cdk-cite-Helson99</token>, <token>cdk-cite-Clark06</token>.
        /// </summary>
        /// <param name="rs">ring set</param>
        /// <returns>the ring set core</returns>
        private IRingSet GetRingSetCore(IRingSet rs)
        {
            var ringlookup = new MultiDictionary<IBond, IRing>();
            var ringsystem = new LinkedHashSet<IRing>();

            foreach (var ring in rs)
            {
                ringsystem.Add((IRing)ring);
                foreach (var bond in ring.Bonds)
                    ringlookup.Add(bond, (IRing)ring);
            }

            // iteratively reduce ring system by removing ring that only share one bond
            var toremove = new HashSet<IRing>();
            do
            {
                toremove.Clear();
                foreach (var ring in ringsystem)
                {
                    int numAttach = 0;
                    foreach (var bond in ring.Bonds)
                    {
                        foreach (var attached in ringlookup[bond])
                        {
                            if (attached != ring && ringsystem.Contains(attached))
                            {
                                numAttach++;
                                break;
                            }
                        }
                    }
                    if (numAttach <= 1)
                        toremove.Add(ring);
                }
                foreach (var item in toremove)
                    ringsystem.Remove(item);
            } while (toremove.Count != 0);

            IRingSet core = rs.Builder.NewRingSet();
            foreach (var ring in ringsystem)
                core.Add(ring);

            return core;
        }

        /// <summary>
        /// Check if a ring in a ring set is a macro cycle. We define this as a
        /// ring with >= 10 atom and has at least one bond that isn't contained
        /// in any other rings.
        /// </summary>
        /// <param name="ring">ring to check</param>
        /// <param name="rs">rest of ring system</param>
        /// <returns>ring is a macro cycle</returns>
        private bool IsMacroCycle(IRing ring, IChemObjectSet<IRing> rs)
        {
            if (ring.Atoms.Count < 10)
                return false;
            foreach (var bond in ring.Bonds)
            {
                bool found = false;
                foreach (var other in rs)
                {
                    if (ring == other)
                        continue;
                    if (other.Contains(bond))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Does a layout of all aliphatic parts connected to the parts of the molecule
        /// that have already been laid out. Starts at the first bond with unplaced
        /// neighbours and stops when a ring is encountered.
        /// </summary>
        /// <exception cref="CDKException">if an error occurs</exception>
        private void LayoutAcyclicParts()
        {
            Debug.WriteLine("Start of handleAliphatics");

            int safetyCounter = 0;
            IAtomContainer unplacedAtoms = null;
            IAtomContainer placedAtoms = null;
            IAtomContainer longestUnplacedChain = null;
            IAtom atom = null;

            Vector2 direction;
            Vector2 startVector;
            bool done;
            do
            {
                safetyCounter++;
                done = false;
                atom = GetNextAtomWithAliphaticUnplacedNeigbors();
                if (atom != null)
                {
                    unplacedAtoms = GetUnplacedAtoms(atom);
                    placedAtoms = GetPlacedAtoms(atom);

                    longestUnplacedChain = AtomPlacer.GetLongestUnplacedChain(molecule, atom);

                    Debug.WriteLine("---start of longest unplaced chain---");
                    try
                    {
                        Debug.WriteLine("Start at atom no. " + (molecule.Atoms.IndexOf(atom) + 1));
                        Debug.WriteLine(AtomPlacer.ListNumbers(molecule, longestUnplacedChain));
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine(exc);
                    }
                    Debug.WriteLine("---end of longest unplaced chain---");

                    if (longestUnplacedChain.Atoms.Count > 1)
                    {
                        if (placedAtoms.Atoms.Count > 1)
                        {
                            Debug.WriteLine("More than one atoms placed already");
                            Debug.WriteLine("trying to place neighbors of atom " + (molecule.Atoms.IndexOf(atom) + 1));
                            atomPlacer.DistributePartners(atom, placedAtoms, GeometryUtil.Get2DCenter(placedAtoms),
                                                          unplacedAtoms, BondLength);
                            direction = longestUnplacedChain.Atoms[1].Point2D.Value;
                            startVector = atom.Point2D.Value;
                            direction -= startVector;
                            Debug.WriteLine("Done placing neighbors of atom " + (molecule.Atoms.IndexOf(atom) + 1));
                        }
                        else if(placedAtoms.Atoms.Count == 1)
                        {
                            Debug.WriteLine("Less than or equal one atoms placed already");
                            Debug.WriteLine("Trying to get next bond vector.");
                            direction = atomPlacer.GetNextBondVector(atom, placedAtoms.Atoms[0],
                                                                     GeometryUtil.Get2DCenter(molecule), true);

                        }
                        else
                        {
                            direction = firstBondVector;
                        }

                        for (int f = 1; f < longestUnplacedChain.Atoms.Count; f++)
                        {
                            longestUnplacedChain.Atoms[f].IsPlaced = false;
                        }
                        atomPlacer.PlaceLinearChain(longestUnplacedChain, direction, BondLength);
                    }
                    else
                    {
                        done = true;
                    }
                }
                else
                {
                    done = true;
                }
            } while (!done && safetyCounter <= molecule.Atoms.Count);

            Debug.WriteLine("End of handleAliphatics");
        }

        /// <summary>
        /// Does the layout for the next RingSystem that is connected to those parts of
        /// the molecule that have already been laid out. Finds the next ring with an
        /// unplaced ring atom and lays out this ring. Then lays out the ring substituents
        /// of this ring. Then moves and rotates the laid out ring to match the position
        /// of its attachment bond to the rest of the molecule.
        /// </summary>
        /// <exception cref="CDKException">if an error occurs</exception>
        private void LayoutCyclicParts()
        {
            Debug.WriteLine("Start of LayoutNextRingSystem()");

            ResetUnplacedRings();
            IAtomContainer placedAtoms = AtomPlacer.GetPlacedAtoms(molecule);
            Debug.WriteLine("Finding attachment bond to already placed part...");
            IBond nextRingAttachmentBond = GetNextBondWithUnplacedRingAtom();
            if (nextRingAttachmentBond != null)
            {
                Debug.WriteLine("...bond found.");

                // Get the chain and the ring atom that are connected to where we
                // are comming from. Both are connected by nextRingAttachmentBond.
                IAtom ringAttachmentAtom = GetRingAtom(nextRingAttachmentBond);
                IAtom chainAttachmentAtom = GetOtherBondAtom(ringAttachmentAtom, nextRingAttachmentBond);

                // Get ring system which ringAttachmentAtom is part of
                IRingSet nextRingSystem = GetRingSystemOfAtom(ringSystems, ringAttachmentAtom);

                // Get all rings of nextRingSytem as one IAtomContainer
                IAtomContainer ringSystem = RingSetManipulator.GetAllInOneContainer(nextRingSystem);

                // Save coordinates of ringAttachmentAtom and chainAttachmentAtom
                Vector2 oldRingAttachmentAtomPoint = ringAttachmentAtom.Point2D.Value;
                Vector2 oldChainAttachmentAtomPoint = chainAttachmentAtom.Point2D.Value;

                // Do the layout of the next ring system
                LayoutRingSet(firstBondVector, nextRingSystem);

                // Place all the substituents of next ring system
                AtomPlacer.MarkNotPlaced(placedAtoms);
                IAtomContainer placedRingSubstituents = ringPlacer.PlaceRingSubstituents(nextRingSystem, BondLength);
                ringSystem.Add(placedRingSubstituents);
                AtomPlacer.MarkPlaced(placedAtoms);

                // Move and rotate the laid out ring system to match the geometry of the attachment bond
                Debug.WriteLine("Computing translation/rotation of new ringset to fit old attachment bond orientation...");

                // old placed ring atom coordinate
                Vector2 oldPoint2 = oldRingAttachmentAtomPoint;
                // old placed substituent atom coordinate
                Vector2 oldPoint1 = oldChainAttachmentAtomPoint;

                // new placed ring atom coordinate
                Vector2 newPoint2 = ringAttachmentAtom.Point2D.Value;
                // new placed substituent atom coordinate
                Vector2 newPoint1 = chainAttachmentAtom.Point2D.Value;

                Debug.WriteLine("oldPoint1: " + oldPoint1);
                Debug.WriteLine("oldPoint2: " + oldPoint2);
                Debug.WriteLine("newPoint1: " + newPoint1);
                Debug.WriteLine("newPoint2: " + newPoint2);

                double oldAngle = GeometryUtil.GetAngle(oldPoint2.X - oldPoint1.X, oldPoint2.Y - oldPoint1.Y);
                double newAngle = GeometryUtil.GetAngle(newPoint2.X - newPoint1.X, newPoint2.Y - newPoint1.Y);
                double angleDiff = oldAngle - newAngle;

                Debug.WriteLine("oldAngle: " + oldAngle + ", newAngle: " + newAngle + "; diff = " + angleDiff);

                Vector2 translationVector = oldPoint1;
                translationVector -= newPoint1;

                // Move to fit old attachment bond orientation
                GeometryUtil.Translate2D(ringSystem, translationVector);

                // Rotate to fit old attachment bond orientation
                GeometryUtil.Rotate(ringSystem, oldPoint1, angleDiff);

                Debug.WriteLine("...done translating/rotating new ringset to fit old attachment bond orientation.");
            }
            else
            {
                Debug.WriteLine("...no bond found");

                // partially laid out ring system
                if (ringSystems != null)
                {
                    foreach (IRingSet ringset in ringSystems)
                    {
                        foreach (IAtomContainer ring in ringset)
                            ringPlacer.CompletePartiallyPlacedRing(ringset, (IRing)ring, BondLength);
                        if (AllPlaced(ringset))
                            ringPlacer.PlaceRingSubstituents(ringset, BondLength);
                    }
                }
            }

            Debug.WriteLine("End of LayoutNextRingSystem()");
        }

        /// <summary>
        /// Returns an AtomContainer with all unplaced atoms connected to a given atom
        /// </summary>
        /// <param name="atom">The Atom whose unplaced bonding partners are to be returned</param>
        /// <returns>an AtomContainer with all unplaced atoms connected to a given atom</returns>
        private IAtomContainer GetUnplacedAtoms(IAtom atom)
        {
            IAtomContainer unplacedAtoms = atom.Builder.NewAtomContainer();
            var bonds = molecule.GetConnectedBonds(atom);
            IAtom connectedAtom;
            foreach (var bond in bonds)
            {
                connectedAtom = bond.GetOther(atom);
                if (!connectedAtom.IsPlaced)
                {
                    unplacedAtoms.Atoms.Add(connectedAtom);
                }
            }
            return unplacedAtoms;
        }

        /// <summary>
        /// Returns an AtomContainer with all placed atoms connected to a given atom
        /// </summary>
        /// <param name="atom">The Atom whose placed bonding partners are to be returned</param>
        /// <returns>an AtomContainer with all placed atoms connected to a given atom</returns>
        private IAtomContainer GetPlacedAtoms(IAtom atom)
        {
            IAtomContainer placedAtoms = atom.Builder.NewAtomContainer();
            var bonds = molecule.GetConnectedBonds(atom);
            IAtom connectedAtom;
            foreach (var bond in bonds)
            {
                connectedAtom = bond.GetOther(atom);
                if (connectedAtom.IsPlaced)
                {
                    placedAtoms.Atoms.Add(connectedAtom);
                }
            }
            return placedAtoms;
        }

        /// <summary>
        /// Returns the next atom with unplaced aliphatic neighbors
        /// </summary>
        /// <returns>the next atom with unplaced aliphatic neighbors</returns>
        private IAtom GetNextAtomWithAliphaticUnplacedNeigbors()
        {
            foreach (var bond in molecule.Bonds)
            {
                if (bond.End.IsPlaced && !bond.Begin.IsPlaced)
                {
                    return bond.End;
                }

                if (bond.Begin.IsPlaced && !bond.End.IsPlaced)
                {
                    return bond.Begin;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the next bond with an unplaced ring atom
        /// </summary>
        /// <returns>the next bond with an unplaced ring atom</returns>
        private IBond GetNextBondWithUnplacedRingAtom()
        {
            foreach (IBond bond in molecule.Bonds)
            {
                IAtom beg = bond.Begin;
                IAtom end = bond.End;
                if (beg.Point2D != null && end.Point2D != null)
                {
                    if (end.IsPlaced && !beg.IsPlaced && beg.IsInRing)
                    {
                        return bond;
                    }
                    if (beg.IsPlaced && !end.IsPlaced && end.IsInRing)
                    {
                        return bond;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Places the first bond of the first ring such that one atom is at (0,0) and
        /// the other one at the position given by bondVector
        /// </summary>
        /// <param name="bondVector">A 2D vector to point to the position of the second bond atom</param>
        /// <param name="bond">the bond to lay out</param>
        /// <returns>an IAtomContainer with the atoms of the bond and the bond itself</returns>
        private IAtomContainer PlaceFirstBond(IBond bond, Vector2 bondVector)
        {
            IAtomContainer sharedAtoms = null;
            try
            {
                bondVector = Vector2.Normalize(bondVector);
                Debug.WriteLine($"placeFirstBondOfFirstRing->bondVector.Length: {bondVector.Length()}");
                bondVector *= BondLength;
                Debug.WriteLine($"placeFirstBondOfFirstRing->bondVector.Length after scaling:{bondVector.Length()}");
                IAtom atom;
                Vector2 point = Vector2.Zero;
                atom = bond.Begin;
                Debug.WriteLine("Atom 1 of first Bond: " + (molecule.Atoms.IndexOf(atom) + 1));
                atom.Point2D = point;
                atom.IsPlaced = true;
                point = Vector2.Zero;
                atom = bond.End;
                Debug.WriteLine("Atom 2 of first Bond: " + (molecule.Atoms.IndexOf(atom) + 1));
                point += bondVector;
                atom.Point2D = point;
                atom.IsPlaced = true;

                // The new ring is layed out relativ to some shared atoms that have
                // already been placed. Usually this is another ring, that has
                // already been draw and to which the new ring is somehow connected,
                // or some other system of atoms in an aliphatic chain. In this
                // case, it's the first bond that we layout by hand.
                sharedAtoms = atom.Builder.NewAtomContainer();
                sharedAtoms.Bonds.Add(bond);
                sharedAtoms.Atoms.Add(bond.Begin);
                sharedAtoms.Atoms.Add(bond.End);
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc);
            }
            return sharedAtoms;
        }

        /// <summary>
        /// Are all rings in the Vector placed?
        /// </summary>
        /// <param name="rings">The Vector to be checked</param>
        /// <returns>true if all rings are placed, false otherwise</returns>
        private bool AllPlaced(IRingSet rings)
        {
            for (int f = 0; f < rings.Count; f++)
            {
                if (!((IRing)rings[f]).IsPlaced)
                {
                    Debug.WriteLine("allPlaced->Ring " + f + " not placed");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get the unplaced ring atom in this bond
        /// </summary>
        /// <param name="bond">the bond to be search for the unplaced ring atom</param>
        /// <returns>the unplaced ring atom in this bond</returns>
        private IAtom GetRingAtom(IBond bond)
        {
            if (bond.Begin.IsInRing && !bond.Begin.IsPlaced)
            {
                return bond.Begin;
            }
            if (bond.End.IsInRing && !bond.End.IsPlaced)
            {
                return bond.End;
            }
            return null;
        }

        /// <summary>
        /// Get the ring system of which the given atom is part of
        /// </summary>
        /// <param name="ringSystems">a List of ring systems to be searched</param>
        /// <param name="ringAtom">the ring atom to be search in the ring system.</param>
        /// <returns>the ring system the given atom is part of</returns>
        private IRingSet GetRingSystemOfAtom(IList<IRingSet> ringSystems, IAtom ringAtom)
        {
            IRingSet ringSet = null;
            for (int f = 0; f < ringSystems.Count; f++)
            {
                ringSet = (IRingSet)ringSystems[f];
                if (ringSet.Contains(ringAtom))
                {
                    return ringSet;
                }
            }
            return null;
        }

        /// <summary>
        /// Set all the atoms in unplaced rings to be unplaced
        /// </summary>
        private void ResetUnplacedRings()
        {
            IRing ring = null;
            if (sssr == null)
            {
                return;
            }
            int unplacedCounter = 0;
            for (int f = 0; f < sssr.Count; f++)
            {
                ring = (IRing)sssr[f];
                if (!ring.IsPlaced)
                {
                    Debug.WriteLine("Ring with " + ring.Atoms.Count + " atoms is not placed.");
                    unplacedCounter++;
                    for (int g = 0; g < ring.Atoms.Count; g++)
                    {
                        ring.Atoms[g].IsPlaced = false;
                    }
                }
            }
            Debug.WriteLine("There are " + unplacedCounter + " unplaced Rings.");
        }

        /// <summary>
        /// Returns the other atom of the bond.
        /// Expects bond to have only two atoms.
        /// Returns null if the given atom is not part of the given bond.
        /// </summary>
        /// <param name="atom">the atom we already have</param>
        /// <param name="bond">the bond</param>
        /// <returns>the other atom of the bond</returns>
        public IAtom GetOtherBondAtom(IAtom atom, IBond bond)
        {
            if (!bond.Contains(atom)) return null;
            if (bond.Begin.Equals(atom))
                return bond.End;
            else
                return bond.Begin;
        }

        /// <summary>
        /// Multiple groups need special placement by overlaying the repeat part coordinates.
        /// </summary>
        /// coordinates on each other.
        /// <param name="mol">molecule to place the multiple groups of</param>
        private void PlaceMultipleGroups(IAtomContainer mol)
        {
            var sgroups = mol.GetProperty<IList<Sgroup>>(CDKPropertyName.CtabSgroups);
            if (sgroups == null)
                return;
            var multipleGroups = new List<Sgroup>();
            foreach (var sgroup in sgroups)
            {
                if (sgroup.Type == SgroupType.CtabMultipleGroup)
                    multipleGroups.Add(sgroup);
            }
            if (multipleGroups.Count == 0)
                return;

            int[][] adjlist = GraphUtil.ToAdjList(mol);
            var idxs = new Dictionary<IAtom, int>();
            foreach (var atom in mol.Atoms)
                idxs[atom] = idxs.Count;

            foreach (var sgroup in multipleGroups)
            {
                int numCrossing = sgroup.Bonds.Count;
                if (numCrossing != 0 && numCrossing != 2)
                    continue;

                // extract substructure
                IAtomContainer substructure = mol.Builder.NewAtomContainer();
                var visit = new HashSet<IAtom>();
                var patoms = (ICollection<IAtom>)sgroup.GetValue(SgroupKeys.CtabParentAtomList);
                if (patoms == null)
                    continue;
                foreach (var atom in patoms)
                {
                    substructure.Atoms.Add(atom);
                    visit.Add(atom);
                }
                foreach (var bond in mol.Bonds)
                {
                    IAtom beg = bond.Begin;
                    IAtom end = bond.End;
                    if (visit.Contains(beg) && visit.Contains(end))
                        substructure.Bonds.Add(bond);
                }

                // advanced API usage, we make a set that only includes the atoms we want to match
                // and use this in a custom AtomMatcher to skip matches we don't want and update as
                // we go
                foreach (var atom in sgroup.Atoms)
                    visit.Add(atom);

                Pattern ptrn = VentoFoggia.FindSubstructure(substructure, new AtomicNumberAtomMatcher(visit), BondMatcher.CreateOrderMatcher());

                var sgroupAtoms = sgroup.Atoms;

                // when there are crossing bonds, things are more tricky as
                // we need to translate connected parts
                List<KeyValuePair<Vector2, Vector2>> outgoing = new List<KeyValuePair<Vector2, Vector2>>();
                List<KeyValuePair<IBond, Vector2>> xBondVec = new List<KeyValuePair<IBond, Vector2>>();
                if (numCrossing == 2)
                {
                    foreach (var bond in mol.Bonds)
                    {
                        IAtom beg = bond.Begin;
                        IAtom end = bond.End;
                        if (patoms.Contains(beg) == patoms.Contains(end))
                            continue;
                        if (patoms.Contains(beg))
                        {
                            outgoing.Add(new KeyValuePair<Vector2, Vector2>(
                                beg.Point2D.Value,
                                new Vector2(end.Point2D.Value.X - beg.Point2D.Value.X,
                                end.Point2D.Value.Y - beg.Point2D.Value.Y)));
                        }
                        else
                        {
                            outgoing.Add(new KeyValuePair<Vector2, Vector2>(
                                end.Point2D.Value,
                                new Vector2(beg.Point2D.Value.X - end.Point2D.Value.X,
                                beg.Point2D.Value.Y - end.Point2D.Value.Y)));
                        }
                    }
                    foreach (var bond in sgroup.Bonds)
                    {
                        IAtom beg = bond.Begin;
                        IAtom end = bond.End;
                        if (sgroupAtoms.Contains(beg))
                        {
                            xBondVec.Add(new KeyValuePair<IBond, Vector2>(bond,
                                new Vector2(
                                    end.Point2D.Value.X - beg.Point2D.Value.X,
                                    end.Point2D.Value.Y - beg.Point2D.Value.Y)));
                        }
                        else
                        {
                            xBondVec.Add(new KeyValuePair<IBond, Vector2>(
                                bond,
                                new Vector2(
                                    beg.Point2D.Value.X - end.Point2D.Value.X,
                                    beg.Point2D.Value.Y - end.Point2D.Value.Y)));
                        }
                    }
                }

                // no crossing bonds is easy just map the repeat part and transfer coordinates
                foreach (var patom in patoms)
                    visit.Remove(patom); // don't need to map parent
                foreach (var atoms in ptrn.MatchAll(mol).GetUniqueAtoms().ToAtomMap())
                {
                    foreach (var e in atoms)
                    {
                        e.Value.Point2D = e.Key.Point2D;
                    }
                    // search is lazy so can update the matcher before the next match
                    // is found (implementation ninja)
                    foreach (var v in atoms.Values)
                        visit.Remove(v);
                }

                // reposition
                Trace.Assert(xBondVec.Count == outgoing.Count);
                foreach (var e in xBondVec)
                {
                    IBond bond = e.Key;

                    // can't fix move ring bonds
                    if (bond.IsInRing)
                        continue;

                    IAtom beg = sgroupAtoms.Contains(bond.Begin) ? bond.Begin : bond.End;
                    KeyValuePair<Vector2, Vector2>? best = null;
                    foreach (var candidate in outgoing)
                    {
                        if (best == null || Vector2.Distance(candidate.Key, beg.Point2D.Value) < Vector2.Distance(best.Value.Key, beg.Point2D.Value))
                            best = candidate;
                    }
                    outgoing.Remove(best.Value);
                    Trace.Assert(best != null);

                    // visit rest of connected molecule
                    var iVisit = new HashSet<int>();
                    iVisit.Add(idxs[beg]);
                    Visit(iVisit, adjlist, idxs[bond.GetOther(beg)]);
                    iVisit.Remove(idxs[beg]);
                    IAtomContainer frag = mol.Builder.NewAtomContainer();
                    foreach (var idx in iVisit)
                        frag.Atoms.Add(mol.Atoms[idx]);

                    Vector2 orgVec = e.Value;
                    Vector2 newVec = best.Value.Value;

                    Vector2 endP = bond.GetOther(beg).Point2D.Value;
                    Vector2 newEndP = beg.Point2D.Value;
                    newEndP += newVec;

                    // need perpendicular dot product to get signed angle
                    double pDot = orgVec.X * newVec.Y - orgVec.Y * newVec.X;
                    double theta = Math.Atan2(pDot, Vector2.Dot(newVec, orgVec));

                    // position
                    GeometryUtil.Translate2D(frag, newEndP.X - endP.X, newEndP.Y - endP.Y);
                    GeometryUtil.Rotate(frag, bond.GetOther(beg).Point2D.Value, theta);
                }
            }
        }

        class AtomicNumberAtomMatcher : AtomMatcher
        {
            HashSet<IAtom> visit;

            public AtomicNumberAtomMatcher(HashSet<IAtom> visit)
            {
                this.visit = visit;
            }

            public override bool Matches(IAtom a, IAtom b)
            {
                if (!visit.Contains(b))
                    return false;
                int aElem = NullAsZero(a.AtomicNumber);
                int bElem = NullAsZero(b.AtomicNumber);
                if (aElem != bElem)
                    return false;
                int aChg = NullAsZero(a.FormalCharge);
                int bChg = NullAsZero(b.FormalCharge);
                if (aChg != bChg)
                    return false;
                int aMass = NullAsZero(a.MassNumber);
                int bMass = NullAsZero(b.MassNumber);
                if (aMass != bMass)
                    return false;
                int aHcnt = NullAsZero(a.ImplicitHydrogenCount);
                int bHcnt = NullAsZero(b.ImplicitHydrogenCount);
                if (aHcnt != bHcnt)
                    return false;
                return true;
            }
        }

        private void PlacePositionalVariation(IAtomContainer mol)
        {
            var sgroups = mol.GetProperty<IList<Sgroup>>(CDKPropertyName.CtabSgroups);
            if (sgroups == null)
                return;

            var mapping = AggregateMulticenterSgroups(sgroups);

            if (!mapping.Any())
                return;

            // helps with traversal
            GraphUtil.EdgeToBondMap bondMap = GraphUtil.EdgeToBondMap.WithSpaceFor(mol);
            int[][] adjlist = GraphUtil.ToAdjList(mol, bondMap);
            var idxs = new Dictionary<IAtom, int>();
            foreach (var atom in mol.Atoms)
                idxs[atom] = idxs.Count;

            foreach (var e in mapping)
            {
                var bonds = new LinkedHashSet<IBond>();

                IAtomContainer shared = mol.Builder.NewAtomContainer();
                foreach (var atom in e.Key)
                    shared.Atoms.Add(atom);
                Vector2 center = GeometryUtil.Get2DCenter(shared);

                foreach (var bond in mol.Bonds)
                {
                    if (e.Key.Contains(bond.Begin) && e.Key.Contains(bond.End))
                    {
                        bonds.Add(bond);
                    }
                }

                if (bonds.Count >= e.Value.Count)
                {
                    var begIter = e.Value.GetEnumerator();
                    var bndIter = bonds.GetEnumerator();

                    while (begIter.MoveNext() && bndIter.MoveNext())
                    {
                        IBond bond = bndIter.Current;
                        IAtom atom = begIter.Current;

                        if (NumRingBonds(mol, bond.Begin) > 2 && NumRingBonds(mol, bond.End) > 2)
                            continue;

                        Vector2 newBegP = bond.Begin.Point2D.Value;
                        Vector2 newEndP = bond.End.Point2D.Value;

                        Vector2 bndVec = new Vector2(newEndP.X - newBegP.X, newEndP.Y - newBegP.Y);
                        Vector2 bndXVec = new Vector2(-bndVec.Y, bndVec.X);

                        // ensure vector is pointing out of rings
                        Vector2 centerVec = new Vector2(center.X - ((newBegP.X + newEndP.X) / 2), center.Y - ((newBegP.Y + newEndP.Y) / 2));
                        if (Vector2.Dot(bndXVec, centerVec) > 0)
                        {
                            bndXVec = Vector2.Negate(bndXVec);
                        }

                        bndVec = Vector2.Normalize(bndVec);
                        bndXVec = Vector2.Normalize(bndXVec);

                        bndVec *= 0.5 * BondLength; // crossing point

                        double bndStep = (BondLength) / 5;

                        newBegP += bndVec;
                        bndXVec = Vector2.Normalize(bndXVec);
                        bndXVec *= 2 * bndStep;
                        newBegP -= bndXVec;
                        newEndP -= bndVec;
                        bndXVec = Vector2.Normalize(bndXVec);
                        bndXVec *= 3 * bndStep;
                        newEndP += bndXVec;

                        int atomIdx = idxs[atom];
                        if (adjlist[atomIdx].Length != 1)
                            continue;

                        // get all atoms connected to the part we will move
                        var visited = new HashSet<int>();
                        Visit(visited, adjlist, atomIdx);

                        // gather up other position group
                        HashSet<int> newvisit = new HashSet<int>();
                        do
                        {
                            newvisit.Clear();
                            foreach (var idx in visited)
                            {
                                IAtom visitedAtom = mol.Atoms[idx];
                                if (e.Key.Contains(visitedAtom) || e.Value.Contains(visitedAtom))
                                    continue;
                                foreach (var e2 in mapping.Entries)
                                {
                                    if (e2.Key.Contains(visitedAtom))
                                    {
                                        int other = idxs[e2.Value];
                                        if (!visited.Contains(other) && newvisit.Add(other))
                                        {
                                            Visit(newvisit, adjlist, other);
                                        }
                                    }
                                    else if (e2.Value == visitedAtom)
                                    {
                                        int other = idxs[e2.Key.First()];
                                        if (!visited.Contains(other) && newvisit.Add(other))
                                        {
                                            Visit(newvisit, adjlist, other);
                                        }
                                    }
                                }
                            }
                            foreach (var visit in newvisit)
                                visited.Add(visit);
                        } while (newvisit.Any());

                        IAtomContainer frag = mol.Builder.NewAtomContainer();
                        foreach (var visit in visited)
                            frag.Atoms.Add(mol.Atoms[visit]);

                        IBond attachBond = bondMap[atomIdx, adjlist[atomIdx][0]];
                        Vector2 begP = atom.Point2D.Value;
                        Vector2 endP = attachBond.GetOther(atom).Point2D.Value;

                        Vector2 orgVec = new Vector2(endP.X - begP.X, endP.Y - begP.Y);
                        Vector2 newVec = new Vector2(newEndP.X - newBegP.X, newEndP.Y - newBegP.Y);

                        // need perpendiculat dot product to get signed angle
                        double pDot = orgVec.X * newVec.Y - orgVec.Y * newVec.X;
                        double theta = Math.Atan2(pDot, Vector2.Dot(newVec, orgVec));

                        // position
                        GeometryUtil.Translate2D(frag, newBegP.X - begP.X, newBegP.Y - begP.Y);
                        GeometryUtil.Rotate(frag, atom.Point2D.Value, theta);

                        // stretch bond
                        frag.Atoms.Remove(atom);
                        GeometryUtil.Translate2D(frag, newEndP.X - endP.X, newEndP.Y - endP.Y);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Positional variation not yet handled");
                }
            }
        }

        private static void Visit(HashSet<int> visited, int[][] g, int v)
        {
            visited.Add(v);
            foreach (var w in g[v])
            {
                if (!visited.Contains(w))
                    Visit(visited, g, w);
            }
        }

        private static IMultiDictionary<ISet<IAtom>, IAtom> AggregateMulticenterSgroups(IList<Sgroup> sgroups)
        {
            var mapping = new MultiDictionary<ISet<IAtom>, IAtom>();
            foreach (var sgroup in sgroups)
            {
                if (sgroup.Type != SgroupType.ExtMulticenter)
                    continue;

                IAtom beg = null;
                var ends = new HashSet<IAtom>();

                var bonds = sgroup.Bonds;
                if (bonds.Count != 1)
                    continue;
                IBond bond = bonds.First();

                foreach (var atom in sgroup.Atoms)
                {
                    if (bond.Contains(atom))
                        beg = atom;
                    else
                        ends.Add(atom);
                }

                if (beg == null || ends.Count == 0)
                    continue;

                mapping.Add(ends, beg);
            }
            return mapping;
        }

        private static int NumRingBonds(IAtomContainer mol, IAtom atom)
        {
            int cnt = 0;
            foreach (var bond in mol.GetConnectedBonds(atom))
            {
                if (bond.IsInRing)
                    cnt++;
            }
            return cnt;
        }

        /// <summary>
        /// Place and update brackets for polymer Sgroups.
        /// </summary>
        /// <param name="mol">molecule</param>
        private void PlaceSgroupBrackets(IAtomContainer mol)
        {
            IList<Sgroup> sgroups = mol.GetProperty<IList<Sgroup>>(CDKPropertyName.CtabSgroups);
            if (sgroups == null) return;

            // index all crossing bonds
            var bondMap = new MultiDictionary<IBond, Sgroup>();
            var counter = new Dictionary<IBond, int>();
            foreach (var sgroup in sgroups)
            {
                if (!HasBrackets(sgroup))
                    continue;
                foreach (var bond in sgroup.Bonds)
                {
                    bondMap.Add(bond, sgroup);
                    counter[bond] = 0;
                }
            }
            sgroups = new List<Sgroup>(sgroups);
            // place child sgroups first
            ((List<Sgroup>)sgroups).Sort((o1, o2) =>
            {
                if (o1.Parents.Any() != o2.Parents.Any())
                {
                    if (!o1.Parents.Any())
                        return +1;
                    return -1;
                }
                return 0;
            });

            foreach (var sgroup in sgroups)
            {
                if (!HasBrackets(sgroup))
                    continue;

                var atoms = sgroup.Atoms;
                var xbonds = sgroup.Bonds;

                // clear all the existing brackets
                sgroup.PutValue(SgroupKeys.CtabBracket, null);

                // assign brackets to crossing bonds
                if (xbonds.Count >= 2)
                {
                    // check for vertical alignment
                    bool vert = true;
                    foreach (var bond in xbonds)
                    {
                        double theta = Angle(bond);
                        if (Math.Abs(Vectors.RadianToDegree(theta)) > 40 && Math.Abs(Vectors.RadianToDegree(theta)) < 140)
                        {
                            vert = false;
                            break;
                        }
                    }

                    foreach (var bond in xbonds)
                        sgroup.AddBracket(NewCrossingBracket(bond,
                                                             bondMap,
                                                             counter,
                                                             vert));
                }
                // <= 1 crossing bonds so simply wrap the entire fragment
                else
                {
                    IAtomContainer tmp = mol.Builder.NewAtomContainer();
                    foreach (var atom in atoms)
                        tmp.Atoms.Add(atom);
                    double[] minmax = GeometryUtil.GetMinMax(tmp);
                    double padding = 0.7 * BondLength;
                    sgroup.AddBracket(new SgroupBracket(minmax[0] - padding, minmax[1] - padding,
                                                        minmax[0] - padding, minmax[3] + padding));
                    sgroup.AddBracket(new SgroupBracket(minmax[2] + padding, minmax[1] - padding,
                                                        minmax[2] + padding, minmax[3] + padding));
                }
            }
        }

        private static double Angle(IBond bond)
        {
            Vector2 end = bond.Begin.Point2D.Value;
            Vector2 beg = bond.End.Point2D.Value;
            return Math.Atan2(end.Y - beg.Y, end.X - beg.X);
        }

        /// <summary>
        /// Generate a new bracket across the provided bond.
        /// </summary>
        /// <param name="bond">bond</param>
        /// <param name="bonds">bond map to Sgroups</param>
        /// <param name="counter">count how many brackets this group has already</param>
        /// <param name="vert">vertical align bonds</param>
        /// <returns>the new bracket</returns>
        private SgroupBracket NewCrossingBracket(IBond bond, IMultiDictionary<IBond, Sgroup> bonds, IDictionary<IBond, int> counter, bool vert)
        {
            IAtom beg = bond.Begin;
            IAtom end = bond.End;
            Vector2 begXy = beg.Point2D.Value;
            Vector2 endXy = end.Point2D.Value;
            Vector2 lenOffset = new Vector2(endXy.X - begXy.X, endXy.Y - begXy.Y);
            Vector2 bndCrossVec = new Vector2(-lenOffset.Y, lenOffset.X);
            lenOffset = Vector2.Normalize(lenOffset);
            bndCrossVec = Vector2.Normalize(bndCrossVec);
            bndCrossVec *= (0.9 * BondLength) / 2;

            var sgroups = new List<Sgroup>(bonds[bond]);

            // bond in sgroup, place it in the middle of the bond
            if (sgroups.Count == 1)
            {
                lenOffset *= 0.5 * BondLength;
            }
            // two sgroups, place one near start and one near end
            else if (sgroups.Count == 2)
            {
                bool flip = !sgroups[counter[bond]].Atoms.Contains(beg);
                if (counter[bond] == 0)
                {
                    lenOffset *= flip ? 0.75 : 0.25 * BondLength; // 75 or 25% along
                    counter[bond] = 1;
                }
                else
                {
                    lenOffset *= flip ? 0.25 : 0.75 * BondLength; // 25 or 75% along
                }
            }
            else
            {
                double step = BondLength / (1 + sgroups.Count);
                int idx = counter[bond] + 1;
                counter[bond] = idx;
                lenOffset *= (idx * step) * BondLength;
            }

            // vertical bracket
            if (vert)
            {
                return new SgroupBracket(begXy.X + lenOffset.X, begXy.Y + lenOffset.Y + bndCrossVec.Length(),
                                         begXy.X + lenOffset.X, begXy.Y + lenOffset.Y - bndCrossVec.Length());
            }
            else
            {
                return new SgroupBracket(begXy.X + lenOffset.X + bndCrossVec.X, begXy.Y + lenOffset.Y + bndCrossVec.Y,
                                         begXy.X + lenOffset.X - bndCrossVec.X, begXy.Y + lenOffset.Y - bndCrossVec.Y);
            }
        }

        /// <summary>
        /// Determine whether and Sgroup type has brackets to be placed.
        /// </summary>
        /// <param name="sgroup">the Sgroup</param>
        /// <returns>brackets need to be placed</returns>
        private static bool HasBrackets(Sgroup sgroup)
        {
            switch (sgroup.Type.Ordinal)
            {
                case SgroupType.O.CtabStructureRepeatUnit:
                case SgroupType.O.CtabAnyPolymer:
                case SgroupType.O.CtabCrossLink:
                case SgroupType.O.CtabComponent:
                case SgroupType.O.CtabMixture:
                case SgroupType.O.CtabFormulation:
                case SgroupType.O.CtabGraft:
                case SgroupType.O.CtabModified:
                case SgroupType.O.CtabMonomer:
                case SgroupType.O.CtabCopolymer:
                case SgroupType.O.CtabMultipleGroup:
                    return true;
                case SgroupType.O.CtabGeneric:
                    IList<SgroupBracket> brackets = (IList<SgroupBracket>)sgroup.GetValue(SgroupKeys.CtabBracket);
                    return brackets != null && brackets.Count != 0;
                default:
                    return false;
            }
        }

        private sealed class IntTuple
        {
            private readonly int beg, end;

            public IntTuple(int beg, int end)
            {
                this.beg = beg;
                this.end = end;
            }

            public override bool Equals(Object o)
            {
                if (this == o) return true;
                if (o == null || GetType() != o.GetType()) return false;

                IntTuple that = (IntTuple)o;

                return (this.beg == that.beg && this.end == that.end) ||
                       (this.beg == that.end && this.end == that.beg);
            }

            public override int GetHashCode()
            {
                return beg ^ end;
            }
        }
    }
}
