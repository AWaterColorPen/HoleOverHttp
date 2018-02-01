using HoleOverHttp.Core;

namespace HoleOverHttp.Test.E2E
{
    internal class DummyAuthorizationProvider : IAuthorizationProvider
    {
        public string Key { get; } = "Key";
        public string Value { get; } = "Value";
    }
}