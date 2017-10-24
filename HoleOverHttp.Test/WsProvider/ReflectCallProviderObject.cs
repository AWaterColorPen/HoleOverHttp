using System;
using System.Collections.Generic;

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

        public bool MixedParameterMethod(int p1, IDictionary<string, int> p2)
        {
            return p2 != null && p2.ContainsKey("key") && p2["key"] == 0;
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
    }
}
