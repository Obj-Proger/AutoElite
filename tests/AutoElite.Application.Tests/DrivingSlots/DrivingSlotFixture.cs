using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.DrivingSlots;

/// <summary>An instructor with a vehicle, a ground, a route and a student with a contract for the driving slot tests.</summary>
internal sealed class DrivingSlotFixture
{
    public static readonly DateTime SlotStart = TestData.Now.AddDays(2);

    public DrivingSlotFixture()
    {
        Vehicles.Seed(Vehicle);
        Grounds.Seed(Ground);
        Routes.Seed(Route);
        Contract = SeedContract("student@example.com", Transmission.Manual);
        Student = new FakeCurrentUser { UserId = Contract.StudentId, Roles = new HashSet<UserRole> { UserRole.Student } };
    }

    public FakeDrivingSlotRepository Slots { get; } = new();

    public InMemoryRepository<Vehicle> Vehicles { get; } = new();

    public InMemoryRepository<TrainingGround> Grounds { get; } = new();

    public InMemoryRepository<DrivingRoute> Routes { get; } = new();

    public FakeContractRepository Contracts { get; } = new();

    public FakeSchoolSettingsRepository Settings { get; } = new();

    public FakeCurrentUser Instructor { get; } = FakeCurrentUser.SignedIn(UserRole.DrivingInstructor);

    public Vehicle Vehicle { get; } = CreateVehicle("A123BC66", null);

    public TrainingGround Ground { get; } =
        TrainingGround.Create("Autodrome", Address.Create("Yekaterinburg", "Okruzhnaya 1").Value, null, null).Value;

    public DrivingRoute Route { get; } = DrivingRoute.Create("Ring road", "Roundabouts", Guid.CreateVersion7(), null).Value;

    public Contract Contract { get; }

    public FakeCurrentUser Student { get; }

    public static Vehicle CreateVehicle(string plate, Guid? branchId, Transmission transmission = Transmission.Manual) =>
        Vehicle.Register(LicensePlate.Create(plate).Value, "Lada", "Vesta", 2024, LicenseCategory.B, transmission, branchId, TestData.Now)
            .Value;

    public Contract SeedContract(string email, Transmission transmission, Guid? branchId = null)
    {
        var course = TestData.NewCourse();
        var contract = Contract.Create(
                $"AE-{Contracts.Items.Count + 1}", TestData.NewUser(email), course, course.Price, transmission, branchId,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), TestData.Now)
            .Value;
        Contracts.Seed(contract);

        return contract;
    }

    public DrivingSlot SeedSlot(DateTime? start = null, Vehicle? vehicle = null, Guid? instructorId = null)
    {
        var slotStart = start ?? SlotStart;
        var slot = DrivingSlot.Offer(
                instructorId ?? Instructor.UserId!.Value, vehicle ?? Vehicle, null, null,
                TimeInterval.Create(slotStart, slotStart.AddHours(2)).Value, TestData.Now)
            .Value;
        Slots.Seed(slot);

        return slot;
    }

    public DrivingSlot SeedBookedSlot(DateTime? start = null)
    {
        var slot = SeedSlot(start);
        slot.Book(Contract, TestData.Now);

        return slot;
    }
}
