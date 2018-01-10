namespace HoleOverHttp.Core
{
    public interface IAuthorizationProvider
    {
        string Key { get; }

        string Value { get; }
    }
}
