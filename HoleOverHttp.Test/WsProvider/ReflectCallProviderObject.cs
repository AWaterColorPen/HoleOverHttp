using System;
using System.Collections.Generic;
using System.Threading;

namespace HoleOverHttp.Test.WsProvider
{
    public class ReflectCallProviderObject
    {
        public bool NoParameterMethod()
        {
            return true;
        }

        public bool NullableParameterMethod(int p1, int? p2)
        {
            return p1 == 0 && p2 == 0;
        }

        public bool MixedParameterMethod(int p1, IDictionary<string, int> p2, DummyClass p3)
        {
            return p2 != null && p2.ContainsKey("key") && p2["key"] == 0 && p3 != null && p3.P1 && p3.P2;
        }

        public bool StringMethod(string p1)
        {
            return !string.IsNullOrEmpty(p1) && p1 == "right";
        }

        public bool BoolMethod(bool p1, bool? p2)
        {
            return p1 && p2 == true;
        }

        public bool DateTimeOffsetMethod(DateTimeOffset p1, DateTimeOffset? p2)
        {
            return p1 < DateTimeOffset.UtcNow && p2 < DateTimeOffset.Now;
        }

        public int TimeOutMethod(int sleepTime, int uid)
        {
            Thread.Sleep(sleepTime);
            return uid;
        }
    }
    
    public class DummyClass
    {
        public bool P1;

        public bool P2;
    }
}
