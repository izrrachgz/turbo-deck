using Cqrs.Abstractions;

namespace Application.Modules.Deck.Commands;

public sealed record CreateProfileCommand(string Name) : ICommand<ProfileModel>;
