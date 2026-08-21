using DapperDemo.Repository.Dapper.Services;

namespace DapperDemo.Viewmodel.Services;

/// <summary>One step of the text-size ramp: every type role's size at that step.</summary>
/// <param name="Number">Which step this is, 1 to 6.</param>
/// <param name="Label">What the user reads, e.g. "Padrão".</param>
/// <param name="Display">Size of the display role.</param>
/// <param name="Title">Size of the title role.</param>
/// <param name="Section">Size of the section role.</param>
/// <param name="Body">Size of the body role — the one the control actually sets.</param>
/// <param name="Ui">Size of the ui role.</param>
/// <param name="Caption">Size of the caption role.</param>
/// <param name="Micro">Size of the micro role.</param>
public sealed record TextSizeStep(
    int Number,
    string Label,
    double Display,
    double Title,
    double Section,
    double Body,
    double Ui,
    double Caption,
    double Micro);