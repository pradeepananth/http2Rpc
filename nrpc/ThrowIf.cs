using System;

namespace nRpc
{
    public class ThrowIf
    {
        public static void IsNull(string paramName, object obj)
        {
            if (obj == null) Throw(paramName);        
        }
        
        public static void IsNullOrEmpty(string paramName, string obj)
        {
            if(string.IsNullOrEmpty(obj)) Throw(paramName);
        }

        private static void Throw(string paramName)
        {
            throw new ArgumentNullException(paramName, $"Parameter {paramName} cannot be null");
        }

        public static void IsNullOrEmpty(string paramName, byte[] content)
        {
            if(content == null || content.Length == 0) Throw(paramName);
        }
    }
}