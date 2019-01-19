using System;
using System.Collections.Generic;

namespace nRpc.Server.Internal
{
    internal class MethodIdentity
    {
        public MethodIdentity(string methodName) 
        {
            ThrowIf.IsNullOrEmpty(nameof(methodName), methodName);
            
            MethodName = methodName;
        }

        public MethodIdentity(string methodName, Type messageType) 
        {
            ThrowIf.IsNullOrEmpty(nameof(methodName), methodName);

            MethodName = methodName;
            MessageType = messageType;
        }

        public string MethodName {  get; private set; }

        public Type MessageType { get; private set; }

        public override bool Equals(object obj)
        {
            var item = obj as MethodIdentity;

            if (item == null)
            {
                return false;
            }

            return MethodName.Equals(item.MethodName) && MessageType == item.MessageType;
        }

        public override int GetHashCode()
        {
            return MessageType != null ? MethodName.GetHashCode() ^ MessageType.GetHashCode() : MethodName.GetHashCode();
        }
    }
}