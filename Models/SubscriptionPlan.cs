using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace testapp1.Models
{
    [Table("subscription_plans", Schema = "best")]
    public class SubscriptionPlan
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description", TypeName = "text")]
        public string? Description { get; set; }

        [Required]
        [Column("duration_days")]
        public int DurationDays { get; set; }

        [Required]
        [Column("price_amount", TypeName = "numeric(10,2)")]
        public decimal PriceAmount { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("price_currency")]
        public string PriceCurrency { get; set; } = "USD";

        [Column("features_json", TypeName = "jsonb")]
        public string? FeaturesJson { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}

