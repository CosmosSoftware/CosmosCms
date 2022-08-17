using System.Text.Json.Serialization;

namespace Cosmos.Cms.Models
{
    public class CdnPurgeViewModel
    {
        // Example return: "{\"detail\": \"Request accepted\", \"estimatedSeconds\": 5, \"purgeId\": \"eda4653e-2379-11eb-9bda-9dd6666ed213\", \"supportId\": \"17PY1605029214948278-223626432\", \"httpStatus\": 201}"
        [JsonPropertyName("purgeId")] public string PurgeId { get; set; }

        [JsonPropertyName("detail")] public string Detail { get; set; }

        [JsonPropertyName("estimatedSeconds")] public int EstimatedSeconds { get; set; }

        [JsonPropertyName("supportId")] public string SupportId { get; set; }

        [JsonPropertyName("httpStatus")] public string HttpStatus { get; set; }
    }
}