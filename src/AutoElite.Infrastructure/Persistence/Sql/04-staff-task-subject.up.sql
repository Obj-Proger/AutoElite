-- Finds the tasks about a record, which the card of the record lists. The subject is a complex property, which an
-- index of the EF Core model cannot hold.
CREATE INDEX ix_staff_tasks_subject ON staff_tasks (subject_id, subject_kind) WHERE subject_id IS NOT NULL;
