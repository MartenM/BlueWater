namespace Bluewater.Infra.Options;

public class SeedingOptions
{
    // When true, seeds the full development dataset (fake members, equipment, news, etc.)
    // even outside the Development environment. Useful for spinning up demo environments.
    public bool ForceDevelopmentSeed { get; set; }
}
