CREATE TABLE MigrationRecord (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    MigrationId BIGINT NOT NULL,
    Module VARCHAR(128) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    Checksum CHAR(64) NOT NULL,
    AppliedUtc DATETIME(6) NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ModifiedUtc DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    DeletedUtc DATETIME(6) NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_MigrationRecord_MigrationId (MigrationId)
) ENGINE=InnoDB;

CREATE TABLE StoredFile (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    OriginalFileName VARCHAR(255) NOT NULL,
    StorageName VARCHAR(255) NOT NULL,
    ContentType VARCHAR(255) NOT NULL,
    Extension VARCHAR(16) NOT NULL,
    Size BIGINT NOT NULL,
    Sha256 CHAR(64) NOT NULL,
    Purpose VARCHAR(100) NULL,
    CreatedUtc DATETIME(6) NOT NULL,
    ModifiedUtc DATETIME(6) NOT NULL,
    DeletedUtc DATETIME(6) NULL,
    PRIMARY KEY (Id),
    CONSTRAINT CK_StoredFile_Size CHECK (Size > 0),
    UNIQUE KEY UX_StoredFile_StorageName (StorageName),
    KEY IX_StoredFile_CreatedUtc (CreatedUtc),
    KEY IX_StoredFile_OriginalFileName (OriginalFileName),
    KEY IX_StoredFile_ContentType (ContentType),
    KEY IX_StoredFile_Sha256 (Sha256)
) ENGINE=InnoDB;

INSERT INTO MigrationRecord
    (MigrationId, Module, Name, Checksum, AppliedUtc, DeletedUtc)
VALUES
    (1, 'Core', 'migration-1.sql', '0000000000000000000000000000000000000000000000000000000000000000', UTC_TIMESTAMP(6), NULL);
