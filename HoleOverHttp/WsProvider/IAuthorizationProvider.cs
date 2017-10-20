namespace HoleOverHttp.WsProvider
{
    public interface IAuthorizationProvider
    {
        string Key { get; }
        string Value { get; }
    }
}
