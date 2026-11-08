CREATE TABLE MigrationRecord (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MigrationRecord PRIMARY KEY,
    MigrationId BIGINT NOT NULL,
    Module NVARCHAR(128) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Checksum CHAR(64) NOT NULL,
    AppliedUtc DATETIME2(6) NOT NULL,
    CreatedUtc DATETIME2(6) NOT NULL CONSTRAINT DF_MigrationRecord_CreatedUtc DEFAULT SYSUTCDATETIME(),
    ModifiedUtc DATETIME2(6) NOT NULL CONSTRAINT DF_MigrationRecord_ModifiedUtc DEFAULT SYSUTCDATETIME(),
    DeletedUtc DATETIME2(6) NULL
);

CREATE UNIQUE INDEX UX_MigrationRecord_MigrationId
    ON MigrationRecord (MigrationId)
    WHERE DeletedUtc IS NULL;

CREATE TABLE StoredFile (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoredFile PRIMARY KEY,
    OriginalFileName NVARCHAR(255) NOT NULL,
    StorageName NVARCHAR(255) NOT NULL,
    ContentType NVARCHAR(255) NOT NULL,
    Extension NVARCHAR(16) NOT NULL,
    Size BIGINT NOT NULL,
    Sha256 CHAR(64) NOT NULL,
    Purpose NVARCHAR(100) NULL,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
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
    (1, N'Core', N'migration-1.sql', '0000000000000000000000000000000000000000000000000000000000000000', SYSUTCDATETIME(), NULL);
