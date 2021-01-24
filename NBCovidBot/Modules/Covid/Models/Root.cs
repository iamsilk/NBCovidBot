using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NBCovidBot.Modules.Covid.Models
{
    public class Root<T>
    {
        [JsonPropertyName("objectIdFieldName")]
        public string ObjectIdFieldName { get; set; }

        [JsonPropertyName("uniqueIdField")]
        public UniqueIdField UniqueIdField { get; set; }

        [JsonPropertyName("globalIdFieldName")]
        public string GlobalIdFieldName { get; set; }

        [JsonPropertyName("geometryType")]
        public string GeometryType { get; set; }

        [JsonPropertyName("spatialReference")]
        public SpatialReference SpatialReference { get; set; }

        [JsonPropertyName("fields")]
        public List<Field> Fields { get; set; }

        [JsonPropertyName("features")]
        public List<Feature<T>> Features { get; set; }
    }
}
