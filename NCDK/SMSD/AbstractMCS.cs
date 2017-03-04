/* Copyright (C) 2006-2010  Syed Asad Rahman <asad@ebi.ac.uk>
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
using NCDK.Isomorphisms.Matchers;
using System.Collections.Generic;

namespace NCDK.SMSD
{
    /// <summary>
    /// Interface for all MCS algorithms.
    // @cdk.module smsd
    // @cdk.githash
    // @author Syed Asad Rahman <asad@ebi.ac.uk>
    /// </summary>
    public abstract class AbstractMCS
    {
       /// <summary>
        /// initialize query and target molecules.
        ///
        /// <param name="source">query mol</param>
        /// <param name="target">target mol</param>
        /// <param name="removeHydrogen">true if remove H (implicit) before mapping</param>
        /// <param name="cleanAndConfigureMolecule">eg: percieveAtomTypesAndConfigureAtoms, detect aromaticity etc</param>
        // @throws CDKException
        /// </summary>
        public abstract void Init(IAtomContainer source, IAtomContainer target, bool removeHydrogen,
                bool cleanAndConfigureMolecule);

        /// <summary>
        /// initialize query and target molecules.
        ///
        /// Note: Here its assumed that hydrogens are implicit
        /// and user has called these two methods
        /// percieveAtomTypesAndConfigureAtoms and CDKAromicityDetector
        /// before initializing calling this method.
        ///
        /// <param name="source">query mol</param>
        /// <param name="target">target mol</param>
        // @throws CDKException
        /// </summary>
        public abstract void Init(IQueryAtomContainer source, IAtomContainer target);

        /// <summary>
        /// initialize query and target molecules.
        ///
        /// <param name="stereoFilter">set true to rank the solutions as per stereo matches</param>
        /// <param name="fragmentFilter">set true to return matches with minimum fragments</param>
        /// <param name="energyFilter">set true to return matches with minimum bond changes</param>
        /// based on the bond breaking energy
        /// </summary>
        public abstract void SetChemFilters(bool stereoFilter, bool fragmentFilter, bool energyFilter);

        /// <summary>
        /// Returns summation energy score of the disorder if the MCS is removed
        /// from the target and query graph. Amongst the solutions, a solution
        /// with lowest energy score is preferred.
        ///
        /// <param name="key">Index of the mapping solution</param>
        /// <returns>Total bond breaking energy required to remove the mapped part</returns>
        /// </summary>
        public abstract double? GetEnergyScore(int key);

        /// <summary>
        /// Returns number of fragment generated in the solution space,
        /// if the MCS is removed from the target and query graph.
        /// Amongst the solutions, a solution with lowest fragment size
        /// is preferred.
        ///
        /// <param name="key">Index of the mapping solution</param>
        /// <returns>Fragment Count(s) generated after removing the mapped parts</returns>
        /// </summary>
        public abstract int? GetFragmentSize(int key);

        /// <summary>
        ///
        /// Returns modified target molecule on which mapping was
        /// performed.
        ///
        ///
        /// <returns>return modified product Molecule</returns>
        /// </summary>
        public abstract IAtomContainer ProductMolecule { get; }

        /// <summary>
        /// Returns modified query molecule on which mapping was
        /// performed.
        ///
        /// <returns>return modified reactant Molecule</returns>
        /// </summary>
        public abstract IAtomContainer ReactantMolecule { get; }

        /// <summary>
        /// Returns a number which denotes the quality of the mcs.
        /// A solution with highest stereo score is preferred over other
        /// scores.
        /// <param name="key">Index of the mapping solution</param>
        /// <returns>true if no stereo mismatch occurs</returns>
        /// else false if stereo mismatch occurs
        /// </summary>
        public abstract int? GetStereoScore(int key);

        /// <summary>
        ///
        /// Returns true if mols have different stereo
        /// chemistry else false if no stereo mismatch.
        ///
        /// <returns>true if mols have different stereo</returns>
        /// chemistry else false if no stereo mismatch.
        /// true if stereo mismatch occurs
        /// else true if stereo mismatch occurs.
        /// </summary>
        public abstract bool IsStereoMisMatch();

        /// <summary>
        /// Checks if query is a subgraph of the target.
        /// Returns true if query is a subgraph of target else false
        /// <returns>true if query molecule is a subgraph of the target molecule</returns>
        /// </summary>
        public abstract bool IsSubgraph();

        /// <summary>
        /// Returns Tanimoto similarity between query and target molecules
        /// (Score is between 0-min and 1-max).
        ///
        /// <returns>Tanimoto Similarity between 0 and 1</returns>
        // @throws IOException
        /// </summary>
        public abstract double GetTanimotoSimilarity();

        /// <summary>
        /// Returns Euclidean Distance between query and target molecule.
        /// <returns>Euclidean Distance (lower the score, better the match)</returns>
        // @throws IOException
        /// </summary>
        public abstract double GetEuclideanDistance();

        /// <summary>
        /// Returns all plausible mappings between query and target molecules
        /// Each map in the list has atom-atom equivalence of the mappings
        /// between query and target molecule i.e. map.Key for the query
        /// and map.Value for the target molecule.
        /// <returns>All possible MCS atom Mappings</returns>
        /// </summary>
        public abstract IList<IDictionary<IAtom, IAtom>> GetAllAtomMapping();

        /// <summary>
        /// Returns all plausible mappings between query and target molecules
        /// Each map in the list has atom-atom equivalence index of the mappings
        /// between query and target molecule i.e. map.Key for the query
        /// and map.Value for the target molecule.
        /// <returns>All possible MCS Mapping Index</returns>
        /// </summary>
        public abstract IList<IDictionary<int, int>> GetAllMapping();

        /// <summary>
        /// Returns one of the best matches with atoms mapped.
        /// <returns>Best Atom Mapping</returns>
        /// </summary>
        public abstract IDictionary<IAtom, IAtom> GetFirstAtomMapping();

        /// <summary>
        /// Returns one of the best matches with atom indexes mapped.
        /// <returns>Best Mapping Index</returns>
        /// </summary>
        public abstract IDictionary<int, int> GetFirstMapping();

        /// <summary>
        /// timeout in mins (default 0.10 min) for bond sensitive searches
        /// </summary>
        public abstract double BondSensitiveTimeOut { get; set; }

        /// <summary>
        /// timeout in mins (default 1.00 min) for bond insensitive searches
        /// </summary>
        public abstract double BondInSensitiveTimeOut { get; set; }
    }
}
