namespace AutoElite.Domain.Enums;

/// <summary>A driving skill the instructor grades after a practical lesson.</summary>
public enum DrivingSkill
{
    /// <summary>Starting off, stopping and smooth control of the vehicle.</summary>
    VehicleControl = 1,

    /// <summary>Changing gears at the right moment.</summary>
    GearShifting = 2,

    /// <summary>Exercises on the closed ground: parallel parking, garage entry, hill start, turning.</summary>
    Manoeuvring = 3,

    /// <summary>Following traffic rules, signs and road markings.</summary>
    TrafficRules = 4,

    /// <summary>Watching mirrors, blind spots and other road users.</summary>
    Observation = 5,

    /// <summary>Driving in real city traffic.</summary>
    CityDriving = 6
}
