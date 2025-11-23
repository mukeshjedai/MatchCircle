using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("user_interactions", Schema = "best")]
    public class UserInteraction
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("from_user_id")]
        public long FromUserId { get; set; }

        [Required]
        [Column("to_user_id")]
        public long ToUserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("interaction_type")]
        public string InteractionType { get; set; } = string.Empty;

        [Column("message", TypeName = "text")]
        public string? Message { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "pending"; // pending, accepted, declined

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("FromUserId")]
        public virtual User FromUser { get; set; } = null!;

        [ForeignKey("ToUserId")]
        public virtual User ToUser { get; set; } = null!;
    }
}

