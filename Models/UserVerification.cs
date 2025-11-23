using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("user_verifications", Schema = "best")]
    public class UserVerification
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("verification_type")]
        public string VerificationType { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("document_url")]
        public string? DocumentUrl { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("reviewed_by")]
        public long? ReviewedBy { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [Column("notes", TypeName = "text")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ReviewedBy")]
        public virtual User? Reviewer { get; set; }
    }
}

