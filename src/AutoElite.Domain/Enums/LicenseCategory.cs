namespace AutoElite.Domain.Enums;

/// <summary>
/// Categories and subcategories of a Russian driving licence. Each course trains for one category;
/// questions, vehicles and exams are tied to categories.
/// </summary>
public enum LicenseCategory
{
    /// <summary>Motorcycles.</summary>
    A = 1,

    /// <summary>Light motorcycles (engine up to 125 cm³, power up to 11 kW).</summary>
    A1 = 2,

    /// <summary>Cars up to 3.5 t and up to 8 passenger seats.</summary>
    B = 3,

    /// <summary>Tricycles and quadricycles.</summary>
    B1 = 4,

    /// <summary>Category B vehicles with a trailer over 750 kg.</summary>
    BE = 5,

    /// <summary>Trucks over 3.5 t.</summary>
    C = 6,

    /// <summary>Medium trucks from 3.5 t to 7.5 t.</summary>
    C1 = 7,

    /// <summary>Category C vehicles with a trailer over 750 kg.</summary>
    CE = 8,

    /// <summary>Category C1 vehicles with a trailer over 750 kg.</summary>
    C1E = 9,

    /// <summary>Buses with more than 8 passenger seats.</summary>
    D = 10,

    /// <summary>Minibuses with 9 to 16 passenger seats.</summary>
    D1 = 11,

    /// <summary>Category D vehicles with a trailer over 750 kg.</summary>
    DE = 12,

    /// <summary>Category D1 vehicles with a trailer over 750 kg.</summary>
    D1E = 13,

    /// <summary>Mopeds and light quadricycles.</summary>
    M = 14,

    /// <summary>Trams.</summary>
    Tm = 15,

    /// <summary>Trolleybuses.</summary>
    Tb = 16
}
