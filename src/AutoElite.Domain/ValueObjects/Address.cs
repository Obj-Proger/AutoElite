namespace AutoElite.Domain.ValueObjects;

/// <summary>A postal address split into the city and the rest of the address (street, building, office).</summary>
/// <remarks>The city is kept separately so that lists can be filtered and grouped by it.</remarks>
public sealed class Address : ValueObject
{
    private const int MaxCityLength = 100;
    private const int MaxStreetLength = 300;

    /// <summary>Gets the city or settlement.</summary>
    public string City { get; }

    /// <summary>Gets the street, building and any further details within the city.</summary>
    public string Street { get; }

    private Address(string city, string street)
    {
        City = city;
        Street = street;
    }

    /// <summary>Validates and creates an <see cref="Address"/> from raw input.</summary>
    /// <param name="city">The city. Required.</param>
    /// <param name="street">The street, building and further details. Required.</param>
    /// <returns>The created address, or the first validation failure encountered.</returns>
    public static Result<Address> Create(string? city, string? street)
    {
        var cityResult = Guard.RequiredText(city, MaxCityLength, DomainErrors.Address.CityEmpty, DomainErrors.Address.CityTooLong);
        if (cityResult.IsFailure)
            return Result.Failure<Address>(cityResult.Error);

        var streetResult = Guard.RequiredText(street, MaxStreetLength, DomainErrors.Address.StreetEmpty, DomainErrors.Address.StreetTooLong);
        if (streetResult.IsFailure)
            return Result.Failure<Address>(streetResult.Error);

        return new Address(cityResult.Value, streetResult.Value);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
    }

    /// <inheritdoc />
    public override string ToString() => $"{City}, {Street}";
}
