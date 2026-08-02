using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Mensagens.Dapper.Dtos;

[Table("Tutors")]
public class Tutors
{
    public int TutorId { get; init; }

    public required string Name { get; init; }

    public required string Telephone { get; init; }

    public string? Address { get; init; }
}

// CREATE TABLE IF NOT EXISTS Tutors (
//     TutorId INTEGER PRIMARY KEY AUTOINCREMENT,
//     Name VARCHAR(255) NOT NULL,
//     Telephone VARCHAR(100) NOT NULL,
//     Address VARCHAR(100) NOT NULL);