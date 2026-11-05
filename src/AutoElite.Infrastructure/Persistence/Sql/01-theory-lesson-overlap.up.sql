-- A group, and an instructor, is in at most one lesson at a time; cancelled lessons do not count.
-- The application checks this first; the constraints keep it true when two requests schedule at once.
ALTER TABLE theory_lessons
    ADD CONSTRAINT ex_theory_lessons_group_time
    EXCLUDE USING gist (group_id WITH =, tstzrange(start_utc, end_utc) WITH &&)
    WHERE (status <> 'Cancelled');

ALTER TABLE theory_lessons
    ADD CONSTRAINT ex_theory_lessons_instructor_time
    EXCLUDE USING gist (instructor_id WITH =, tstzrange(start_utc, end_utc) WITH &&)
    WHERE (status <> 'Cancelled');

-- Reminders read the scheduled lessons by their start.
CREATE INDEX ix_theory_lessons_start_utc ON theory_lessons (start_utc);
