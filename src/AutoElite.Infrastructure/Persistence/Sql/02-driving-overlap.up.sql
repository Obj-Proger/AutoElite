-- An instructor, and a vehicle, is in at most one slot at a time; withdrawn slots do not count.
ALTER TABLE driving_slots
    ADD CONSTRAINT ex_driving_slots_instructor_time
    EXCLUDE USING gist (instructor_id WITH =, tstzrange(start_utc, end_utc) WITH &&)
    WHERE (status <> 'Withdrawn');

ALTER TABLE driving_slots
    ADD CONSTRAINT ex_driving_slots_vehicle_time
    EXCLUDE USING gist (vehicle_id WITH =, tstzrange(start_utc, end_utc) WITH &&)
    WHERE (status <> 'Withdrawn');

-- A student holds at most one active booking at a time. The time is the slot's, copied onto the booking when it is
-- made, because a constraint only sees the row's own columns; a slot's time does not change once it is offered.
ALTER TABLE driving_bookings
    ADD COLUMN slot_start_utc timestamp with time zone,
    ADD COLUMN slot_end_utc timestamp with time zone;

CREATE FUNCTION driving_bookings_copy_slot_time() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    SELECT slot.start_utc, slot.end_utc
    INTO NEW.slot_start_utc, NEW.slot_end_utc
    FROM driving_slots AS slot
    WHERE slot.id = NEW.driving_slot_id;

    RETURN NEW;
END
$$;

CREATE TRIGGER trg_driving_bookings_slot_time
    BEFORE INSERT ON driving_bookings
    FOR EACH ROW EXECUTE FUNCTION driving_bookings_copy_slot_time();

ALTER TABLE driving_bookings
    ADD CONSTRAINT ex_driving_bookings_student_time
    EXCLUDE USING gist (student_id WITH =, tstzrange(slot_start_utc, slot_end_utc) WITH &&)
    WHERE (status = 'Active');

-- Reminders and the schedules read slots by their start.
CREATE INDEX ix_driving_slots_start_utc ON driving_slots (start_utc);
