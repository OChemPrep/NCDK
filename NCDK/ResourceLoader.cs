// Copyright (C) 2017  Kazuya Ujihara
// This file is under LGPL-2.1 

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NCDK.Graphs.Invariant;
using NCDK.Layout;
using UnityEngine;

namespace NCDK
{
    public static class ResourceLoader
    {
#if true
        private static Stream GetAsUnityStream(string name)
        {
            var fileExt = Path.GetExtension(name);
            var pathComponents = name.Split('.');

            int ResourcesIdx = 0;
            for (int i = 0; i < pathComponents.Length; i++)
            {
                if(pathComponents[i].Equals("Resources"))
                {
                    ResourcesIdx = i;
                    break;
                }
            }

            // Make a path this is relative the the Resources folder and doesn't
            // include the file extension.
            string fullPath = string.Empty;
            for (int i = ResourcesIdx + 1; i < pathComponents.Length - 1; i++)
            {
                fullPath = Path.Combine(fullPath, pathComponents[i]);
            }

            UnityEngine.Debug.Log("Loading resource " + name + " as " + fullPath);

            try
            {
                var o = Resources.Load(fullPath) as TextAsset;

                MemoryStream memStream = new MemoryStream(o.bytes);
                return memStream;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.Log(exception.Message);
            }

            UnityEngine.Debug.Log(name + " not found.");
            return null;

        }
#else
        private static Stream GetAsUnityStream(string name)
        {
            {
                var fileExt = Path.GetExtension(name);
                var pathComponents = name.Split('.');
                var fileName = pathComponents[pathComponents.Length - 2];

                var fullPath = Path.Combine(Application.dataPath, "NCDK");
                if (pathComponents.Length > 2)
                {
                    for (int i = 0; i < pathComponents.Length - 2; i++)
                    {
                        fullPath = Path.Combine(fullPath, pathComponents[i]);
                    }
                }

                fullPath = Path.Combine(fullPath, fileName + fileExt);

                UnityEngine.Debug.Log("Loading data file from: " + fullPath);

                name = fullPath;
            }

            if (File.Exists(name))
            {
                UnityEngine.Debug.Log(name + " found.");
                try
                {
                    var srm = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read);
                    UnityEngine.Debug.Log("Loaded stream from " + name);
                    return srm;
                }
                catch (Exception exception)
                {
                    Trace.TraceInformation(exception.Message);
                }
            }

            Trace.TraceInformation($"Resource not found {name}.");
            return null;

        }
#endif

        public static Stream GetAsStream(string name)
        {
            return GetAsUnityStream(name);
        }

        public static Stream GetAsStream(Type type, string name)
        {
            if(type == typeof(IdentityTemplateLibrary))
            {
                string relativePath = "NCDK.Layout.Resources." + name;
                return GetAsUnityStream(relativePath);
            }
            else if(type == typeof(Canon))
            {
                string relativePath = "NCDK.Graphs.Invariant.Resources." + name;
                return GetAsUnityStream(relativePath);
            }

            Trace.TraceInformation($"Resource not found {name}.");
            return null;
        }
    }
}
