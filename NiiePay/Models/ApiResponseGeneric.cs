using System.Text.Json.Serialization;

namespace NiiePay.Models
{
    // Generic API response wrapper for query endpoints
    public class ApiResponseGeneric
    {
        public string Status { get; set; } = null!;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }

        public string Message { get; set; } = null!;

        // Optional total count (for search results)
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Count { get; set; }
    }
}
