using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inscryption_ai
{
    public class WebSocketResponse
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("action_id")]
        public string ActionId { get; set; }
        
        [JsonPropertyName("action_name")]
        public string ActionName { get; set; }

        [JsonPropertyName("result")]
        public JsonElement Result { get; set; }

        [JsonPropertyName("params")]
        public string Params { get; set; }
    }

    public class RegisterAction
    {
        [JsonPropertyName("path")] public string Type => "actions/register";
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("schema")] public Dictionary<string, object> Schema { get; set; }

        public RegisterAction(string name, string description, Dictionary<string, object> schema)
        {
            Name = name;
            Description = description;
            Schema = schema;
        }
    }

    public class EphemeralAction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Schema { get; set; }

        public EphemeralAction(string name, string description, Dictionary<string, object> schema)
        {
            this.Name = name;
            this.Description = description;
            this.Schema = schema;
        }
    }
    
    public class RegisterEphemeralActionGroup
    {
        [JsonPropertyName("path")] public string Type => "actions/register/ephemeral";
        [JsonPropertyName("actions")] public List<EphemeralAction> Actions { get; set; }

        public RegisterEphemeralActionGroup(List<EphemeralAction> actions)
        {
            Actions = actions;
        }
    }

    public class ActionResponse
    {
        [JsonPropertyName("path")] public string Type => "action/result";
        [JsonPropertyName("action_id")] public string ActionId { get; set; }
        [JsonPropertyName("result")] public string Result { get; set; }

        public ActionResponse(string actionId, string result)
        {
            ActionId = actionId;
            Result = result;
        }
    }

    public class AddEnvironmentContext
    {
        [JsonPropertyName("path")] public string Type => "context/environment";
        [JsonPropertyName("value")] public string Value { get; set; }

        public AddEnvironmentContext(string value)
        {
            Value = value;
        }
    }
    
    public class RequestAction
    {
        [JsonPropertyName("path")] public string Type => "actions/request";
    }
    
    public class ForceAction
    {
        [JsonPropertyName("path")] public string Type => "actions/force";
        [JsonPropertyName("name")] public string Name { get; set; }

        public ForceAction(string name)
        {
            Name = name;
        }
    }

    public static class WebSocketResponseFactory
    {
        public static WebSocketResponse ParseResponse(string json)
        {
            return JsonSerializer.Deserialize<WebSocketResponse>(json);
        }
    }
}