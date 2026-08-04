using System.ComponentModel.DataAnnotations.Schema;

namespace DapperDemo.Repository.Dapper.Dtos;

[Table("PetSitterTutors")]
public class PetSitterTutors
{
    public int PetSitterId { get; init; }

    public int TutorId { get; init; }
}

// CREATE TABLE IF NOT EXISTS PetSitterTutors (
//     PetSitterId INTEGER NOT NULL,
//     TutorId INTEGER NOT NULL,
//     PRIMARY KEY (PetSitterId, TutorId),
// FOREIGN KEY (PetSitterId) REFERENCES PetSitter(PetSitterId),
// FOREIGN KEY (TutorId) REFERENCES Tutors(TutorId));