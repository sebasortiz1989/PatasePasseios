using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Repository.Dapper.Dtos;

[Table("PetSitter")]
public class PetSitter
{
    public int PetSitterId { get; init; }

    public required string Email { get; init; }

    public required string PasswordHash { get; init; }

    public string? Password { get; init; }

    public required string Name { get; init; }

    public DateTime BirthDate { get; init; }

    /// <summary>Gets the Pix key tutors pay into. Free-form: it may be a phone, an e-mail, a CPF or a random key.</summary>
    public string? Pix { get; init; }

    /// <summary>
    /// Gets a value indicating whether the billing figures are bulleted out on screen. Stored
    /// per account rather than kept in memory, so closing the app does not reveal them again.
    /// </summary>
    public bool HideMoney { get; init; }

    /// <summary>
    /// Gets the profile photo's file name, not the bytes — the image itself lives beside the
    /// database in <see cref="Services.DogImageStore"/>, exactly as a dog's photo does.
    /// </summary>
    public string? Image { get; init; }
}

// CREATE TABLE IF NOT EXISTS PetSitter (
//     PetSitterId INTEGER PRIMARY KEY AUTOINCREMENT,
//     Email VARCHAR(255) NOT NULL UNIQUE,
//     PasswordHash VARCHAR(255) NOT NULL,
//     Name VARCHAR(100) NOT NULL,
//     BirthDate DATETIME,
//     Pix VARCHAR(255),
//     HideMoney INTEGER NOT NULL DEFAULT 0,
//     Image VARCHAR(255));