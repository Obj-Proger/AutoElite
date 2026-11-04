using FluentValidation;

namespace AutoElite.Application.Vehicles.UpdateVehicle;

/// <summary>Checks the plate, the category and the transmission of a changed vehicle.</summary>
internal sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(command => command.Plate).ValidPlate();
        RuleFor(command => command.Category).ValidCategory();
        RuleFor(command => command.Transmission).ValidTransmission();
    }
}
