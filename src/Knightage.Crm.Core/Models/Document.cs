namespace Knightage.Crm.Core.Models;

public class Document
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeadId { get; set; }
    public string Type { get; set; } = "Quotation";
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}
