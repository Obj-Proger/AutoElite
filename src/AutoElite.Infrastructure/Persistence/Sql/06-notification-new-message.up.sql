-- A recipient has at most one unread "new message" notification per chat: the handler skips recipients that have one,
-- and when two messages are delivered at once, this index rejects the second notification, whose event is then
-- delivered again and finds the first.
CREATE UNIQUE INDEX ux_notifications_new_message_unread ON notifications (recipient_id, subject_kind, subject_id)
    WHERE type = 'NewMessage' AND read_at_utc IS NULL;
