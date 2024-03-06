using Newtonsoft.Json;

namespace BigData_Project
{
    public class Pharmacy
    {
        public Pharmacy(string id, string tradingName, string addressLine1,
            string addressLine2, string addressLine3, string town, string country,
            string postcode, string? latitude = null, string? longitude = null)
        {
            Id = id;
            TradingName = tradingName;
            AddressLine1 = addressLine1;
            AddressLine2 = addressLine2;
            AddressLine3 = addressLine3;
            Town = town;
            Country = country;
            Postcode = postcode;
            Latitude = latitude;
            Longitude = longitude;
        }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; }

        public string TradingName { get; }

        public string AddressLine1 { get; }

        public string AddressLine2 { get; }

        public string AddressLine3 { get; }

        public string Town { get; }

        public string Country { get; }

        public string Postcode { get; }

        public string? Latitude { get; }

        public string? Longitude { get; }
    }
}

