namespace DisplayAMap.Models
{
    public class EarthquakeInfo
    {
        // Keep property names matching the CSV headers
        public string Date { get; set; }
        public string Time { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Depth { get; set; }
        public double Magnitude { get; set; }
    }
}
