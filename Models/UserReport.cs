using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("user_reports", Schema = "best")]
    public class UserReport
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("reporter_id")]
        public long ReporterId { get; set; }

        [Required]
        [Column("reported_user_id")]
        public long ReportedUserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("reason_category")]
        public string ReasonCategory { get; set; } = string.Empty;

        [Column("reason_text", TypeName = "text")]
        public string? ReasonText { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "open";

        [Column("handled_by")]
        public long? HandledBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ReporterId")]
        public virtual User Reporter { get; set; } = null!;

        [ForeignKey("ReportedUserId")]
        public virtual User ReportedUser { get; set; } = null!;

        [ForeignKey("HandledBy")]
        public virtual User? Handler { get; set; }
    }
}

