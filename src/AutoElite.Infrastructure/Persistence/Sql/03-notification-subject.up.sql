-- Finds the notifications about a record, which repeated reminders and "read about" marking look for. The
-- subject is a complex property, which an index of the EF Core model cannot hold.
CREATE INDEX ix_notifications_subject ON notifications (subject_id, type, created_at_utc) WHERE subject_id IS NOT NULL;
