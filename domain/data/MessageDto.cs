using Newtonsoft.Json;

public class MessageDto {

    [JsonProperty ( "action" )]
    public string Action { get; private set; }

    [JsonProperty ( "body" )]
    public string Body { get; private set; }

    [JsonProperty ( "clientNodeId" )]
    public string ClientNodeId { get; private set; }

    [JsonProperty ( "localReaderName" )]
    public string LocalReaderName { get; private set; }

    [JsonProperty ( "remoteReaderName" )]
    public string RemoteReaderName { get; private set; }

    [JsonProperty ( "serverNodeId" )]
    public string ServerNodeId { get; private set; }

    [JsonProperty ( "sessionId" )]
    public string SessionId { get; private set; }

    public MessageDto SetAction ( string action )
    {
        this.Action = action;
        return this;
    }

    public MessageDto SetBody ( string body )
    {
        this.Body = body;
        return this;
    }

    public MessageDto SetClientNodeId ( string clientNodeId )
    {
        this.ClientNodeId = clientNodeId;
        return this;
    }

    public MessageDto SetLocalReaderName ( string localReaderName )
    {
        this.LocalReaderName = localReaderName;
        return this;
    }

    public MessageDto SetRemoteReaderName ( string remoteReaderName )
    {
        this.RemoteReaderName = remoteReaderName;
        return this;
    }

    public MessageDto SetServerNodeId ( string serverNodeId )
    {
        this.ServerNodeId = serverNodeId;
        return this;
    }

    public MessageDto SetSessionId ( string sessionId )
    {
        this.SessionId = sessionId;
        return this;
    }
}
