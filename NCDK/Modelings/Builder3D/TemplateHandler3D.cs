/*  Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *                     2011  Egon Willighagen <egonw@users.sf.net>
 *                     2014  Mark B Vine (orcid:0000-0002-7794-0426)
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
 */
using NCDK.IO.Iterator;
using NCDK.Isomorphisms;
using NCDK.Isomorphisms.Matchers;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    /// Helper class for ModelBuilder3D. Handles templates. This is
    /// our layout solution for 3D ring systems
    /// </summary>
    // @author      cho
    // @author      steinbeck
    // @cdk.created 2004-09-21
    // @cdk.module  builder3d
    // @cdk.githash
    public class TemplateHandler3D
    {
        private static readonly IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;
        public const string TEMPLATE_PATH = "Data.ringTemplateStructures.sdf.gz";

        private readonly List<IAtomContainer> templates = new List<IAtomContainer>();
        bool _defaultTemplatesLoaded = false;
        private readonly List<IQueryAtomContainer> queries = new List<IQueryAtomContainer>();
        private readonly List<Pattern> patterns = new List<Pattern>();

        private static TemplateHandler3D self = null;

        private UniversalIsomorphismTester universalIsomorphismTester = new UniversalIsomorphismTester();

        private TemplateHandler3D()
        {
        }

        public static TemplateHandler3D Instance
        {
            get
            {
                if(self == null)
                {
                    self = new TemplateHandler3D();
                }
                return self;
            }
        }

        private void AddTemplateMol(IAtomContainer mol)
        {
            templates.Add(mol);
            QueryAtomContainer query = QueryAtomContainerCreator.CreateAnyAtomContainer(mol, false);
            queries.Add(query);
            for(int i = 0; i < mol.Atoms.Count; i++)
            {
                query.Atoms[i].Point3D = mol.Atoms[i].Point3D;
            }
            patterns.Add(Pattern.FindSubstructure(query));
        }

        /// <summary>
        /// Load ring template
        /// </summary>
        /// <exception cref="CDKException">The template file cannot be loaded</exception>
        private void LoadTemplates()
        {
            try
            {
                using(var gin = GetType().Assembly.GetManifestResourceStream(GetType(), TEMPLATE_PATH))
                using(var ins = new GZipStream(gin, CompressionMode.Decompress))
                {
                    LoadTemplates(ins);
                }

                _defaultTemplatesLoaded = true;
            }
            catch(IOException e)
            {
                throw new CDKException("Could not load ring templates", e);
            }
        }


        public void LoadTemplates(Stream stream)
        {
            using(EnumerableSDFReader sdfr = new EnumerableSDFReader(stream, builder))
            {
                foreach(var mol in sdfr)
                {
                    AddTemplateMol(mol);
                }
            }
        }


        public static BitArray GetBitSetFromFile(IEnumerable<string> st)
        {
            BitArray bitSet = new BitArray(1024);
            foreach(var s in st)
                bitSet.Set(int.Parse(s), true);
            return bitSet;
        }

        /// <summary>
        /// Returns the largest (number of atoms) ring set in a molecule.
        /// </summary>
        /// <param name="ringSystems">RingSystems of a molecule</param>
        /// <returns>The largestRingSet</returns>
        public IRingSet GetLargestRingSet(List<IRingSet> ringSystems)
        {
            IRingSet largestRingSet = null;
            int atomNumber = 0;
            IAtomContainer container = null;
            for(int i = 0; i < ringSystems.Count; i++)
            {
                container = GetAllInOneContainer(ringSystems[i]);
                if(atomNumber < container.Atoms.Count)
                {
                    atomNumber = container.Atoms.Count;
                    largestRingSet = ringSystems[i];
                }
            }
            return largestRingSet;
        }

        private IAtomContainer GetAllInOneContainer(IRingSet ringSet)
        {
            IAtomContainer resultContainer = ringSet.Builder.NewAtomContainer();
            var containers = RingSetManipulator.GetAllAtomContainers(ringSet);
            foreach(var container in containers)
                resultContainer.Add(container);
            return resultContainer;
        }


        private bool IsExactMatch(IAtomContainer query,
                                 IDictionary<IChemObject, IChemObject> mapping)
        {
            foreach(IQueryAtom src in query.Atoms)
            {
                IAtom dst = (IAtom)mapping[src];
                if(!src.Matches(dst))
                    return false;
            }
            foreach(IQueryBond src in query.Bonds)
            {
                IBond dst = (IBond)mapping[src];
                if(!src.Matches(dst))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if one of the loaded templates is a substructure in the given
        /// Molecule. If so, it assigns the coordinates from the template to the
        /// respective atoms in the Molecule.
        /// </summary>
        /// <param name="mol">AtomContainer from the ring systems.</param>
        /// <param name="numberOfRingAtoms">Number of atoms in the specified ring</param>
        public void MapTemplates(IAtomContainer mol, int numberOfRingAtoms)
        {
            if(!_defaultTemplatesLoaded)
                LoadTemplates();

            IAtomContainer best = null;
            IDictionary<IChemObject, IChemObject> bestMap = null;
            IAtomContainer secondBest = null;
            IDictionary<IChemObject, IChemObject> secondBestMap = null;

            for(int i = 0; i < templates.Count; i++)
            {
                IAtomContainer query = queries[i];

                //if the atom count is different, it can't be right anyway
                if(query.Atoms.Count != mol.Atoms.Count)
                {
                    continue;
                }

                int bondMatchCount = 0;
                int lowMatchCount = 0;

                Mappings mappings = patterns[i].MatchAll(mol);
                foreach(IDictionary<IChemObject, IChemObject> map in mappings.ToAtomBondMap())
                {
                    if(IsExactMatch(query, map))
                    {
                        AssignCoords(query, map);
                        Console.WriteLine("Found exact match at index " + i + ": " + templates[i].ToString());
                        return;
                    }
                    else if(query.Bonds.Count == mol.Bonds.Count)
                    {
                        best = query;
                        bestMap = new Dictionary<IChemObject, IChemObject>(map);
                        bondMatchCount++;
                    }
                    else
                    {
                        secondBest = query;
                        secondBestMap = new Dictionary<IChemObject, IChemObject>(map);
                        lowMatchCount++;
                    }
                }

                if(bondMatchCount > 0)
                    Console.WriteLine("Found " + bondMatchCount + " bond count matches at index " + i + ": " + templates[i].ToString());

                if(lowMatchCount > 0)
                    Console.WriteLine("Found " + lowMatchCount + " low matches at index " + i + ": " + templates[i].ToString());
            }

            if(best != null)
            {
                AssignCoords(best, bestMap);
            }
            else if(secondBest != null)
            {
                AssignCoords(secondBest, secondBestMap);
            }

            Console.Error.WriteLine("WARNING: Maybe RingTemplateError!");
        }

        private void AssignCoords(IAtomContainer template,
                                  IDictionary<IChemObject, IChemObject> map)
        {
            foreach(IAtom src in template.Atoms)
            {
                IAtom dst = (IAtom)map[src];
                dst.Point3D = src.Point3D;
            }
        }

        /// <summary>
        /// Gets the templateCount attribute of the TemplateHandler object.
        /// </summary>
        /// <returns>The templateCount value</returns>
        public int TemplateCount => templates.Count;

        /// <summary>
        ///  Gets the templateAt attribute of the TemplateHandler object.
        /// </summary>
        /// <param name="position">Description of the Parameter</param>
        /// <returns>The templateAt value</returns>
        public IAtomContainer GetTemplateAt(int position)
        {
            return templates[position];
        }
    }
}
