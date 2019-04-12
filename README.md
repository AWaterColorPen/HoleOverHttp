# HoleOverHttp

[![NuGet version](https://badge.fury.io/nu/HoleOverHttp.svg)](https://badge.fury.io/nu/HoleOverHttp)

Library to help providing server api and connection to client. 
Implementation in C#, targeting .NET Standard 2.0+. 

The library can help server acoss network security groups and firewall to provider api and connection to multi clients.

## Usage

 * Install 
```shell
Install-Package HoleOverHttp
```

 * Provider Connection
```cs
IAuthorizationProvider authorizationProvider = new Mock<IAuthorizationProvider>().Object;
var providerConnection = new WebSocketProviderConnection(host: "localhost:23333", namespace: "namespace", tokenProvider: authorizationProvider);

// set secure
providerConnection.Secure = false;
```

 * Service Object as a class
```cs
public class ServiceObject
{
    public int MethodName(int param)
    {
        return param * param;
    }
}
```

 * Server Provider
```cs
// register service and connection
var serverProvider = new ReflectCallProvider();
serverProvider.RegisterConnection(providerConnection);
serverProvider.RegisterService(new ServiceObject());

// run serve async
var tokenSource = new CancellationTokenSource();
await serverProvider.ServeAsync(tokenSource.Token);
```
* Client Call Registry
```cs
// create a call registry instance 
var callConnectionPool = new ReusableCallConnectionPool();
// have to implementation your own CallRegistry instance
var webListenerCallRegistry = new WebListenerCallRegistry(callConnectionPool: callConnectionPool, prefixes: new[] { "http://localhost:23333/ws/" }));

// enable remote register
var tokenSource = new CancellationTokenSource();
webListenerCallRegistry.RegisterRemoteSocket(tokenSource.Token);
```

* Client Call
```cs
var result = callConnectionPool.CallAsync(namespace: "namespace", method: "MethodName", param: Encoding.UTF8.GetBytes("{param:0}")).Result;
```
