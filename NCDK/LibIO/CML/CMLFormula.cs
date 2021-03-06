﻿/*
 *    Copyright 2011 Peter Murray-Rust et. al.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NCDK.LibIO.CML
{
    /// <summary>
    /// user-modifiable class supporting formula. * The semantics of formula have
    /// been updated (2005-10) and the relationship between concise attribute and
    /// atomArray children is explicit. This class supports the parsing of a number
    /// of current inline structures but does not guarantee correctness as there are
    /// no agreed community syntax/semantics. This is a particular problem for
    /// charges which could be "2+", "++", "+2", etc. For robust inline interchange
    /// ONLY the concise representation is supported.
    /// </summary>
    /// <remarks>
    /// NOTE: The atomArray child, in array format, together with the formalCharge
    /// attribute is the primary means of holding the formula. There is now no lazy
    /// evaluation. The concise attribute can be autogenerated from the atomArray and
    /// formalCharge. If a formula is input with only concise then the atomArray is
    /// automatically generated from it.
    /// </remarks>
    public partial class CMLFormula : CMLElement
    {
        public const string SMILES = "SMILES";
        public const string SMILES1 = "cml:smiles";

        /// <summary>
        /// type of hydrogen counting
        /// </summary>
        // @author pm286
        public enum HydrogenStrategies
        {
            /// <summary>use hydrogen count attribute</summary>
            HYDROGEN_COUNT,
            /// <summary>use explicit hydrogens</summary>
            EXPLICIT_HYDROGENS,
        }

        // only edit insertion module!

        // marks whether concise has been processed before atomArray has been read
        internal bool processedConcise = false;
        private const bool allowNegativeCounts = false;

        // element:   formula
        // element:   atomArray

        public void Normalize()
        {
            // create all possible renderings of formula
            // any or all may be present
            // concise
            var conciseAtt = this.Attribute(Attribute_concise);
            // formal charge
            int formalCharge = 0;
            if (this.Attribute(Attribute_formalCharge) != null)
            {
                formalCharge = int.Parse(this.Attribute(Attribute_formalCharge).Value);
            }
            string conciseS = conciseAtt?.Value;
            // convention
            string convention = this.Convention;
            // inline formula (might be SMILES)
            string inline = this.Inline;
            if (inline != null)
            {
                inline = inline.Trim();
            }

            // atomArray
            CMLAtomArray atomArray = null;
            string atomArray2Concise = null;
            var atomArrays = this.Elements(XName_CML_atomArray).Cast<CMLAtomArray>().ToList();
            if (atomArrays.Count > 1)
            {
                throw new ApplicationException($"Only one atomArray child allowed for formula; found: {atomArrays.Count}");
            }
            else if (atomArrays.Count == 1)
            {
                atomArray = atomArrays.First();
                atomArray.Sort(Sorts.CHFIRST);
                atomArray2Concise = atomArray.GenerateConcise(formalCharge);
            }

            // concise from inline
            if (inline != null)
            {
                if (SMILES.Equals(convention) ||
                    SMILES1.Equals(convention))
                {
                    //                throw new RuntimeException("Move to SMILESTool");
                    //                inline2Concise = GetConciseFromSMILES(inline);
                }
            }
            if (conciseS == null)
            {
                if (atomArray2Concise != null)
                {
                    conciseS = atomArray2Concise;
                }
            }
            if (conciseS != null)
            {
                conciseS = NormalizeConciseAndFormalCharge(conciseS, formalCharge);
            }
            // if no atomArray, create
            if (atomArray == null)
            {
                // causes problems with Jmol
                //            if (conciseS != null) {
                //                atomArray = CreateAndAddAtomArrayAndFormalChargeFromConcise(conciseS);
                //            }
            }
            else
            {
                CheckAtomArrayFormat(atomArray);
            }
            if (atomArray != null)
            {
                atomArray.Sort(Sorts.CHFIRST);
            }
            // check consistency
            if (atomArray2Concise != null &&
                    !atomArray2Concise.Equals(conciseS))
            {
                throw new ApplicationException($"concise ({conciseS}) and atomArray ({atomArray2Concise}) differ");
            }
            if (conciseS != null)
            {
                // by this time we may have generated a non-zero formal charge, so normalize it into concise
                conciseS = NormalizeConciseAndFormalCharge(conciseS, this.FormalCharge);
                ForceConcise(conciseS);
            }
        }

        CMLAtomArray CreateAndAddAtomArrayAndFormalChargeFromConcise(string concise)
        {
            CMLAtomArray atomArray = new CMLAtomArray();
            if (concise != null)
            {
                var elements = new List<string>();
                var counts = new List<double>();
                var tokens = Regex.Split(concise, @"\s");
                int nelement = tokens.Length / 2;
                for (int i = 0; i < nelement; i++)
                {
                    string elem = tokens[2 * i];
                    var ce = Config.Elements.OfString(elem);
                    if (ce == Config.Elements.Unknown)
                    {
                        throw new ApplicationException($"Unknown chemical element: {elem}");
                    }
                    if (elements.Contains(elem))
                    {
                        throw new ApplicationException($"Duplicate element in concise: {elem}");
                    }
                    elements.Add(elem);
                    string countS = tokens[2 * i + 1];
                    try
                    {
                        counts.Add(double.Parse(countS));
                    }
                    catch (FormatException)
                    {
                        throw new ApplicationException($"Bad element count in concise: {countS}");
                    }
                }
                if (tokens.Length > nelement * 2)
                {
                    string chargeS = tokens[nelement * 2];
                    try
                    {
                        int formalCharge = int.Parse(chargeS);
                        FormalCharge = formalCharge;
                    }
                    catch (FormatException)
                    {
                        throw new ApplicationException($"Bad formal charge in concise: {chargeS}");
                    }
                }
                double[] countD = new double[nelement];
                for (int i = 0; i < nelement; i++)
                {
                    countD[i] = counts[i];
                }
                atomArray.ElementType = elements.ToArray();
                atomArray.Count = countD;
            }
            Add(atomArray);
            return atomArray;
        }

        /// <summary>
        /// checks that atomArray is in array format with unduplicated valid
        /// elements. must have elementType and count attributes of equal lengths.
        /// </summary>
        /// <param name="atomArray">to check (not modified)</param>
        /// <exception cref="ApplicationException">if invalid</exception>
        public void CheckAtomArrayFormat(CMLAtomArray atomArray)
        {
            if (atomArray.HasElements)
            {
                throw new ApplicationException("No children allowed for formula/atomArray");
            }
            var elements = atomArray.ElementType;
            var counts = atomArray.Count;
            if (elements == null || counts == null)
            {
                throw new ApplicationException("formula/atomArray must have elementType and count attributes");
            }
            if (elements.Length != counts.Length)
            {
                throw new ApplicationException("formula/atomArray must have equal length elementType and count values");
            }
            var elementSet = new HashSet<string>();
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null && !(elements[i].Equals("null")))
                {
                    if (elementSet.Contains(elements[i]))
                    {
                        throw new ApplicationException($"formula/atomArray@elementType has duplicate element: {elements[i]}");
                    }
                    elementSet.Add(elements[i]);
                    if (counts[i] <= 0 && !allowNegativeCounts)
                    {
                        throw new ApplicationException($"formula/atomArray@count has nonPositive value: {counts[i]}  {elements[i]}");
                    }
                }
            }
        }
        
        /// <summary>
        /// set concise attribute. if atomArray is absent will automatically compute
        /// atomArray and formalCharge, so use with care if atomArray is present will
        /// throw CMLRuntime this logic may need to be revised
        /// </summary>
        /// <param name="value">concise value</param>
        /// <exception cref="ApplicationException">attribute wrong value/type</exception>
        public virtual string Concise
        {
            get { return Attribute(Attribute_concise).Value; }
            set
            {
                if (AtomArrayElements.Any())
                {
                    throw new ApplicationException("Cannot reset concise if atomArray is present");
                }
                ForceConcise(value);
            }
        }

        private void ForceConcise(string value)
        {
            SetAttributeValue(Attribute_concise, value);
            Normalize();
            // if building, then XOM attributes are processed before children
            // this flag will allow subsequent atomArray to override
            processedConcise = true;
        }

        // FIXME move to tools    
        private string NormalizeConciseAndFormalCharge(string conciseS, int formalCharge)
        {
            if (conciseS != null)
            {
                CMLAtomArray atomArray = CreateAndAddAtomArrayAndFormalChargeFromConcise(conciseS);
                if (atomArray != null)
                {
                    atomArray.Sort(Sorts.CHFIRST);
                    conciseS = atomArray.GenerateConcise(formalCharge);
                }
            }
            return conciseS;
        }

        /// <summary>
        /// An inline representation of the object.
        /// No description
        /// </summary>
        public IEnumerable<CMLAtomArray> AtomArrayElements
            => this.Elements(XName_CML_atomArray).Cast<CMLAtomArray>();

        /// <summary>
        /// Adds element and count to formula. If element is already known,
        /// increments the count.
        /// </summary>
        /// <param name="elementType">the element atomicSymbol</param>
        /// <param name="count">the element multiplier</param>
        public void Add(string elementType, double count)
        {
            CMLAtomArray atomArray = AtomArrayElements.FirstOrDefault();
            if (atomArray == null)
            {
                // if no atomArray , create from concise
                Normalize();
                // if still none, create new one with empty attributes
                if (atomArray == null)
                {
                    atomArray = new CMLAtomArray();
                    Add(atomArray);
                    atomArray.ElementType = Array.Empty<string>();
                    atomArray.Count = Array.Empty<double>();
                }
            }
            string[] elements = GetElementTypes();
            if (elements == null)
            {
                elements = new string[0];
            }
            double[] counts = GetCounts();
            if (counts == null)
            {
                counts = new double[0];
            }
            int nelem = elements.Length;
            bool added = false;
            for (int i = 0; i < nelem; i++)
            {
                if (elements[i].Equals(elementType))
                {
                    counts[i] += count;
                    added = true;
                    break;
                }
            }
            if (!added)
            {
                string[] newElem = new string[nelem + 1];
                Array.Copy(elements, 0, newElem, 0, nelem);
                newElem[nelem] = elementType;
                double[] newCount = new double[nelem + 1];
                Array.Copy(counts, 0, newCount, 0, nelem);
                newCount[nelem] = count;
                atomArray.ElementType = newElem;
                atomArray.Count = newCount;
            }
            else
            {
                atomArray.ElementType = elements;
                atomArray.Count = counts;
            }
            int formalCharge = (this.Attribute(Attribute_formalCharge) == null) ? 0 : this.FormalCharge;
            string conciseS = atomArray.GenerateConcise(formalCharge);
            SetAttributeValue(Attribute_concise, conciseS);
        }

        /// <summary>
        /// Count for corresponding element.
        /// No defaults.
        /// </summary>
        /// <returns>double[] array of element counts; or null for none.</returns>
        public double[] GetCounts()
        {
            CMLAtomArray atomArray = AtomArrayElements.FirstOrDefault();
            return atomArray?.Count;
        }

        /// <summary>
        /// get atom count
        /// </summary>
        /// <returns>count</returns>
        public double GetTotalAtomCount()
        {
            //nwe23 - Fixed a bug here where GetCounts() returns null
            // for an empty formula, resulting in this crashing rather than
            // returning 0 as expected for empty formula
            double[] counts = GetCounts();
            if (counts == null)
            {
                return 0;
            }
            double total = 0;
            foreach (var count in counts)
            {
                total += count;
            }
            return total;
        }

        /// <summary>
        /// Count for corresponding element.
        /// No defaults.
        /// </summary>
        /// <returns>double[] array of element counts; or null for none.</returns>
        public string[] GetElementTypes()
        {
            CMLAtomArray atomArray = AtomArrayElements.FirstOrDefault();
            return atomArray?.ElementType;
        }
    }
}
