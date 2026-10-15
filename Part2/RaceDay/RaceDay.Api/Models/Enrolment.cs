using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    [Key]
    public int EnrolmentID { get; set; }

    // Foreign key -> Users.UserID (the Participant)
    public int ParticipantID { get; set; }

    // Foreign key -> Events.EventID
    public int EventID { get; set; }

    // Foreign key -> Categories.CategoryID
    public int CategoryID { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.Now;

    // "Active" by default; a cancelled enrolment can be marked instead of deleted
    [Required, MaxLength(20)]
    public string EnrolmentStatus { get; set; } = "Active";
}