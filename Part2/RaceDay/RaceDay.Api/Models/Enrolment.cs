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

    // "Pending" when a participant first enrols; the Organiser can mark it "Confirmed" or "Cancelled"
    [Required, MaxLength(20)]
    public string EnrolmentStatus { get; set; } = "Pending";
}