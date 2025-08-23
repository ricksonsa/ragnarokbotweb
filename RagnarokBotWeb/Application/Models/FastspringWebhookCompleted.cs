namespace RagnarokBotWeb.Application.Models
{

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Address
    {
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Display { get; set; }
    }

    public class Customer
    {
        public string? First { get; set; }
        public string? Last { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public bool? Subscribed { get; set; }
    }

    public class Data
    {
        public string? Order { get; set; }
        public string? Id { get; set; }
        public string? Reference { get; set; }
        public object? BuyerReference { get; set; }
        public string? IpAddress { get; set; }
        public bool? Completed { get; set; }
        public long Changed { get; set; }
        public long ChangedValue { get; set; }
        public int? ChangedInSeconds { get; set; }
        public string? ChangedDisplay { get; set; }
        public string? ChangedDisplayISO8601 { get; set; }
        public string? ChangedDisplayEmailEnhancements { get; set; }
        public string? ChangedDisplayEmailEnhancementsWithTime { get; set; }
        public string? Language { get; set; }
        public bool? Live { get; set; }
        public string? Currency { get; set; }
        public string? PayoutCurrency { get; set; }
        public object? Quote { get; set; }
        public string? InvoiceUrl { get; set; }
        public string? SiteId { get; set; }
        public string? Account { get; set; }
        public double? Total { get; set; }
        public string? TotalDisplay { get; set; }
        public double? TotalInPayoutCurrency { get; set; }
        public string? TotalInPayoutCurrencyDisplay { get; set; }
        public double? Tax { get; set; }
        public string? TaxDisplay { get; set; }
        public double? TaxInPayoutCurrency { get; set; }
        public string? TaxInPayoutCurrencyDisplay { get; set; }
        public double? Subtotal { get; set; }
        public string? SubtotalDisplay { get; set; }
        public double? SubtotalInPayoutCurrency { get; set; }
        public string? SubtotalInPayoutCurrencyDisplay { get; set; }
        public double? Discount { get; set; }
        public string? DiscountDisplay { get; set; }
        public double? DiscountInPayoutCurrency { get; set; }
        public string? DiscountInPayoutCurrencyDisplay { get; set; }
        public double? DiscountWithTax { get; set; }
        public string? DiscountWithTaxDisplay { get; set; }
        public double? DiscountWithTaxInPayoutCurrency { get; set; }
        public string? DiscountWithTaxInPayoutCurrencyDisplay { get; set; }
        public string? BillDescriptor { get; set; }
        public Payment? Payment { get; set; }
        public Customer? Customer { get; set; }
        public Address? Address { get; set; }
        public List<Recipient1?>? Recipients { get; set; }
        public List<object?>? Notes { get; set; }
        public List<Item>? Items { get; set; }
    }

    public class Event
    {
        public string? Id { get; set; }
        public bool? Processed { get; set; }
        public long Created { get; set; }
        public string? Type { get; set; }
        public bool? Live { get; set; }
        public Data? Data { get; set; }
    }

    public class Fulfillments
    {
    }

    public class Item
    {
        public string? Product { get; set; }
        public int? Quantity { get; set; }
        public string? Display { get; set; }
        public object? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public string? ShortDisplay { get; set; }
        public double? Subtotal { get; set; }
        public string? SubtotalDisplay { get; set; }
        public double? SubtotalInPayoutCurrency { get; set; }
        public string? SubtotalInPayoutCurrencyDisplay { get; set; }
        public double? Discount { get; set; }
        public string? DiscountDisplay { get; set; }
        public double? DiscountInPayoutCurrency { get; set; }
        public string? DiscountInPayoutCurrencyDisplay { get; set; }
        public bool? IsAddon { get; set; }
        public Fulfillments Fulfillments { get; set; }
        public Withholdings Withholdings { get; set; }
        public double? ProratedItemChangeAmount { get; set; }
        public string? ProratedItemChangeAmountDisplay { get; set; }
        public double? ProratedItemChangeAmountInPayoutCurrency { get; set; }
        public string? ProratedItemChangeAmountInPayoutCurrencyDisplay { get; set; }
        public double? ProratedItemProratedCharge { get; set; }
        public string? ProratedItemProratedChargeDisplay { get; set; }
        public double? ProratedItemProratedChargeInPayoutCurrency { get; set; }
        public string? ProratedItemProratedChargeInPayoutCurrencyDisplay { get; set; }
        public double? ProratedItemCreditAmount { get; set; }
        public string? ProratedItemCreditAmountDisplay { get; set; }
        public double? ProratedItemCreditAmountInPayoutCurrency { get; set; }
        public string? ProratedItemCreditAmountInPayoutCurrencyDisplay { get; set; }
        public double? ProratedItemTaxAmount { get; set; }
        public string? ProratedItemTaxAmountDisplay { get; set; }
        public double? ProratedItemTaxAmountInPayoutCurrency { get; set; }
        public string? ProratedItemTaxAmountInPayoutCurrencyDisplay { get; set; }
        public double? ProratedItemTotal { get; set; }
        public string? ProratedItemTotalDisplay { get; set; }
        public double? ProratedItemTotalInPayoutCurrency { get; set; }
        public string? ProratedItemTotalInPayoutCurrencyDisplay { get; set; }
    }

    public class Payment
    {
        public string? Type { get; set; }
        public string? CardEnding { get; set; }
    }

    public class Recipient1
    {
        public Recipient1? Recipient { get; set; }
    }

    public class Recipient2
    {
        public string? First { get; set; }
        public string? Last { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public bool? Subscribed { get; set; }
        public string? Account { get; set; }
        public Address? Address { get; set; }
    }

    public class FastSpringWebhookCompleted
    {
        public List<Event> Events { get; set; }
    }

    public class Withholdings
    {
        public bool? TaxWithholdings { get; set; }
    }
}
