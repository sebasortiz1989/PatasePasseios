using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Repository.Dapper.Dtos;

[Table("Tutors")]
public class Tutors
{
    public int TutorId { get; init; }

    public required string Name { get; init; }

    public required string Telephone { get; init; }

    public string? Address { get; init; }

    /// <summary>
    /// Gets money the tutor has handed over beyond what they owed at the time.
    /// </summary>
    /// <remarks>
    /// Overpaying leaves a balance in the tutor's favour rather than being lost. It is spent
    /// automatically against the next service booked for one of their dogs.
    /// </remarks>
    public decimal Credit { get; init; }
}

// CREATE TABLE IF NOT EXISTS Tutors (
//     TutorId INTEGER PRIMARY KEY AUTOINCREMENT,
//     Name VARCHAR(255) NOT NULL,
//     Telephone VARCHAR(100) NOT NULL,
//     Address VARCHAR(100) NOT NULL);