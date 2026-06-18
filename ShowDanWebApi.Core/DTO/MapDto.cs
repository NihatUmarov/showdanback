namespace ShowDanWebApi.Core.DTO
{
    public class MapDto
    {

        // Запросы
        public record RegionListRequest(string CountryCode);
        public record CityListRequest(int RegionId);

        public record IdentifyCityRequest(double Lat, double Lon);
        // Ответы
        public record CountryResponse(int Id, string? IsoCode, string? Name);
        public record RegionResponse(int Id, string? Name);
        public record CityResponse(int Id, string? Name, double? Lat, double? Lon);

     
    }

    public class OsmResponse
    {
        public List<OsmElement> Elements { get; set; } = new();
    }

    public class OsmElement
    {
        public string Type { get; set; } = string.Empty;
        public long Id { get; set; }
        public OsmCenter Center { get; set; } = new();
        public List<OsmGeometry> Geometry { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class OsmCenter { public double Lat { get; set; } public double Lon { get; set; } }
    public class OsmGeometry { public double Lat { get; set; } public double Lon { get; set; } }
}
