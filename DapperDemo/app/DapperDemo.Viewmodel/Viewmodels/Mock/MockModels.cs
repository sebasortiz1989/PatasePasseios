namespace Verion.Treinamento.DapperDemo.Viewmodel.Viewmodels.Mock;

public enum ServiceKind
{
    Walk,
    Sitting,
    Hotel
}

public sealed record MockTutor
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Phone { get; init; }

    public required string Neighborhood { get; init; }

    public required List<int> DogIds { get; init; }
}

public sealed record MockDog
{
    public int Id { get; init; }

    public int OwnerId { get; init; }

    public required string Name { get; init; }

    public required string Breed { get; init; }

    public required string Description { get; init; }
}

public sealed record MockService
{
    public int Id { get; init; }

    public ServiceKind Kind { get; init; }

    public int DogId { get; init; }

    public DateTime Date { get; init; }

    public DateTime? EndDate { get; init; }

    public decimal? Price { get; init; }

    public decimal? PricePerDay { get; init; }

    public bool RequiresWalking { get; init; }

    public bool Paid { get; set; }
}
