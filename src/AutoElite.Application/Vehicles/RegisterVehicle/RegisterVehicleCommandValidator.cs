using FluentValidation;

namespace AutoElite.Application.Vehicles.RegisterVehicle;

/// <summary>Checks the plate, the category and the transmission of a new vehicle.</summary>
internal sealed class RegisterVehicleCommandValidator : AbstractValidator<RegisterVehicleCommand>
{
    public RegisterVehicleCommandValidator()
    {
        RuleFor(command => command.Plate).ValidPlate();
        RuleFor(command => command.Category).ValidCategory();
        RuleFor(command => command.Transmission).ValidTransmission();
    }
}
