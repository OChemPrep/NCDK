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
using NCDK.QSAR.Results;
using NCDK.Smiles;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates chi path cluster descriptors.
    /// </summary>
    /// <remarks>
    /// The code currently evluates the simple and valence chi chain descriptors of orders 3, 4,5 and 6.
    /// It utilizes the graph isomorphism code of the CDK to find fragments matching
    /// SMILES strings representing the fragments corresponding to each type of chain.
    /// <para>
    /// The order of the values returned is
    /// <list type="bullet"> 
    /// <item>SPC-4 - Simple path cluster, order 4</item>
    /// <item>SPC-5 - Simple path cluster, order 5</item>
    /// <item>SPC-6 - Simple path cluster, order 6</item>
    /// <item>VPC-4 - Valence path cluster, order 4</item>
    /// <item>VPC-5 - Valence path cluster, order 5</item>
    /// <item>VPC-6 - Valence path cluster, order 6</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note</b>: These descriptors are calculated using graph isomorphism to identify
    /// the various fragments. As a result calculations may be slow. In addition, recent
    /// versions of Molconn-Z use simplified fragment definitions (i.e., rings without
    /// branches etc.) whereas these descriptors use the older more complex fragment
    /// definitions.
    /// </para>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2006-11-13
    // @cdk.module qsarmolecular
    // @cdk.githash
    // @cdk.dictref qsar-descriptors:chiPathCluster
    // @cdk.keyword chi path cluster index
    // @cdk.keyword descriptor
    public partial class ChiPathClusterDescriptor : IMolecularDescriptor
    {
        private SmilesParser sp;

        private static readonly string[] NAMES = { "SPC-4", "SPC-5", "SPC-6", "VPC-4", "VPC-5", "VPC-6" };

        public ChiPathClusterDescriptor() { }

        public IImplementationSpecification Specification => _Specification;
        private static DescriptorSpecification _Specification { get; } =
         new DescriptorSpecification(
                "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#chiPathCluster",
                typeof(ChiPathClusterDescriptor).FullName,
                "The Chemistry Development Kit");

        public IReadOnlyList<string> ParameterNames => null;
        public object GetParameterType(string name) => null;
        public IReadOnlyList<string> DescriptorNames => NAMES;
        public object[] Parameters { get { return null; } set { } }

        public DescriptorValue<ArrayResult<double>> Calculate(IAtomContainer container)
        {
            if (sp == null) sp = new SmilesParser(container.Builder);

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
            catch (Exception e)
            {
                return GetDummyDescriptorValue(new CDKException("Error in hydrogen addition: " + e.Message));
            }

            var subgraph4 = Order4(localAtomContainer);
            var subgraph5 = Order5(localAtomContainer);
            var subgraph6 = Order6(localAtomContainer);

            double order4s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph4);
            double order5s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph5);
            double order6s = ChiIndexUtils.EvalSimpleIndex(localAtomContainer, subgraph6);

            double order4v, order5v, order6v;
            try
            {
                order4v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph4);
                order5v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph5);
                order6v = ChiIndexUtils.EvalValenceIndex(localAtomContainer, subgraph6);
            }
            catch (CDKException e)
            {
                return GetDummyDescriptorValue(new CDKException("Error in substructure search: " + e.Message));
            }

            ArrayResult<double> retval = new ArrayResult<double>();
            retval.Add(order4s);
            retval.Add(order5s);
            retval.Add(order6s);

            retval.Add(order4v);
            retval.Add(order5v);
            retval.Add(order6v);

            return new DescriptorValue<ArrayResult<double>>(_Specification, ParameterNames, Parameters, retval, DescriptorNames);
        }

        private DescriptorValue<ArrayResult<double>> GetDummyDescriptorValue(Exception e)
        {
            int ndesc = DescriptorNames.Count;
            ArrayResult<double> results = new ArrayResult<double>(ndesc);
            for (int i = 0; i < ndesc; i++)
                results.Add(double.NaN);
            return new DescriptorValue<ArrayResult<double>>(_Specification, ParameterNames, Parameters, results,
                    DescriptorNames, e);
        }

        /// <inheritdoc/>
        public IDescriptorResult DescriptorResultType { get; } = new ArrayResult<double>(6);

        private IList<IList<int>> Order4(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[1];
            try
            {
                queries[0] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)CC"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }

        private IList<IList<int>> Order5(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[3];
            try
            {
                queries[0] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)CCC"), false);
                queries[1] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)(C)CC"), false);
                queries[2] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CCC(C)CC"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }

        private IList<IList<int>> Order6(IAtomContainer atomContainer)
        {
            QueryAtomContainer[] queries = new QueryAtomContainer[5];
            try
            {
                queries[0] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)(C)CCC"), false);
                queries[1] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)C(C)CC"), false);
                queries[2] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)CC(C)C"), false);
                queries[3] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CC(C)CCCC"), false);
                queries[4] = QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(sp.ParseSmiles("CCC(C)CCC"), false);
            }
            catch (InvalidSmilesException e)
            {
                Console.Error.WriteLine(e.StackTrace); //To change body of catch statement use File | Settings | File Templates.
            }
            return ChiIndexUtils.GetFragments(atomContainer, queries);
        }
    }
}
