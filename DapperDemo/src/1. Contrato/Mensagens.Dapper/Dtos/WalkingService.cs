using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Mensagens.Dapper.Dtos;

[Table("WalkingService")]
public class WalkingService
{
    public int WalkingServiceId { get; init; }

    public int DogId { get; init; }

    public int PetSitterId { get; init; }

    public DateTime Date { get; init; }

    public decimal Price { get; init; }

    public bool ServicePaid { get; init; }
}

// CREATE TABLE IF NOT EXISTS WalkingService (
//     WalkingServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
//     DogId INTEGER NOT NULL,
//     PetSitterId INTEGER NOT NULL,
//     Date DATETIME NOT NULL,
//     Price DECIMAL(10, 2) NOT NULL,
//     ServicePaid BOOLEAN,
// FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
// FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));