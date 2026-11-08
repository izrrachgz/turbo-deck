PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

CREATE TABLE MigrationRecord (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    MigrationId INTEGER NOT NULL,
    Module TEXT NOT NULL,
    Name TEXT NOT NULL,
    Checksum TEXT NOT NULL,
    AppliedUtc TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f000Z', 'now')),
    ModifiedUtc TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f000Z', 'now')),
    DeletedUtc TEXT NULL
);

CREATE UNIQUE INDEX UX_MigrationRecord_MigrationId
    ON MigrationRecord (MigrationId)
    WHERE DeletedUtc IS NULL;

CREATE TABLE StoredFile (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    OriginalFileName TEXT NOT NULL,
    StorageName TEXT NOT NULL,
    ContentType TEXT NOT NULL,
    Extension TEXT NOT NULL,
    Size INTEGER NOT NULL,
    Sha256 TEXT NOT NULL,
    Purpose TEXT NULL,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT CK_StoredFile_Size CHECK (Size > 0)
);

CREATE UNIQUE INDEX UX_StoredFile_StorageName ON StoredFile (StorageName);
CREATE INDEX IX_StoredFile_CreatedUtc ON StoredFile (CreatedUtc DESC) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_StoredFile_OriginalFileName ON StoredFile (OriginalFileName) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_StoredFile_ContentType ON StoredFile (ContentType) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_StoredFile_Sha256 ON StoredFile (Sha256) WHERE DeletedUtc IS NULL;

INSERT INTO MigrationRecord
    (MigrationId, Module, Name, Checksum, AppliedUtc, DeletedUtc)
VALUES
    (1, 'Core', 'migration-1.sql', '0000000000000000000000000000000000000000000000000000000000000000',
     strftime('%Y-%m-%d %H:%M:%f000Z', 'now'), NULL);
