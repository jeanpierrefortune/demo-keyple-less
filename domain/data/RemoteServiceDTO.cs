using Newtonsoft.Json;

public class InputData {
}

public class BodyContent {
    [JsonProperty ( "serviceId" )]
    public string ServiceId { get; set; }

    [JsonProperty ( "inputData" )]
    public InputData InputData { get; set; }
}

public class RemoteServiceDTO {
    [JsonProperty ( "action" )]
    public string Action { get; set; }

    [JsonProperty ( "body" )]
    public string Body { get; set; }

    [JsonProperty ( "clientNodeId" )]
    public string ClientNodeId { get; set; }

    [JsonProperty ( "localReaderName" )]
    public string LocalReaderName { get; set; }

    [JsonProperty ( "sessionId" )]
    public string SessionId { get; set; }
}
