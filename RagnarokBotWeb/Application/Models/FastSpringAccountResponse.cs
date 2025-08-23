namespace RagnarokBotWeb.Application.Models
{
    public class FastSpringAccountResponseRoot
    {
        public class Address
        {
            public string AddressLine1 { get; set; }
            public string AddressLine2 { get; set; }
            public string City { get; set; }
            public string Region { get; set; }
            public string RegionCustom { get; set; }
            public int? PostalCode { get; set; }
            public string Company { get; set; }
        }

        public class Charge
        {
            public object Currency { get; set; }
            public object Total { get; set; }
            public object PayoutCurrency { get; set; }
            public object TotalInPayoutCurrency { get; set; }
            public object Status { get; set; }
            public object Order { get; set; }
            public object OrderReference { get; set; }
            public object Subscription { get; set; }
            public long Timestamp { get; set; }
            public long TimestampValue { get; set; }
            public object TimestampInSeconds { get; set; }
            public object TimestampDisplay { get; set; }
            public object TimestampDisplayISO8601 { get; set; }
        }

        public class Contact
        {
            public string First { get; set; }
            public string Last { get; set; }
            public string Email { get; set; }
            public string Company { get; set; }
            public string Phone { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Lookup
        {
            public object Global { get; set; }
            public object Custom { get; set; }
        }

        public class Payment
        {
            public object Methods { get; set; }
            public object Active { get; set; }
        }

        public class FastSpringAccountResponse
        {
            public string Id { get; set; }
            public string Account { get; set; }
            public string Action { get; set; }
            public Contact Contact { get; set; }
            public object Address { get; set; }
            public string Language { get; set; }
            public string Country { get; set; }
            public Lookup Lookup { get; set; }
            public string Url { get; set; }
            public Payment Payment { get; set; }
            public List<object> Orders { get; set; }
            public List<object> Subscriptions { get; set; }
            public List<Charge> Charges { get; set; }
            public bool Subscribed { get; set; }
            public object Result { get; set; }
            public object TaxExemptionData { get; set; }
        }

    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);


}
