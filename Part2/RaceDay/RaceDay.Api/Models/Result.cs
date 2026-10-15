using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Result
{
    [Key]
    public int ResultID { get; set; }

    // Foreign key -> Enrolments.EnrolmentID (one result per enrolment)
    public int EnrolmentID { get; set; }

    // Stored as SQL Server TIME (hours:minutes:seconds)
    public TimeSpan FinishTime { get; set; }

    public int FinishingPosition { get; set; }

    public DateTime ResultDate { get; set; } = DateTime.Now;
}