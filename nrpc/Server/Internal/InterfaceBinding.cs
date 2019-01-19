using System.Reflection;
using System.Collections.Generic;

namespace nRpc.Server.Internal
{
    internal class InterfaceBinding
    {
        public InterfaceBinding(object instance, IDictionary<MethodIdentity,MethodInfo> methods)
        {
            Instance = instance;
            Methods = methods;
        }

        public object Instance;

        public IDictionary<MethodIdentity,MethodInfo> Methods; 
    }
}