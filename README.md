# HoleOverHttp

[![NuGet version](https://badge.fury.io/nu/HoleOverHttp.svg)](https://badge.fury.io/nu/HoleOverHttp)

Library to help providing server api and connection to client. 
Implementation in C#, targeting .NET Standard 2.0+ (Frameworks 4.5+, Core 2.0+). 

The library can help server acoss network security group and firewall to provider api and connection to client.

## Usage

 * Install 
```
Install-Package HoleOverHttp
```

 * Server Provider
```
var serverProvider = new ReflectCallProviderConnection(host: "localhost:23333", namespace: "namespace");
var serverProvider.RegisterService(new ServiceObject());
await serverProvider.ServeAsync(cancellationToken);
```

* Client Call
```
var callConnectionPool = new ReusableCallConnectionPool();
var result = callConnectionPool.CallAsync(namespace: "namespace", method: "MethodName", param: Encoding.UTF8.GetBytes("{param:0}")).Result;
```
