namespace DartHost.App.Models;

public sealed record CommandDefinition(
    string Name,
    string DisplayName,
    string Description);
