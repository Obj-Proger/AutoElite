DROP INDEX IF EXISTS ix_driving_slots_start_utc;
ALTER TABLE driving_bookings DROP CONSTRAINT IF EXISTS ex_driving_bookings_student_time;
DROP TRIGGER IF EXISTS trg_driving_bookings_slot_time ON driving_bookings;
DROP FUNCTION IF EXISTS driving_bookings_copy_slot_time();
ALTER TABLE driving_bookings DROP COLUMN IF EXISTS slot_end_utc, DROP COLUMN IF EXISTS slot_start_utc;
ALTER TABLE driving_slots DROP CONSTRAINT IF EXISTS ex_driving_slots_vehicle_time;
ALTER TABLE driving_slots DROP CONSTRAINT IF EXISTS ex_driving_slots_instructor_time;
