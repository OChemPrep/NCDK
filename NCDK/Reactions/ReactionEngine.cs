/* 
 * Copyright (C) 2008  Miguel Rojas <miguelrojasch@users.sf.net>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Dict;
using NCDK.Reactions.Types.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Reactions
{
    /// <summary>
    /// <p>The base class for all chemical reactions objects in this cdk.
    /// It provides methods for adding parameters</p>
    ///
    // @author         Miguel Rojas
    ///
    // @cdk.created    2008-02-01
    // @cdk.module     reaction
    // @cdk.set        reaction-types
    // @cdk.githash
    /// </summary>
    public class ReactionEngine
    {
        private EntryDictionary dictionary;
        public Dictionary<string, object> ParamsMap { get; set; }
        public IReactionMechanism Mechanism { get; set; }

        /// <summary>
        /// Constructor of the ReactionEngine object.
        /// </summary>
        public ReactionEngine()
        {
            try
            {
                IReactionProcess reaction = (IReactionProcess)this;
                EntryReact entry = InitiateDictionary("reaction-processes", (IReactionProcess)reaction);
                InitiateParameterMap2(entry);
                reaction.ParameterList = ParameterList;
                ExtractMechanism(entry);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }

        }

        /// <summary>
        /// Extract the mechanism necessary for this reaction.
        ///
        /// <param name="entry">The EntryReact object</param>
        /// </summary>
        private void ExtractMechanism(EntryReact entry)
        {
            string mechanismName = "NCDK.Reactions.Mechanisms." + entry.Mechanism;
            try
            {
                Mechanism = (IReactionMechanism)this.GetType().Assembly.GetType(mechanismName).GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                Trace.TraceInformation("Loaded mechanism: ", mechanismName);
            }
            catch (ArgumentException exception)
            {
                Trace.TraceError("Could not find this IReactionMechanism: ", mechanismName);
                Debug.WriteLine(exception);
            }
            catch (Exception exception)
            {
                Trace.TraceError("Could not load this IReactionMechanism: ", mechanismName);
                Debug.WriteLine(exception);
            }
        }

        /// <summary>
        /// Open the Dictionary OWLReact.
        ///
        /// <param name="nameDict">Name of the Dictionary</param>
        /// <param name="reaction">The IReactionProcess</param>
        /// <returns>The entry for this reaction</returns>
        /// </summary>
        private EntryReact InitiateDictionary(string nameDict, IReactionProcess reaction)
        {
            DictionaryDatabase db = new DictionaryDatabase();
            dictionary = db.GetDictionary(nameDict);
            string entryString = reaction.Specification.SpecificationReference;
            entryString = entryString.Substring(entryString.IndexOf('#') + 1);

            return (EntryReact)dictionary[entryString.ToLowerInvariant()];
        }

        /// <summary>
        /// Creates a map with the name and type of the parameters.
        /// </summary>
        private void InitiateParameterMap2(EntryReact entry)
        {
            var paramDic = entry.ParameterClass;

            ParameterList = new List<IParameterReact>();
            foreach (var param in paramDic)
            {
                string paramName = "NCDK.Reactions.Types.Parameters." + param[0];
                try
                {
                    IParameterReact ipc = (IParameterReact)this.GetType().Assembly.GetType(paramName).GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                    ipc.IsSetParameter = bool.Parse(param[1]);
                    ipc.Value = param[2];

                    Trace.TraceInformation("Loaded parameter class: ", paramName);
                    ParameterList.Add(ipc);
                }
                catch (ArgumentException exception)
                {
                    Trace.TraceError("Could not find this IParameterReact: ", paramName);
                    Debug.WriteLine(exception);
                }
                catch (Exception exception)
                {
                    Trace.TraceError("Could not load this IParameterReact: ", paramName);
                    Debug.WriteLine(exception);
                }
            }
        }

        /// <summary>
        /// the current parameter IDictionary for this reaction.
        /// </summary>
        /// <remarks>Must be done before calling calculate as the parameters influence the calculation outcome.</remarks>
        public IList<IParameterReact> ParameterList { get; set; }

        /// <summary>
        /// Return the IParameterReact if it exists given the class.
        ///
        /// <param name="paramClass">The class</param>
        /// <returns>The IParameterReact</returns>
        /// </summary>
        public IParameterReact GetParameterClass(Type paramClass)
        {
            foreach (var ipr in ParameterList)
            {
                if (ipr.GetType().Equals(paramClass)) return ipr;
            }

            return null;
        }
    }
}
