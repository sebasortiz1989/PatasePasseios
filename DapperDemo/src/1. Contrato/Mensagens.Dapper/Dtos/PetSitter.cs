using System.ComponentModel.DataAnnotations.Schema;

namespace Verion.Treinamento.Mensagens.Dapper.Dtos;

[Table("PetSitter")]
public class PetSitter
{
    public int PetSitterId { get; init; }

    public required string Email { get; init; }

    public required string PasswordHash { get; init; }

    public string? Password { get; init; }

    public required string Name { get; init; }

    public DateTime BirthDate { get; init; }
}

// CREATE TABLE IF NOT EXISTS PetSitter (
//     PetSitterId INTEGER PRIMARY KEY AUTOINCREMENT,
//     Email VARCHAR(255) NOT NULL UNIQUE,
//     PasswordHash VARCHAR(255) NOT NULL,
//     Name VARCHAR(100) NOT NULL,
//     BirthDate DATETIME);