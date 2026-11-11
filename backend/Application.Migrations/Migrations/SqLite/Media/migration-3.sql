PRAGMA foreign_keys = ON;

CREATE TABLE MediaAsset (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    StoredFileId INTEGER NOT NULL,
    DisplayName TEXT NOT NULL,
    Kind TEXT NOT NULL,
    InspectionStatus TEXT NOT NULL,
    Width INTEGER NULL,
    Height INTEGER NULL,
    DurationMilliseconds INTEGER NULL,
    VideoCodec TEXT NULL,
    AudioCodec TEXT NULL,
    HasAudio INTEGER NOT NULL DEFAULT 0,
    WaveformData TEXT NULL,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT FK_MediaAsset_StoredFile FOREIGN KEY (StoredFileId) REFERENCES StoredFile (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_MediaAsset_Kind CHECK (Kind IN ('image', 'audio', 'video', 'unknown')),
    CONSTRAINT CK_MediaAsset_HasAudio CHECK (HasAudio IN (0, 1))
);

CREATE UNIQUE INDEX UX_MediaAsset_StoredFileId ON MediaAsset (StoredFileId) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_MediaAsset_CreatedUtc ON MediaAsset (CreatedUtc DESC) WHERE DeletedUtc IS NULL;

CREATE TABLE CellConfig_v3 (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    PageId INTEGER NOT NULL,
    RowIndex INTEGER NOT NULL,
    ColumnIndex INTEGER NOT NULL,
    Label TEXT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    MediaAssetId INTEGER NULL,
    Volume NUMERIC NOT NULL DEFAULT 1,
    PlaybackRate NUMERIC NOT NULL DEFAULT 1,
    PitchSemitones NUMERIC NOT NULL DEFAULT 0,
    QueuePriority TEXT NOT NULL DEFAULT 'normal',
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT FK_CellConfig_DeckPage FOREIGN KEY (PageId) REFERENCES DeckPage (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_CellConfig_MediaAsset FOREIGN KEY (MediaAssetId) REFERENCES MediaAsset (Id) ON DELETE SET NULL,
    CONSTRAINT CK_CellConfig_RowIndex CHECK (RowIndex BETWEEN 0 AND 3),
    CONSTRAINT CK_CellConfig_ColumnIndex CHECK (ColumnIndex BETWEEN 0 AND 7),
    CONSTRAINT CK_CellConfig_Enabled CHECK (Enabled IN (0, 1)),
    CONSTRAINT CK_CellConfig_Volume CHECK (Volume BETWEEN 0 AND 1),
    CONSTRAINT CK_CellConfig_PlaybackRate CHECK (PlaybackRate BETWEEN 0.25 AND 4),
    CONSTRAINT CK_CellConfig_PitchSemitones CHECK (PitchSemitones BETWEEN -24 AND 24),
    CONSTRAINT CK_CellConfig_QueuePriority CHECK (QueuePriority IN ('normal', 'override'))
);

INSERT INTO CellConfig_v3
    (Id, PageId, RowIndex, ColumnIndex, Label, Enabled, MediaAssetId, Volume, PlaybackRate, PitchSemitones,
     QueuePriority, CreatedUtc, ModifiedUtc, DeletedUtc)
SELECT
    Id, PageId, RowIndex, ColumnIndex, Label, Enabled, NULL, 1, 1, 0, 'normal', CreatedUtc, ModifiedUtc, DeletedUtc
FROM CellConfig;

DROP TABLE CellConfig;
ALTER TABLE CellConfig_v3 RENAME TO CellConfig;

CREATE UNIQUE INDEX UX_CellConfig_Page_Position ON CellConfig (PageId, RowIndex, ColumnIndex) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_PageId ON CellConfig (PageId) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_MediaAssetId ON CellConfig (MediaAssetId) WHERE DeletedUtc IS NULL;
