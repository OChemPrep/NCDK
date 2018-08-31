/* Copyright (C) 2008-2010  Egon Willighagen <egonw@users.sf.net>
 *                    2009  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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
using NCDK.Config;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Validate
{
    /// <summary>
    /// Validator which tests a number of basic chemical semantics.
    /// </summary>
    // @author   Egon Willighagen
    // @cdk.githash
    // @cdk.created  2003-08-22
    public class BasicValidator : AbstractValidator
    {
        public BasicValidator() { }

        public override ValidationReport ValidateAtom(IAtom subject)
        {
            ValidationReport report = new ValidationReport();
            report.Add(ValidateCharge(subject));
            report.Add(ValidateHydrogenCount(subject));
            report.Add(ValidatePseudoAtom(subject));
            return report;
        }

        public override ValidationReport ValidateBond(IBond subject)
        {
            ValidationReport report = new ValidationReport();
            report.Add(ValidateStereoChemistry(subject));
            report.Add(ValidateMaxBondOrder(subject));
            return report;
        }

        public override ValidationReport ValidateIsotope(IIsotope subject)
        {
            return ValidateIsotopeExistence(subject);
        }

        public override ValidationReport ValidateMolecule(IAtomContainer subject)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest emptyMolecule = new ValidationTest(subject, "Molecule does not contain any atom");

            if (subject.Atoms.Count == 0)
            {
                report.Errors.Add(emptyMolecule);
            }
            else
            {
                report.OKs.Add(emptyMolecule);
                ValidationTest massCalcProblem = new ValidationTest(subject,
                        "Molecule contains PseudoAtom's. Won't be able to calculate some properties, like molecular mass.");
                bool foundMassCalcProblem = false;
                for (int i = 0; i < subject.Atoms.Count; i++)
                {
                    if (subject.Atoms[i] is IPseudoAtom)
                    {
                        foundMassCalcProblem = true;
                    }
                    else
                    {
                        report.Add(ValidateBondOrderSum(subject.Atoms[i], subject));
                    }
                }
                if (foundMassCalcProblem)
                {
                    report.Warnings.Add(massCalcProblem);
                }
                else
                {
                    report.OKs.Add(massCalcProblem);
                }
            }
            return report;
        }

        public override ValidationReport ValidateReaction(IReaction subject)
        {
            ValidationReport report = new ValidationReport();
            IAtomContainer container1 = subject.Builder.NewAtomContainer();
            var reactants = subject.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                container1.Add(reactants[i]);
            }
            IAtomContainer container2 = subject.Builder.NewAtomContainer();
            var products = subject.Products;
            for (int i = 0; i < products.Count; i++)
            {
                container2.Add(products[i]);
            }
            report.Add(ValidateAtomCountConservation(subject, container1, container2));
            report.Add(ValidateChargeConservation(subject, container1, container2));
            return report;
        }

        // the Atom tests

        private ValidationReport ValidateCharge(IAtom atom)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest tooCharged = new ValidationTest(atom, "Atom has an unlikely large positive or negative charge");
            if (atom.Symbol.Equals("O") || atom.Symbol.Equals("N") || atom.Symbol.Equals("C")
                    || atom.Symbol.Equals("H"))
            {
                if (atom.FormalCharge == 0)
                {
                    report.OKs.Add(tooCharged);
                }
                else
                {
                    tooCharged.Details = $"Atom {atom.Symbol} has charge {atom.FormalCharge}";
                    if (atom.FormalCharge < -3)
                    {
                        report.Errors.Add(tooCharged);
                    }
                    else if (atom.FormalCharge < -1)
                    {
                        report.Warnings.Add(tooCharged);
                    }
                    else if (atom.FormalCharge > 3)
                    {
                        report.Errors.Add(tooCharged);
                    }
                    else if (atom.FormalCharge > 1)
                    {
                        report.Warnings.Add(tooCharged);
                    }
                }
            }
            else
            {
                if (atom.FormalCharge == 0)
                {
                    report.OKs.Add(tooCharged);
                }
                else
                {
                    tooCharged.Details = $"Atom {atom.Symbol} has charge {atom.FormalCharge}";
                    if (atom.FormalCharge < -4)
                    {
                        report.Errors.Add(tooCharged);
                    }
                    else if (atom.FormalCharge < -3)
                    {
                        report.Warnings.Add(tooCharged);
                    }
                    else if (atom.FormalCharge > 4)
                    {
                        report.Errors.Add(tooCharged);
                    }
                    else if (atom.FormalCharge > 3)
                    {
                        report.Warnings.Add(tooCharged);
                    }
                }
            }
            return report;
        }

        private ValidationReport ValidateHydrogenCount(IAtom atom)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest negativeHydrogenCount = new ValidationTest(atom,
                    "An Atom cannot have a negative number of hydrogens attached.");
            if (atom.ImplicitHydrogenCount == null)
            {
                report.Warnings.Add(new ValidationTest(atom, "An atom had unset (null) implicit hydrogen count"));
            }
            else if (atom.ImplicitHydrogenCount < 0)
            {
                negativeHydrogenCount.Details = $"Atom has {atom.ImplicitHydrogenCount} hydrogens.";
                report.Errors.Add(negativeHydrogenCount);
            }
            else
            {
                report.OKs.Add(negativeHydrogenCount);
            }
            return report;
        }

        private ValidationReport ValidatePseudoAtom(IAtom atom)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest isElementOrPseudo = new ValidationTest(atom, "Non-element atom must be of class PseudoAtom.");
            if (atom is IPseudoAtom)
            {
                // that's fine
                report.OKs.Add(isElementOrPseudo);
            }
            else
            {
                // check whether atom is really an element
                try
                {
                    IsotopeFactory isotopeFactory = Isotopes.Instance;
                    IElement element = isotopeFactory.GetElement(atom.Symbol);
                    if (element == null)
                    {
                        isElementOrPseudo.Details = $"Element {atom.Symbol} does not exist.";
                        report.Errors.Add(isElementOrPseudo);
                    }
                    else
                    {
                        report.OKs.Add(isElementOrPseudo);
                    }
                }
                catch (Exception exception)
                {
                    // well... don't throw an error then.
                    isElementOrPseudo.Details = exception.ToString();
                    report.CDKErrors.Add(isElementOrPseudo);
                }
            }
            return report;
        }

        // the Bond tests

        private ValidationReport ValidateStereoChemistry(IBond bond)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest bondStereo = new ValidationTest(bond, "Defining stereochemistry on bonds is not safe.",
                    "Use atom based stereochemistry.");
            if (bond.Stereo != BondStereo.None)
            {
                report.Warnings.Add(bondStereo);
            }
            else
            {
                report.OKs.Add(bondStereo);
            }
            return report;
        }

        private ValidationReport ValidateMaxBondOrder(IBond bond)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest maxBO = new ValidationTest(bond, "Bond order exceeds the maximum for one of its atoms.");
            try
            {
                AtomTypeFactory structgenATF = AtomTypeFactory.GetInstance("NCDK.Dict.cdk-atom-types.owl", bond.Builder);
                for (int i = 0; i < bond.Atoms.Count; i++)
                {
                    IAtom atom = bond.Atoms[i];
                    if (atom is IPseudoAtom)
                    {
                        // ok, all is fine; we don't know the properties of pseudo atoms
                        break;
                    }
                    var atomTypes = structgenATF.GetAtomTypes(atom.Symbol);
                    IAtomType failedOn = null;
                    bool foundMatchingAtomType = false;
                    foreach (var atomType in atomTypes)
                    {
                        if (!BondManipulator.IsHigherOrder(bond.Order, atomType.MaxBondOrder))
                        {
                            foundMatchingAtomType = true;
                        }
                        else
                        {
                            failedOn = atomType;
                        }
                    }
                    if (foundMatchingAtomType)
                    {
                        report.OKs.Add(maxBO);
                    }
                    else
                    {
                        if (failedOn != null)
                        {
                            maxBO.Details = $"Bond order exceeds the one allowed for atom {atom.Symbol} for which the maximum bond order is {failedOn.MaxBondOrder}";
                        }
                        report.Errors.Add(maxBO);
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("Error while performing atom bos validation");
                Debug.WriteLine(exception);
                maxBO.Details = $"Error while performing atom bos validation: {exception.ToString()}";
                report.CDKErrors.Add(maxBO);
            }
            return report;
        }

        // the Isotope tests

        public ValidationReport ValidateIsotopeExistence(IIsotope isotope)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest isotopeExists = new ValidationTest(isotope, "Isotope with this mass number is not known for this element.");
            try
            {
                IsotopeFactory isotopeFac = Isotopes.Instance;
                var isotopes = isotopeFac.GetIsotopes(isotope.Symbol);
                bool foundKnownIsotope = false;
                if (isotope.MassNumber != 0)
                {
                    foreach (var facIsotope in isotopes)
                    {
                        if (facIsotope.MassNumber == isotope.MassNumber)
                        {
                            foundKnownIsotope = true;
                        }
                    }
                }
                if (!foundKnownIsotope)
                {
                    report.Errors.Add(isotopeExists);
                }
                else
                {
                    // isotopic number is not set
                    report.OKs.Add(isotopeExists);
                }
            }
            catch (Exception)
            {
                // too bad...
            }
            return report;
        }

        // the Molecule tests

        private ValidationReport ValidateBondOrderSum(IAtom atom, IAtomContainer molecule)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest checkBondSum = new ValidationTest(atom, "The atom's total bond order is too high.");
            try
            {
                AtomTypeFactory structgenATF = AtomTypeFactory.GetInstance("NCDK.Dict.Data.cdk-atom-types.owl", atom.Builder);
                int bos = (int)molecule.GetBondOrderSum(atom);
                var atomTypes = structgenATF.GetAtomTypes(atom.Symbol).ToList();
                if (atomTypes.Count == 0)
                {
                    checkBondSum.Details = $"Cannot validate bond order sum for atom not in valency atom type list: {atom.Symbol}";
                    report.Warnings.Add(checkBondSum);
                }
                else
                {
                    IAtomType failedOn = null;
                    bool foundMatchingAtomType = false;
                    foreach (var type in atomTypes)
                    {
                        if (atom.FormalCharge == type.FormalCharge)
                        {
                            foundMatchingAtomType = true;
                            if (bos == type.BondOrderSum)
                            {
                                // skip this atom type
                            }
                            else
                            {
                                failedOn = type;
                            }
                        }
                    }
                    if (foundMatchingAtomType)
                    {
                        report.OKs.Add(checkBondSum);
                    }
                    else
                    {
                        if (failedOn != null)
                        {
                            checkBondSum.Details = $"Bond order exceeds the one allowed for atom {atom.Symbol} for which the total bond order is {failedOn.BondOrderSum}";
                        }
                        else
                        {

                        }
                        report.Errors.Add(checkBondSum);
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("Error while performing atom bos validation: ", exception.Message);
                Debug.WriteLine(exception);
            }
            return report;
        }

        private ValidationReport ValidateAtomCountConservation(IReaction reaction, IAtomContainer reactants,
                IAtomContainer products)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest atomCount = new ValidationTest(reaction, "Atom count mismatch for reaction: the product side has a different atom count than the reactant side.");
            if (reactants.Atoms.Count != products.Atoms.Count)
            {
                report.Errors.Add(atomCount);
            }
            else
            {
                report.OKs.Add(atomCount);
            }
            return report;
        }

        private ValidationReport ValidateChargeConservation(IReaction reaction, IAtomContainer reactants,
                IAtomContainer products)
        {
            ValidationReport report = new ValidationReport();
            ValidationTest chargeConservation = new ValidationTest(reaction, "Total formal charge is not preserved during the reaction");
            var atoms1 = reactants.Atoms;
            int totalCharge1 = 0;
            foreach (var atom1 in atoms1)
            {
                totalCharge1 = +atom1.FormalCharge.Value;
            }
            var atoms2 = products.Atoms;
            int totalCharge2 = 0;
            foreach (var atom2 in atoms2)
            {
                totalCharge2 = +atom2.FormalCharge.Value;
            }
            if (totalCharge1 != totalCharge2)
            {
                report.Errors.Add(chargeConservation);
            }
            else
            {
                report.OKs.Add(chargeConservation);
            }
            return report;
        }
    }
}
