using System.ComponentModel.DataAnnotations.Schema;

namespace Verion.Treinamento.Mensagens.Dapper.Dtos;

[Table("PetHotelService")]
public class PetHotelService
{
    public int PetHotelServiceId { get; init; }

    public int DogId { get; init; }

    public int PetSitterId { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public decimal Price { get; init; }

    public bool ServicePaid { get; init; }
}

// CREATE TABLE IF NOT EXISTS PetHotelService (
//     PetHotelServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
//     DogId INTEGER NOT NULL,
//     PetSitterId INTEGER NOT NULL,
//     StartDate DATETIME NOT NULL,
//     EndDate DATETIME NOT NULL,
//     Price DECIMAL(10, 2) NOT NULL,
//     ServicePaid BOOLEAN,
// FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
// FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));