using System;
using MessagePack;

namespace nRpc.Tests.Data
{

    [Serializable]
    [MessagePackObject]
    public class AnyObject
    {
        [Key(0)]
        public int anyInt { get; internal set; }
        [Key(1)]
        public string anyString { get; internal set; }

        public static bool operator == (AnyObject t, AnyObject that)
        {
            return t.Equals(that);
        }

        public static bool operator != (AnyObject t, AnyObject that)
        {
            return !t.Equals(that);
        }

        public override bool Equals(object obj) 
        {
            if(!(obj is AnyObject)) return false;
            var other = obj as AnyObject;
            return (this.anyInt == other.anyInt) && (this.anyString == other.anyString);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}