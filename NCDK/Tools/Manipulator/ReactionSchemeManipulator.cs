/* Copyright (C) 2008  Miguel Rojas <miguelrojasch@users.sf.net>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *  */
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    // @cdk.module standard
    // @cdk.githash
    ///
    /// <seealso cref="ChemModelManipulator"/>
    /// </summary>
    public class ReactionSchemeManipulator
    {

        /// <summary>
        /// Get all molecule objects from a set of Reactions given a <see cref="IAtomContainerSet"/> to add.
        ///
        /// <param name="scheme">The set of reaction to inspect</param>
        /// <param name="molSet">The set of molecules to be added</param>
        /// <returns>The IAtomContainerSet</returns>
        /// </summary>
        public static IAtomContainerSet<IAtomContainer> GetAllAtomContainers(IReactionScheme scheme, IAtomContainerSet<IAtomContainer> molSet)
        {
            // A ReactionScheme can contain other IRreactionSet objects
            foreach (var rm in scheme.Schemes)
            {
                foreach (var ac in GetAllAtomContainers(rm, molSet))
                {
                    bool contain = false;
                    foreach (var atomContainer in molSet)
                    {
                        if (atomContainer.Equals(ac))
                        {
                            contain = true;
                            break;
                        }
                    }
                    if (!contain) molSet.Add((IAtomContainer)(ac));
                }
            }
            foreach (var reaction in scheme.Reactions)
            {
                var newAtomContainerSet = ReactionManipulator.GetAllAtomContainers(reaction);
                foreach (var ac in newAtomContainerSet)
                {
                    bool contain = false;
                    foreach (var atomContainer in molSet)
                    {
                        if (atomContainer.Equals(ac))
                        {
                            contain = true;
                            break;
                        }
                    }
                    if (!contain) molSet.Add(ac);

                }
            }

            return molSet;
        }

        /// <summary>
        /// get all AtomContainers object from a set of Reactions.
        ///
        /// <param name="scheme">The scheme of reaction to inspect</param>
        /// <returns>The IAtomContainerSet</returns>
        /// </summary>
        public static IAtomContainerSet<IAtomContainer> GetAllAtomContainers(IReactionScheme scheme)
        {
            return GetAllAtomContainers(scheme, scheme.Builder.CreateAtomContainerSet());
        }

        /// <summary>
        /// Get all ID of this IReactionSet.
        ///
        /// <param name="scheme">The IReactionScheme to analyze</param>
        /// <returns>A List with all ID</returns>
        /// </summary>
        public static IEnumerable<string> GetAllIDs(IReactionScheme scheme)
        {
            List<string> IDlist = new List<string>();
            if (scheme.Id != null) IDlist.Add(scheme.Id);
            foreach (var reaction in scheme.Reactions)
            {
                IDlist.AddRange(ReactionManipulator.GetAllIDs(reaction));
            }
            if (scheme.Schemes.Count != 0) foreach (var rs in scheme.Schemes)
                {
                    IDlist.AddRange(GetAllIDs(rs));
                }
            return IDlist;
        }

        /// <summary>
        /// Get all IReaction's object from a given IReactionScheme.
        ///
        /// <param name="scheme">The IReactionScheme to extract</param>
        /// <returns>The IReactionSet</returns>
        /// </summary>
        public static IReactionSet GetAllReactions(IReactionScheme scheme)
        {
            IReactionSet reactionSet = scheme.Builder.CreateReactionSet();

            // A ReactionScheme can contain other IRreactionSet objects
            if (scheme.Schemes.Count != 0) foreach (var schemeInt in scheme.Schemes)
                {
                    foreach (var reaction in GetAllReactions(schemeInt))
                        reactionSet.Add(reaction);
                }
            foreach (var reaction in scheme.Reactions)
                reactionSet.Add(reaction);

            return reactionSet;
        }

        /// <summary>
        /// Create a IReactionScheme give a IReactionSet object.
        ///
        /// <param name="reactionSet">The IReactionSet</param>
        /// <returns>The IReactionScheme</returns>
        /// </summary>
        public static IReactionScheme CreateReactionScheme(IReactionSet reactionSet)
        {
            IReactionScheme reactionScheme = reactionSet.Builder.CreateReactionScheme();

            // Looking for those reactants which doesn't have any precursor. They are the top.
            List<IReaction> listTopR = new List<IReaction>();
            foreach (var reaction in reactionSet)
            {
                if (ExtractPrecursorReaction(reaction, reactionSet).Count == 0) listTopR.Add(reaction);
            }

            foreach (var reaction in listTopR)
            {
                reactionScheme.Add(reaction);
                IReactionScheme newReactionScheme = SetScheme(reaction, reactionSet);
                if (newReactionScheme.Reactions.Count() != 0 || newReactionScheme.Schemes.Count != 0)
                    reactionScheme.Add(newReactionScheme);
            }
            return reactionScheme;
        }

        /// <summary>
        /// Extract a set of Reactions which are in top of a IReactionScheme. The top reactions are those
        /// which any of their reactants are participating in other reactions as a products.
        ///
        /// <param name="reactionScheme">The IReactionScheme</param>
        /// <returns>The set of top reactions</returns>
        /// </summary>
        public static IReactionSet ExtractTopReactions(IReactionScheme reactionScheme)
        {
            IReactionSet reactionSet = reactionScheme.Builder.CreateReactionSet();

            IReactionSet allSet = GetAllReactions(reactionScheme);
            foreach (var reaction in allSet)
            {
                IReactionSet precuSet = ExtractPrecursorReaction(reaction, allSet);
                if (precuSet.Count == 0)
                {
                    bool found = false;
                    foreach (var reactIn in reactionSet)
                    {
                        if (reactIn.Equals(reaction)) found = true;
                    }
                    if (!found) reactionSet.Add(reaction);
                }

            }
            return reactionSet;
        }

        /// <summary>
        /// Create a IReactionScheme given as a top a IReaction. If it doesn't exist any subsequent reaction
        /// return null;
        ///
        /// <param name="reaction">The IReaction as a top</param>
        /// <param name="reactionSet">The IReactionSet to extract a IReactionScheme</param>
        /// <returns>The IReactionScheme</returns>
        /// </summary>
        private static IReactionScheme SetScheme(IReaction reaction, IReactionSet reactionSet)
        {
            IReactionScheme reactionScheme = reaction.Builder.CreateReactionScheme();

            IReactionSet reactConSet = ExtractSubsequentReaction(reaction, reactionSet);
            if (reactConSet.Count != 0)
            {
                foreach (var reactionInt in reactConSet)
                {
                    reactionScheme.Add(reactionInt);
                    IReactionScheme newRScheme = SetScheme(reactionInt, reactionSet);
                    if (newRScheme.Count != 0 || newRScheme.Schemes.Count != 0)
                    {
                        reactionScheme.Add(newRScheme);
                    }
                }
            }
            return reactionScheme;
        }

        /// <summary>
        /// Extract reactions from a IReactionSet which at least one product is existing
        /// as reactant given a IReaction
        ///
        /// <param name="reaction">The IReaction to analyze</param>
        /// <param name="reactionSet">The IReactionSet to inspect</param>
        /// <returns>A IReactionSet containing the reactions</returns>
        /// </summary>
        private static IReactionSet ExtractPrecursorReaction(IReaction reaction, IReactionSet reactionSet)
        {
            IReactionSet reactConSet = reaction.Builder.CreateReactionSet();
            foreach (var reactant in reaction.Reactants)
            {
                foreach (var reactionInt in reactionSet)
                {
                    foreach (var precursor in reactionInt.Products)
                    {
                        if (reactant.Equals(precursor))
                        {
                            reactConSet.Add(reactionInt);
                        }
                    }
                }
            }
            return reactConSet;
        }

        /// <summary>
        /// Extract reactions from a IReactionSet which at least one reactant is existing
        /// as precursor given a IReaction
        ///
        /// <param name="reaction">The IReaction to analyze</param>
        /// <param name="reactionSet">The IReactionSet to inspect</param>
        /// <returns>A IReactionSet containing the reactions</returns>
        /// </summary>
        private static IReactionSet ExtractSubsequentReaction(IReaction reaction, IReactionSet reactionSet)
        {
            IReactionSet reactConSet = reaction.Builder.CreateReactionSet();
            foreach (var reactant in reaction.Products)
            {
                foreach (var reactionInt in reactionSet)
                {
                    foreach (var precursor in reactionInt.Reactants)
                    {
                        if (reactant.Equals(precursor))
                        {
                            reactConSet.Add(reactionInt);
                        }
                    }
                }
            }
            return reactConSet;
        }

        /// <summary>
        /// Extract the list of AtomContainers taking part in the IReactionScheme to originate a
        /// product given a reactant.
        ///
        /// <param name="origenMol">The start IAtomContainer</param>
        /// <param name="finalMol">The end IAtomContainer</param>
        /// <param name="reactionScheme">The IReactionScheme containing the AtomContainers</param>
        /// <returns>A List of IAtomContainerSet given the path</returns>
        /// </summary>
        public static IList<IAtomContainerSet<IAtomContainer>> GetAtomContainerSet(IAtomContainer origenMol, IAtomContainer finalMol,
                IReactionScheme reactionScheme)
        {
            List<IAtomContainerSet<IAtomContainer>> listPath = new List<IAtomContainerSet<IAtomContainer>>();
            IReactionSet reactionSet = GetAllReactions(reactionScheme);

            // down search
            // Looking for those reactants which are the origenMol
            bool found = false;
            foreach (var reaction in reactionSet)
            {
                if (found) break;
                foreach (var reactant in reaction.Reactants)
                {
                    if (found) break;
                    if (reactant.Equals(origenMol))
                    {
                        var allSet = reactionSet.Builder.CreateAtomContainerSet();
                        // START
                        foreach (var product in reaction.Products)
                        {
                            if (found) break;
                            if (!product.Equals(finalMol))
                            {
                                var allSet2 = GetReactionPath(product, finalMol, reactionSet);
                                if (allSet2.Count != 0)
                                {
                                    allSet.Add(origenMol);
                                    allSet.Add(product);
                                    allSet.AddRange(allSet2);
                                }
                            }
                            else
                            {
                                allSet.Add(origenMol);
                                allSet.Add(product);
                            }
                            if (allSet.Count() != 0)
                            {
                                listPath.Add(allSet);
                                found = true;
                            }
                        }

                        break;
                    }
                }
            }
            // TODO Looking for those products which are the origenMol

            // TODO: up search

            return listPath;
        }

        private static IAtomContainerSet<IAtomContainer> GetReactionPath(IAtomContainer reactant, IAtomContainer finalMol,
                IReactionSet reactionSet)
        {
            var allSet = reactionSet.Builder.CreateAtomContainerSet();
            foreach (var reaction in reactionSet)
            {
                foreach (var reactant2 in reaction.Reactants)
                {
                    if (reactant2.Equals(reactant))
                    {
                        foreach (var product in reaction.Products)
                        {
                            if (!product.Equals(finalMol))
                            {
                                var allSet2 = GetReactionPath(product, finalMol, reactionSet);
                                if (allSet2.Count() != 0)
                                {
                                    allSet.Add(reactant);
                                    allSet.AddRange(allSet2);
                                }
                            }
                            else
                            {
                                allSet.Add(product);
                                return allSet;
                            }
                        }

                    }
                }
            }
            return allSet;
        }
    }
}
