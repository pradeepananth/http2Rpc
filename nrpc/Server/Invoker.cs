using System;
using System.Collections.Generic;
using System.Reflection;
using nRpc.Server.Internal;
using nRpc.Utils;

namespace nRpc.Server
{
    public class Invoker
    {
        private Dictionary<string,InterfaceBinding> Map = new Dictionary<string, InterfaceBinding>();

        public void Bind<T>(T instance)
        {
            ThrowIf.IsNull(nameof(instance), instance);
            if(!typeof(T).IsInterface)
            {
                throw new ArgumentException($"{nameof(T)} must be an interface", nameof(T));
            }

            var interfaceName = typeof(T).GetName();
            if (Map.TryGetValue(interfaceName, out var value))
            {
                if (instance.Equals(value.Instance))
                {
                    // Skipping due to binding same thing
                    return;
                }
                throw new ArgumentException($"{interfaceName} already has existing binding of {value.Instance}. Cannot bind {instance}.", nameof(instance));
            }

            AddMethods<T>(instance);
        }

        // NOTE: interfaceName expects value from GetName from TypeUtils
        public object Invoke(string interfaceName, string methodName, Message parameter=null)
        {
            if (Map.TryGetValue(interfaceName, out var interfaceBinding))
            {
                if (parameter == null)
                {
                    if (interfaceBinding.Methods.TryGetValue(new MethodIdentity(methodName), out var methodInfo))
                    {
                        return methodInfo.Invoke(interfaceBinding.Instance, new object[]{});
                    }
                    // TODO: make exception type more explicit
                    throw new Exception($"Could not find the binding of method {methodName} for the interface {interfaceName}");
                }
                else
                {
                    var parameterDeclaringType = parameter.GetType().DeclaringType;
                    if (interfaceBinding.Methods.TryGetValue(new MethodIdentity(methodName, parameterDeclaringType), out var methodInfo))
                    {
                        return methodInfo.Invoke(interfaceBinding.Instance, new object[]{parameter});
                    }
                    // TODO: make exception type more explicit
                    throw new Exception($"Could not find the binding of method {methodName} with parameter type of {parameterDeclaringType} for the interface {interfaceName}");
                }
            }
            // TODO: make exception type more explicit
            throw new Exception("Could not find binding for this interface");
        }

        private void AddMethods<T>(T instance)
        {
            var interfaceType = typeof(T);
            var interfaceMethods = interfaceType.GetAllMethodsOfType();
            
            foreach (var interfaceMethod in interfaceMethods)
            {
                var parameterInfo = interfaceMethod.GetParameters();
                if (!IsValidNrpcFunc(parameterInfo))
                {
                    throw new Exception($"Found method {interfaceMethod} that is not a valid nRPC function in interface {interfaceType}");
                }
                var methodIdentity = parameterInfo.Length == 1 ?
                    new MethodIdentity(interfaceMethod.Name, parameterInfo[0].GetType().DeclaringType) :
                    new MethodIdentity(interfaceMethod.Name);
                var instanceType = instance.GetType();
                var implementationMethod = instanceType.GetMethod(interfaceMethod.Name);
                UpdateMapping(instance, methodIdentity, implementationMethod);
            }
        }

        private static bool IsValidNrpcFunc(ParameterInfo[] parameterInfo) =>
            parameterInfo.Length == 0 || (parameterInfo.Length == 1 && parameterInfo[0].GetType().BaseType == typeof(Message));

        private void UpdateMapping<T>(T instance, MethodIdentity newMethod, MethodInfo newMethodInfo)
        {
            var key = typeof(T).GetName();
            if (Map.TryGetValue(key, out var value))
            {
                value.Methods.Add(newMethod, newMethodInfo);
            }
            else
            {
                Map.Add(key, new InterfaceBinding(
                    instance,
                    new Dictionary<MethodIdentity,MethodInfo>
                    {
                        {newMethod, newMethodInfo}
                    }
                ));
            }
        }
    }
}