using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testapp1.Models
{
    [Table("user_profiles", Schema = "best")]
    public class UserProfile
    {
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("gender")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [Column("date_of_birth")]
        public DateTime DateOfBirth { get; set; }

        [Column("height_cm")]
        public short? HeightCm { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("marital_status")]
        public string MaritalStatus { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("have_children")]
        public string HaveChildren { get; set; } = "no";

        [MaxLength(100)]
        [Column("religion")]
        public string? Religion { get; set; }

        [MaxLength(100)]
        [Column("caste")]
        public string? Caste { get; set; }

        [MaxLength(100)]
        [Column("sub_caste")]
        public string? SubCaste { get; set; }

        [MaxLength(100)]
        [Column("mother_tongue")]
        public string? MotherTongue { get; set; }

        [MaxLength(100)]
        [Column("community")]
        public string? Community { get; set; }

        [MaxLength(100)]
        [Column("country")]
        public string? Country { get; set; }

        [MaxLength(100)]
        [Column("state")]
        public string? State { get; set; }

        [MaxLength(100)]
        [Column("city")]
        public string? City { get; set; }

        [MaxLength(100)]
        [Column("citizenship")]
        public string? Citizenship { get; set; }

        [MaxLength(100)]
        [Column("residence_status")]
        public string? ResidenceStatus { get; set; }

        [MaxLength(100)]
        [Column("education_level")]
        public string? EducationLevel { get; set; }

        [MaxLength(100)]
        [Column("education_field")]
        public string? EducationField { get; set; }

        [MaxLength(100)]
        [Column("occupation")]
        public string? Occupation { get; set; }

        [MaxLength(255)]
        [Column("employer")]
        public string? Employer { get; set; }

        [MaxLength(10)]
        [Column("annual_income_currency")]
        public string? AnnualIncomeCurrency { get; set; }

        [Column("annual_income_min", TypeName = "numeric(15,2)")]
        public decimal? AnnualIncomeMin { get; set; }

        [Column("annual_income_max", TypeName = "numeric(15,2)")]
        public decimal? AnnualIncomeMax { get; set; }

        [MaxLength(50)]
        [Column("diet")]
        public string? Diet { get; set; }

        [MaxLength(50)]
        [Column("smoking")]
        public string? Smoking { get; set; }

        [MaxLength(50)]
        [Column("drinking")]
        public string? Drinking { get; set; }

        [Column("about_me", TypeName = "text")]
        public string? AboutMe { get; set; }

        [Column("family_details", TypeName = "text")]
        public string? FamilyDetails { get; set; }

        [MaxLength(50)]
        [Column("profile_created_by")]
        public string ProfileCreatedBy { get; set; } = "self";

        [MaxLength(50)]
        [Column("visibility")]
        public string Visibility { get; set; } = "public";

        [Column("is_profile_complete")]
        public bool IsProfileComplete { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}

