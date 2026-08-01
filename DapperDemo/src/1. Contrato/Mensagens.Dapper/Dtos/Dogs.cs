using System.ComponentModel.DataAnnotations.Schema;

namespace Verion.Treinamento.Mensagens.Dapper.Dtos;

[Table("Dogs")]
public class Dogs
{
    public int DogId { get; init; }

    public int TutorId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Image { get; init; }
}

// CREATE TABLE IF NOT EXISTS Dogs (
//     DogId INTEGER PRIMARY KEY AUTOINCREMENT,
//     TutorId INTEGER NOT NULL,
//     Name VARCHAR(255) NOT NULL UNIQUE,
//     Description VARCHAR(255),
//     Image BLOB,
// FOREIGN KEY (TutorId) REFERENCES Tutors(TutorId));