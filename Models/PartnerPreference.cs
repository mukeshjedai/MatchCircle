using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace testapp1.Models
{
    [Table("partner_preferences", Schema = "best")]
    public class PartnerPreference
    {
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        [Column("age_min")]
        public short? AgeMin { get; set; }

        [Column("age_max")]
        public short? AgeMax { get; set; }

        [Column("height_min_cm")]
        public short? HeightMinCm { get; set; }

        [Column("height_max_cm")]
        public short? HeightMaxCm { get; set; }

        [MaxLength(20)]
        [Column("preferred_gender")]
        public string PreferredGender { get; set; } = "any";

        [Column("marital_statuses", TypeName = "jsonb")]
        public string? MaritalStatusesJson { get; set; }

        [Column("have_children_allowed", TypeName = "jsonb")]
        public string? HaveChildrenAllowedJson { get; set; }

        [Column("religions", TypeName = "jsonb")]
        public string? ReligionsJson { get; set; }

        [Column("castes", TypeName = "jsonb")]
        public string? CastesJson { get; set; }

        [Column("mother_tongues", TypeName = "jsonb")]
        public string? MotherTonguesJson { get; set; }

        [Column("communities", TypeName = "jsonb")]
        public string? CommunitiesJson { get; set; }

        [Column("countries", TypeName = "jsonb")]
        public string? CountriesJson { get; set; }

        [Column("states", TypeName = "jsonb")]
        public string? StatesJson { get; set; }

        [Column("cities", TypeName = "jsonb")]
        public string? CitiesJson { get; set; }

        [Column("education_levels", TypeName = "jsonb")]
        public string? EducationLevelsJson { get; set; }

        [Column("occupations", TypeName = "jsonb")]
        public string? OccupationsJson { get; set; }

        [Column("income_min", TypeName = "numeric(15,2)")]
        public decimal? IncomeMin { get; set; }

        [MaxLength(10)]
        [Column("income_currency")]
        public string? IncomeCurrency { get; set; }

        [Column("diet_pref", TypeName = "jsonb")]
        public string? DietPrefJson { get; set; }

        [Column("smoking_pref", TypeName = "jsonb")]
        public string? SmokingPrefJson { get; set; }

        [Column("drinking_pref", TypeName = "jsonb")]
        public string? DrinkingPrefJson { get; set; }

        [Column("other_requirements", TypeName = "text")]
        public string? OtherRequirements { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}

