namespace AutoElite.Domain.Enums;

/// <summary>The gearbox of a training vehicle; a licence obtained on an automatic is restricted to automatics.</summary>
public enum Transmission
{
    /// <summary>Manual gearbox.</summary>
    Manual = 1,

    /// <summary>Automatic gearbox (including robotized and CVT).</summary>
    Automatic = 2
}
