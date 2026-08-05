namespace DapperDemo.Viewmodel.Viewmodels.Utils;

/// <summary>
/// A dog as offered in the new-service picker, carrying its tutor along. A service is booked
/// against a dog, but the tutor is who it is arranged with — and two tutors can easily have dogs
/// with the same name, so the label disambiguates them.
/// </summary>
public class DogOption(int id, string name, int tutorId, string tutorName)
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    public int TutorId { get; } = tutorId;

    public string TutorName { get; } = tutorName;

    /// <summary>Gets what the picker shows: "Toby · Marina Alves".</summary>
    public string Label { get; } = string.IsNullOrWhiteSpace(tutorName) ? name : $"{name} · {tutorName}";

    /// <summary>What the searchable picker shows in a row and matches typing against.</summary>
    public override string ToString() => Label;
}