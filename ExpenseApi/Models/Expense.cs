using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ExpenseApi.Models;

/// <summary>
/// Represents an expense in the system.
/// </summary>
public class Expense
{
    /// <summary>
    /// Unique identifier for the expense.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    /// <summary>
    /// User ID who created this expense (email).
    /// </summary>
    [JsonPropertyName("userId")]
    [JsonProperty("userId")]
    public string? UserId { get; set; }

    /// <summary>
    /// Brief description of the expense.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Amount of the expense.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Category of the expense (Meals, Travel, Office, etc.).
    /// </summary>
    [JsonPropertyName("category")]
    [JsonProperty("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Date of purchase.
    /// </summary>
    [JsonPropertyName("purchaseDate")]
    [JsonProperty("purchaseDate")]
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// URL to receipt image in Blob Storage.
    /// </summary>
    [JsonPropertyName("receiptUrl")]
    [JsonProperty("receiptUrl")]
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Timestamp when expense was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when expense was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this expense has been paid/reimbursed.
    /// </summary>
    [JsonPropertyName("paid")]
    [JsonProperty("paid")]
    public bool Paid { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    [JsonPropertyName("isDeleted")]
    [JsonProperty("isDeleted")]
    public bool IsDeleted { get; set; }
}
