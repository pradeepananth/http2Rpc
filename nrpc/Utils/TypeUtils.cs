using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests")]
namespace nRpc.Utils
{
    internal static class TypeUtils
    {
        /// <summary>
        /// Gets all methods for a given type, including methods from inherited classes or interfaces
        /// </summary>
        public static MethodInfo[] GetAllMethodsOfType(this Type t)
        {
            var interfaces = t.GetInterfaces();
            if (interfaces.Length == 0) return t.GetMethods();
            var allMethods = new List<MethodInfo>(t.GetMethods());
            foreach (var @interface in interfaces)
            {
                allMethods.AddRange(@interface.GetMethods());
            }

            return allMethods.ToArray();
        }

        public static string GetName(this Type t) => t.FullName;
    }
}
