/*  Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
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
using NCDK.AtomTypes;

using NCDK.Isomorphisms.Matchers;
using NCDK.QSAR.Result;
using NCDK.Smiles;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates chi cluster descriptors.
    /// <p/>
    /// The code currently evluates the simple and valence chi chain descriptors of orders 3, 4,5 and 6.
    /// It utilizes the graph isomorphism code of the CDK to find fragments matching
    /// SMILES strings representing the fragments corresponding to each type of chain.
    /// <p/>
    /// The order of the values returned is
    /// <ol>
    /// <li>SC-3 - Simple cluster, order 3
    /// <li>SC-4 - Simple cluster, order 4
    /// <li>SC-5 - Simple cluster, order 5
    /// <li>SC-6 - Simple cluster, order 6
    /// <li>VC-3 - Valence cluster, order 3
    /// <li>VC-4 - Valence cluster, order 4
    /// <li>VC-5 - Valence cluster, order 5
    /// <li>VC-6 - Valence cluster, order 6
    /// </ol>
    /// <p/>
    /// <b>Note</b>: These descriptors are calculated using graph isomorphism to identify
    /// the various fragments. As a result calculations may be slow. In addition, recent
    /// versions of Molconn-Z use simplified fragment definitions (i.e., rings without
    /// branches etc.) whereas these descriptors use the older more complex fragment
    /// definitions.
    ///
    // @author Rajarshi Guha
    // @cdk.created 2006-11-13
    // @cdk.module qsarmolecular
    // @cdk.githash
    // @cdk.set qsar-descriptors
    // @cdk.dictref qsar-descriptors:chiCluster
    // @cdk.keyword chi cluster index
    // @cdk.keyword descriptor
    /// </summary>
    public class ChiClusterDescriptor : AbstractMolecularDescriptor, IMolecularDescriptor
    {
        private SmilesParser sp;

        private static readonly string[] NAMES = { "SC-3", "SC-4", "SC-5", "SC-6", "VC-3", "VC-4", "VC-5", "VC-6" };

        public ChiClusterDescriptor() { }

        public override IImplementationSpecification Specification => _Specification;
        private static DescriptorSpecification _Specification { get; } =
         new DescriptorSpecification(
                "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#chiCluster",
                typeof(ChiClusterDescriptor).FullName,
                "The Chemistry Development Kit");

        public override string[] ParameterNames => null;
        public override object GetParameterType(string name) => null;
        public override string[] DescriptorNames => NAMES;
        public override object[] Parameters { get { return null; } set { } }

        private DescriptorValue GetDummyDescriptorValue(Exception e)
        {
            int ndesc = DescriptorNames.Length;
            DoubleArrayResult results = new DoubleArrayResult(ndesc);
            for (int i = 0; i < ndesc; i++)
                results.Add(double.NaN);
            return new DescriptorValue(_Specification, ParameterNames, Parameters, results,
                    DescriptorNames, e);
        }

        public override DescriptorValue Calculate(IAtomContainer container)
        {
            if (sp == null) sp = new SmilesParser(container.Builder);

            // removeHydrogens does a deep copy, so no need to clone
            IAtomContainer localAtomContainer = AtomContainerManipulator.RemoveHydrogens(container);
            CDKAtomTypeMatcher matcher = CDKAtomTypeMatcher.GetInstance(container.Builder);
            foreach (var atom in localAtomContainer.Atoms)
            {
                IAtomType type;
                try
                {
                    type = matcher.FindMatchingAtomType(localAtomContainer, atom);
                    AtomTypeManipulator.Configure(atom, type);
                }
                catch (Exception e)
                {
                    return GetDummyDescriptorValue(new CDKException("Error in atom typing: " + e.Message));
                }
            }
            CDKHydrogenAdder hAdder = CDKHydrogenAdder.GetInstance(container.Builder);
            try
            {
                hAdder.AddImplicitHydrogens(localAtomContainer);
            }
            catch (CDKException e)
            {
                return GetDummyDescriptorValue(new CDKException("Error in hydrogen addition: " + e.Message));
            }

            var subgraph3 = Order3(localAtomContainer);
            var subgraph4 = Order4(localAtomContainer);
            var subgraph5 = Order5(localAtomContainer);
            var subgraph6 = Order6(localAtomContainer);

            double order3s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph3);
            double order4s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph4);
            double order5s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph5);
            double order6s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph6);

            double order3v, order4v, order5v, order6v;
            try
            {
                order3v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph3);
                order4v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph4);
                order5v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph5);
                order6v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph6);
            }
            catch (CDKException e)
            {
                return GetDummyDescriptorValue(new CDKException("Error in substructure search: " + e.Message));
            }
            DoubleArrayResult retval = new DoubleArrayResult();
            retval.Add(order3s);
            retval.Add(order4s);
            retval.Add(order5s);
            retval.Add(order6s);

            retval.Add(order3v);
            retval.Add(order4v);
            retval.Add(order5v);
            retval.Add(order6v);

            return new DescriptorValue(_Specification, ParameterNames, Parameters, retval, DescriptorNames);
        }

        /// <summary>
        /// Returns the specific type of the DescriptorResult object.
        /// <p/>
        /// The return value from this method really indicates what type of result will
        /// be obtained from the <see cref="DescriptorValue"/> object. Note that the same result
        /// can be achieved by interrogating the <see cref="DescriptorValue"/> object; this method
        /// allows you to do the same thing, without actually calculating the descriptor.
        ///
        /// <returns>an object that implements the <see cref="IDescriptorResult"/> interface indicating</returns>
        ///         the actual type of values returned by the descriptor in the <see cref="DescriptorValue"/> object
        /// </summary>
        public override IDescriptorResult DescriptorResultType { get; } = new DoubleArrayResultType(8);

        private IList<IList<int>> Order3(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[1];
            try
            {
                queries[0] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("C(C)(C)(C)"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }

        private IList<IList<int>> Order4(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[1];
            try
            {
                queries[0] = QueryAtomContainerCreator
                        .CreateAnyAtomAnyBondContainer(sp.ParseSmiles("C(C)(C)(C)(C)"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }

        private IList<IList<int>> Order5(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[1];
            try
            {
                queries[0] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)C(C)(C)"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }

        private IList<IList<int>> Order6(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[2];
            try
            {
                queries[0] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("C1(C)C(C)C1(C)"),
                        false);
                queries[1] = QueryAtomContainerCreator
                        .CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)C(C)(C)C"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }
    }
}
