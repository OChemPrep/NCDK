﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace NCDK
{
    public static class ResourceLoader
    {
        public static Stream GetAsStream(string name)
        {
            {
                var asm = Assembly.GetCallingAssembly();
                var srm = asm.GetManifestResourceStream(name);
                if (srm != null)
                    return srm;
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var srm = asm.GetManifestResourceStream(name);
                    if (srm != null)
                        return srm;
                }
                catch (Exception)
                {
                    // ignore
                }
            }

            if (File.Exists(name))
            {
                try
                {
                    var srm = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return srm;
                }
                catch (Exception exception)
                {
                    Trace.TraceError(exception.Message);
                }
            }

            Trace.TraceInformation($"Resource not found {name}.");
            return null;
        }

        public static Stream GetAsStream(Assembly asm, string name)
        {
            var srm = asm.GetManifestResourceStream(name);
            if (srm == null)
                Trace.TraceInformation($"Resource not found {name}.");
            return srm;
        }

        public static Stream GetAsStream(Type type, string name)
        {
            var asm = type.Assembly;
            var srm = asm.GetManifestResourceStream(type, name);
            if (srm == null)
                Trace.TraceInformation($"Resource not found {name}.");
            return srm;
        }
    }
}
