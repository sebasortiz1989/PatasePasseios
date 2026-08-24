namespace PatasePasseios.Viewmodel.Viewmodels.Utils;

public class TutorOption(int id, string label)
{
    public int Id { get; } = id;

    public string Label { get; } = label;

    /// <summary>What the searchable picker shows in a row and matches typing against.</summary>
    public override string ToString() => Label;
}