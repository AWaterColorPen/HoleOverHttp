# HoleOverHttp

[![NuGet version](https://badge.fury.io/nu/HoleOverHttp.svg)](https://badge.fury.io/nu/HoleOverHttp)

Library to help providing server api and connection to client. 
Implementation in C#, targeting .NET Standard 2.0+. 

The library can help server acoss network security groups and firewall to provider api and connection to multi clients.

## Usage

 * Install 
```
Install-Package HoleOverHttp
```

 * Provider Connection
```
IAuthorizationProvider authorizationProvider = new Mock<IAuthorizationProvider>().Object;
var providerConnection = new WebSocketProviderConnection(host: "localhost:23333", namespace: "namespace", tokenProvider: authorizationProvider);

// set secure
providerConnection.Secure = false;
```

 * Service Object as a class
```
public class ServiceObject
{
    public int MethodName(int param)
    {
        return param * param;
    }
}
```

 * Server Provider
```
// register service and connection
var serverProvider = new ReflectCallProvider();
serverProvider.RegisterConnection(providerConnection);
serverProvider.RegisterService(new ServiceObject());

// run serve async
var tokenSource = new CancellationTokenSource();
await serverProvider.ServeAsync(tokenSource.Token);
```

* Client Call
```
var callConnectionPool = new ReusableCallConnectionPool();
var result = callConnectionPool.CallAsync(namespace: "namespace", method: "MethodName", param: Encoding.UTF8.GetBytes("{param:0}")).Result;
```
