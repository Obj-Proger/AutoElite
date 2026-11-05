DROP INDEX IF EXISTS ix_theory_lessons_start_utc;
ALTER TABLE theory_lessons DROP CONSTRAINT IF EXISTS ex_theory_lessons_instructor_time;
ALTER TABLE theory_lessons DROP CONSTRAINT IF EXISTS ex_theory_lessons_group_time;
