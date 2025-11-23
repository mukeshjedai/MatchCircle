using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("payments", Schema = "best")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        [Column("subscription_id")]
        public long? SubscriptionId { get; set; }

        [MaxLength(50)]
        [Column("gateway")]
        public string? Gateway { get; set; }

        [MaxLength(255)]
        [Column("gateway_payment_id")]
        public string? GatewayPaymentId { get; set; }

        [Required]
        [Column("amount", TypeName = "numeric(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("currency")]
        public string Currency { get; set; } = "USD";

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("raw_response", TypeName = "text")]
        public string? RawResponse { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SubscriptionId")]
        public virtual UserSubscription? Subscription { get; set; }
    }
}

