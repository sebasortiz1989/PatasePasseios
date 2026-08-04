using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Repository.Dapper.Dtos;

[Table("PetHotelService")]
public class PetHotelService
{
    public int PetHotelServiceId { get; init; }

    public int DogId { get; init; }

    public int PetSitterId { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    /// <summary>Gets daily rate, not the total for the stay.</summary>
    public decimal PricePerDay { get; init; }

    /// <summary>
    /// Gets anything charged on top of the nightly rate — a late pick-up, for instance. Added to
    /// the stay once, not per night.
    /// </summary>
    public decimal ExtraCharge { get; init; }

    public bool RequiresWalking { get; init; }

    public bool ServicePaid { get; init; }
}

// CREATE TABLE IF NOT EXISTS PetHotelService (
//     PetHotelServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
//     DogId INTEGER NOT NULL,
//     PetSitterId INTEGER NOT NULL,
//     StartDate DATETIME NOT NULL,
//     EndDate DATETIME NOT NULL,
//     PricePerDay DECIMAL(10, 2) NOT NULL,
//     RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
//     ServicePaid BOOLEAN,
// FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
// FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));