using System.Text.Json.Serialization;

namespace NiiePay.Models
{
    internal class JsonIgnoreConditionAttribute : Attribute
    {
        private JsonIgnoreCondition whenWritingNull;

        public JsonIgnoreConditionAttribute(JsonIgnoreCondition whenWritingNull)
        {
            this.whenWritingNull = whenWritingNull;
        }
    }
}