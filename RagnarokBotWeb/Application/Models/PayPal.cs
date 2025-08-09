namespace RagnarokBotWeb.Application.Models
{
    public class PayPal
    {
        // Modelo para configuração do PayPal
        public class PayPalConfig
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string BaseUrl { get; set; } // https://api.paypal.com para produção, https://api.sandbox.paypal.com para testes
        }

        // Modelos para requisições
        public class PayPalAccessToken
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
        }

        public class PayPalAmount
        {
            public string currency_code { get; set; } = "BRL";
            public string value { get; set; }
        }

        public class PayPalPurchaseUnit
        {
            public PayPalAmount amount { get; set; }
            public string description { get; set; }
        }

        public class PayPalApplicationContext
        {
            public string return_url { get; set; }
            public string cancel_url { get; set; }
            public string brand_name { get; set; }
            public string shipping_preference { get; set; }
            public string landing_page { get; set; } = "NO_PREFERENCE";
            public string user_action { get; set; } = "PAY_NOW";
        }

        public class PayPalCreateOrderRequest
        {
            public string intent { get; set; } = "CAPTURE";
            public List<PayPalPurchaseUnit> purchase_units { get; set; }
            public PayPalApplicationContext application_context { get; set; }
        }

        public class PayPalOrderResponse
        {
            public string id { get; set; }
            public string status { get; set; }
            public List<PayPalLink> links { get; set; }
        }

        public class PayPalLink
        {
            public string href { get; set; }
            public string rel { get; set; }
            public string method { get; set; }
        }
    }
}
