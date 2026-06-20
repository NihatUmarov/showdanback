namespace ShowDanWebApi.Core.DTO
{
    public class MapDto
    {

        public List<OsmGeometry> Geometry { get; set; } = new();
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class OsmCenter { public double Lat { get; set; } public double Lon { get; set; } }
    public class OsmGeometry { public double Lat { get; set; } public double Lon { get; set; } }
}
