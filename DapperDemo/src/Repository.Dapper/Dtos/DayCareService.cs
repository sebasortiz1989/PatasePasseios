using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Repository.Dapper.Dtos;

/// <summary>
/// A day at the sitter's. Shaped like a hotel stay minus the span: one date, no check-out, and no
/// meaningful time of day — the date column is stored at midnight.
/// </summary>
[Table("DayCareService")]
public class DayCareService
{
    public int DayCareServiceId { get; init; }

    public int DogId { get; init; }

    public int PetSitterId { get; init; }

    /// <summary>Gets the day itself, at midnight. Day-care is not booked for a time of day.</summary>
    public DateTime Date { get; init; }

    /// <summary>Gets the fee for the day. A flat price, unlike the hotel's daily rate.</summary>
    public decimal Price { get; init; }

    public bool RequiresWalking { get; init; }

    /// <summary>Gets the percentage taken off this booking's total, 0 to 100. Zero means full price.</summary>
    public decimal Discount { get; init; }

    public bool ServicePaid { get; init; }

    /// <summary>Gets a value indicating whether the day actually happened. Independent of <see cref="ServicePaid"/>.</summary>
    public bool ServiceDone { get; init; }
}

// CREATE TABLE IF NOT EXISTS DayCareService (
//     DayCareServiceId INTEGER PRIMARY KEY AUTOINCREMENT,
//     DogId INTEGER NOT NULL,
//     PetSitterId INTEGER NOT NULL,
//     Date DATETIME NOT NULL,
//     Price DECIMAL(10, 2) NOT NULL,
//     RequiresWalking BOOLEAN NOT NULL DEFAULT 0,
//     Discount DECIMAL(5, 2) NOT NULL DEFAULT 0,
//     ServicePaid BOOLEAN,
//     ServiceDone BOOLEAN NOT NULL DEFAULT 0,
// FOREIGN KEY (DogId) REFERENCES Dogs(DogId),
// FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId));