-- Finds the documents of a record, the newest first. The owner is a complex property, which an index of the EF Core
-- model cannot hold.
CREATE INDEX ix_documents_owner ON documents (owner_id, owner_kind, uploaded_at_utc DESC);
