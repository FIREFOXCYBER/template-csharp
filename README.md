# Mycroft C&#35;

## Install

Right now, the package isn't on NuGet. So to use it, just copy over `Client.cs` and `MessageEventHandler.cs` to your project and you will good to go. In the future, you can install the package via NuGet using visual studio.

An example Program.cs file would look something like this

``` csharp
 class Program
{
    static void Main(string[] args)
    {
    	string host;
    	string port;
        if (args.Length < 2)
        {
            host = args[0];
            port = args[1];
        } else {
            host = "localhost";
            port = "1847";
        }
        var server = new MyClient("app_manifest.json");
        server.Connect(host, port);
    }
}
```

## Overview

### Example App
``` csharp
public class SpeechClient : Client
{
    public MyClient(string manifest) : base(manifest)
    {
        handler.On("APP_MANIFEST_OK", AppManifestOk);
    }

    protected async void AppManifestOk(dynamic message)
    {
        InstanceId = message["instanceId"];
        await Up();
    }
}
```

### Description

The mycroft C# apps inherit from the Client class. To respond to different messages, just create methods for them and register them in the constructor using `handler.ON(MESSAGE_TYPE, METHODNAME)`.
In addition to any of the external message types, there are also 2 internal message types that you can handle, CONNECT and END, which are called with on connect and disconnect respectfully.

### Helper Methods

#### Up()
Sends `APP_UP` to mycroft

#### Down()
Sends `APP_DOWN` to mycroft

#### InUse(priority=30)
Sends `APP_IN_USE` to mycroft

#### Query(capability, action, data, instance_id = null, priority = 30)
Sends a `MSG_QUERY` to mycroft

#### Broadcast(content)
Sends a `MSG_BROADCAST` to mycroft

#### QuerySuccess(id, ret)
Sends a `MSG_QUERY_SUCCESS` to mycroft

#### QueryFail(id, message)
Sends a `MSG_QUERY_FAIL` to mycroft
