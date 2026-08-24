using System.ComponentModel.DataAnnotations.Schema;

namespace PatasePasseios.Repository.Dapper.Dtos;

[Table("PetSittingService")]
public class PetSittingService
{
    public int PetSittingServiceId { get; init; }

    public int DogId { get; init; }

    public int PetSitterId { get; init; }

    public DateTime Date { get; init; }

    public decimal Price { get; init; }

    /// <summary>Gets the percentage taken off this booking's total, 0 to 100. Zero means full price.</summary>
    public decimal Discount { get; init; }

    public bool ServicePaid { get; init; }

    /// <summary>Gets a value indicating whether the visit actually happened. Independent of <see cref="ServicePaid"/>.</summary>
    public bool ServiceDone { get; init; }
}

// CREATE TABLE IF NOT EXISTS PetSittingService (
//     PetSittingServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
//     DogId INTEGER NOT NULL,
//     PetSitterId INTEGER NOT NULL,
//     Date DATETIME NOT NULL,
//     Price DECIMAL(10, 2) NOT NULL,
//     Discount DECIMAL(5, 2) NOT NULL DEFAULT 0,
//     ServicePaid BOOLEAN,
//     ServiceDone BOOLEAN NOT NULL DEFAULT 0,
// FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
// FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));