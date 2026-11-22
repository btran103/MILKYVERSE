namespace MilkyVerse.Api.DTOs;

public class SePayWebhookRequest
{
    public long Id { get; set; }
    public string? Gateway { get; set; }
    public string? TransactionDate { get; set; }
    public string? AccountNumber { get; set; }
    public string? Code { get; set; }
    public string? Content { get; set; }
    public string? TransferType { get; set; } // "in" = tiền vào
    public decimal TransferAmount { get; set; }
    public decimal Accumulated { get; set; }
    public string? SubAccount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Description { get; set; }
}

public class SePayQrResponse
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Bank { get; set; } = "TPBank";
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string QrUrl { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "Pending";
    public int ExpiredSeconds { get; set; } = 900;
}
