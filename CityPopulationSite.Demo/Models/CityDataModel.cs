using Newtonsoft.Json;

namespace CityPopulationSite.Demo.Models
{
    public class CityDataModel 
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("loc")]
        public double[] Loc { get; set; }

        [JsonProperty("pop")]
        public long Pop { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("_id")]
        public int Id { get; set; }

    }

}
