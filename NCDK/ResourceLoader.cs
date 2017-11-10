// Copyright (C) 2017  Kazuya Ujihara
// This file is under LGPL-2.1 

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NCDK.Layout;
using UnityEngine;

namespace NCDK
{
    public static class ResourceLoader
    {
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
        public static Stream GetAsStream(string name)
        {
            return GetAsUnityStream(name);
        }

        public static Stream GetAsStream(Type type, string name)
        {
            if(type == typeof(IdentityTemplateLibrary))
            {
                string relativePath = "NCDK/Layout/" + name;
                return GetAsUnityStream(relativePath);
            }

            Trace.TraceInformation($"Resource not found {name}.");
            return null;
        }
    }
}
