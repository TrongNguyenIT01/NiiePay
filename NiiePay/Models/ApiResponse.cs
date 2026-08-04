using System.Text.Json.Serialization;

namespace NiiePay.Models
{
    public class ApiResponse
    {
        public string Status { get; set; } = null!;

        // Bỏ qua thuộc tính này trong JSON nếu giá trị là null (trường hợp tạo thất bại)
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SoTaiKhoan { get; set; }

        public string Message { get; set; } = null!;
    }
}
