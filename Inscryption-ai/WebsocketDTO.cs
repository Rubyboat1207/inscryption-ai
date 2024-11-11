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

        [JsonPropertyName("result")]
        public JsonElement Result { get; set; }

        [JsonPropertyName("params")]
        public JsonElement Params { get; set; }
    }

    /**
     * {
    "path": "actions/register",
    "name": "test",
    "description": "does nothing",
    "schema": {
        "type": "object",
        "properties": {}
    }
}
     */
    public class RegisterAction
    {
        [JsonPropertyName("type")] public string Type => "actions/register";
        [JsonPropertyName("name")] public string Name;
        [JsonPropertyName("description")] public string Description;
        [JsonPropertyName("schema")] public JsonElement Schema;

        public RegisterAction(string name, string description, JsonElement schema)
        {
            Name = name;
            Description = description;
            Schema = schema;
        }
    }

// Factory method to parse and return the appropriate response type based on the "type" field
    public static class WebSocketResponseFactory
    {
        public static WebSocketResponse ParseResponse(string json)
        {
            return JsonSerializer.Deserialize<WebSocketResponse>(json);
        }
    }
}