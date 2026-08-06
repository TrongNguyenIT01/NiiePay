namespace NiiePay.Models
{
    using System.Text.Json.Serialization;
    public class SavingResponse
    {

        public string Status { get; set; } = string.Empty;

     
        [JsonIgnoreCondition(JsonIgnoreCondition.WhenWritingNull)]
        public string? MaSoTietKiem { get; set; }

        [JsonIgnoreCondition(JsonIgnoreCondition.WhenWritingNull)]
        public int? KyHan { get; set; }

        [JsonIgnoreCondition(JsonIgnoreCondition.WhenWritingNull)]
        public double? LaiSuat { get; set; }

        [JsonIgnoreCondition(JsonIgnoreCondition.WhenWritingNull)]
        public string? NgayBatDau { get; set; }

        [JsonIgnoreCondition(JsonIgnoreCondition.WhenWritingNull)]
        public string? NgayHetHan { get; set; }

        [JsonIgnoreCondition(JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

    }
}
