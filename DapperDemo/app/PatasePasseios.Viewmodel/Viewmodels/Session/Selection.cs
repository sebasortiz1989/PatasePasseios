using PatasePasseios.Repository.Dapper.Dtos;

namespace PatasePasseios.Viewmodel.Viewmodels.Session;

/// <summary>
/// Which record each detail screen is showing, as one value.
/// </summary>
/// <remarks>
/// The detail presenters are reused singletons: a dog screen is not "the screen for Jony", it is
/// the screen for whatever <see cref="AppSession.SelectedDogId"/> currently holds. That is why
/// <see cref="CurrentView"/> keeps one of these per entry on its back stack — without it, walking
/// back into a dog screen would re-render it with whatever dog was selected last, and the history
/// would be a loop rather than a path.
/// </remarks>
/// <param name="DogId">The dog a dog screen would show, or null.</param>
/// <param name="TutorId">The tutor a tutor screen would show, or null.</param>
/// <param name="Kind">Which table a service screen's booking lives in, or null.</param>
/// <param name="ServiceId">That booking's id within its table, or null.</param>
public readonly record struct Selection(int? DogId, int? TutorId, ServiceKind? Kind, int? ServiceId);
