/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NCDK.Dict
{
    /// <summary>
    /// Database of dictionaries listing entries with compounds, fragments
    /// and entities.
    /// </summary>
    // @author     Egon Willighagen
    // @cdk.githash
    // @cdk.created    2003-04-06
    // @cdk.keyword    dictionary
    // @cdk.module     dict
    public class DictionaryDatabase
    {
        private static readonly IDictionary<string, EntryDictionary> cache = new Dictionary<string, EntryDictionary>();

        public const string DictRefPropertyName = "NCDK.Dict";

        private string[] dictionaryNames = {"chemical", "elements", "descriptor-algorithms", "reaction-processes" };
        private string[] dictionaryTypes = { "xml", "owl", "owl", "owl_React" };

        private IDictionary<string, EntryDictionary> dictionaries;

        public DictionaryDatabase()
        {
            // read dictionaries distributed with CDK
            dictionaries = new Dictionary<string, EntryDictionary>();
            for (int i = 0; i < dictionaryNames.Length; i++)
            {
                string name = dictionaryNames[i];
                string type = dictionaryTypes[i];
                EntryDictionary dictionary = ReadDictionary("NCDK.Dict.Resources.Data." + name, type);
                if (dictionary != null)
                {
                    dictionaries.Add(name.ToLowerInvariant(), dictionary);
                    Debug.WriteLine("Read dictionary: ", name);
                }
            }
        }

        private EntryDictionary ReadDictionary(string databaseLocator, string type)
        {
            EntryDictionary dictionary;
            // to distinguish between OWL: QSAR & REACT
            if (type.Contains("owl"))
            {
                if (type.Contains("_React"))
                    databaseLocator += "_" + type.Substring(0, type.Length - 6) + ".xml";
                else
                    databaseLocator += "_" + type + ".xml";
            }
            else
            {
                databaseLocator += "." + type;
            }
            if (cache.ContainsKey(databaseLocator))
            {
                return cache[databaseLocator];
            }
            else
            {
                Trace.TraceInformation("Reading dictionary from ", databaseLocator);
                try
                {
                    var reader = new StreamReader(ResourceLoader.GetAsStream(databaseLocator));
                    if (type.Equals("owl"))
                    {
                        dictionary = OWLFile.Unmarshal(reader);
                    }
                    else if (type.Equals("owl_React"))
                    {
                        dictionary = OWLReact.Unmarshal(reader);
                    }
                    else
                    { // assume XML using Castor
                        dictionary = EntryDictionary.Unmarshal(reader);
                    }
                }
                catch (Exception exception)
                {
                    dictionary = null;
                    Trace.TraceError($"Could not read dictionary {databaseLocator}");
                    Debug.WriteLine(exception);
                }
                cache[databaseLocator] = dictionary;
                return dictionary;
            }
        }

        /// <summary>
        /// Reads a custom dictionary into the database.
        /// </summary>
        /// <param name="reader">The reader from which the dictionary data will be read</param>
        /// <param name="name">The name of the dictionary</param>
        public void ReadDictionary(TextReader reader, string name)
        {
            name = name.ToLowerInvariant();
            Debug.WriteLine($"Reading dictionary: {name}");
            if (!dictionaries.ContainsKey(name))
            {
                try
                {
                    EntryDictionary dictionary = EntryDictionary.Unmarshal(reader);
                    dictionaries.Add(name, dictionary);
                    Debug.WriteLine("  ... loaded and stored");
                }
                catch (Exception exception)
                {
                    Trace.TraceError($"Could not read dictionary: {name}");
                    Debug.WriteLine(exception);
                }
            }
            else
            {
                Trace.TraceError($"Dictionary already loaded: {name}");
            }
        }

        /// <summary>
        /// Returns a string[] with the names of the known dictionaries.
        /// </summary>
        /// <returns>The names of the dictionaries</returns>
        public string[] GetDictionaryNames()
        {
            return dictionaryNames;
        }

        public EntryDictionary GetDictionary(string dictionaryName)
        {
            return dictionaries[dictionaryName];
        }

        /// <summary>
        /// Returns a string[] with the id's of all entries in the specified database.
        /// </summary>
        /// <returns>The entry names for the specified dictionary</returns>
        /// <param name="dictionaryName">The name of the dictionary</param>
        public IEnumerable<string> GetDictionaryEntries(string dictionaryName)
        {
            EntryDictionary dictionary = GetDictionary(dictionaryName);
            if (dictionary == null)
            {
                Trace.TraceError("Cannot find requested dictionary");
            }
            else
            {
                // FIXME: dummy method that needs an implementation
                foreach (var entry in dictionary.Entries)
                    yield return entry.Label;
            }
            yield break;
        }

        public IEnumerable<Entry> GetDictionaryEntry(string dictionaryName)
        {
            EntryDictionary dictionary = dictionaries[dictionaryName];
            return dictionary.Entries;
        }

        /// <summary>
        /// Returns true if the database contains the dictionary.
        /// </summary>
        public bool HasDictionary(string name)
        {
            return dictionaries.ContainsKey(name.ToLowerInvariant());
        }

        public IEnumerable<string> ListDictionaries()
        {
            return dictionaries.Keys;
        }

        /// <summary>
        /// Returns true if the given dictionary contains the given entry.
        /// </summary>
        public bool HasEntry(string dictName, string entryID)
        {
            if (HasDictionary(dictName))
            {
                EntryDictionary dictionary = dictionaries[dictName];
                return dictionary.ContainsKey(entryID.ToLowerInvariant());
            }
            else
            {
                return false;
            }
        }
    }
}
