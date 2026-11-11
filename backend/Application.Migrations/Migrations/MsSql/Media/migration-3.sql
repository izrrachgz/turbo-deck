CREATE TABLE MediaAsset (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MediaAsset PRIMARY KEY,
    StoredFileId BIGINT NOT NULL,
    DisplayName NVARCHAR(160) NOT NULL,
    Kind VARCHAR(16) NOT NULL,
    InspectionStatus VARCHAR(24) NOT NULL,
    Width INT NULL,
    Height INT NULL,
    DurationMilliseconds BIGINT NULL,
    VideoCodec VARCHAR(64) NULL,
    AudioCodec VARCHAR(64) NULL,
    HasAudio BIT NOT NULL CONSTRAINT DF_MediaAsset_HasAudio DEFAULT 0,
    WaveformData NVARCHAR(MAX) NULL,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
    CONSTRAINT FK_MediaAsset_StoredFile FOREIGN KEY (StoredFileId) REFERENCES StoredFile (Id),
    CONSTRAINT CK_MediaAsset_Kind CHECK (Kind IN ('image', 'audio', 'video', 'unknown'))
);

CREATE UNIQUE INDEX UX_MediaAsset_StoredFileId ON MediaAsset (StoredFileId) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_MediaAsset_CreatedUtc ON MediaAsset (CreatedUtc DESC) WHERE DeletedUtc IS NULL;

ALTER TABLE CellConfig ADD
    MediaAssetId BIGINT NULL,
    Volume DECIMAL(8,4) NOT NULL CONSTRAINT DF_CellConfig_Volume DEFAULT 1,
    PlaybackRate DECIMAL(8,4) NOT NULL CONSTRAINT DF_CellConfig_PlaybackRate DEFAULT 1,
    PitchSemitones DECIMAL(8,4) NOT NULL CONSTRAINT DF_CellConfig_PitchSemitones DEFAULT 0,
    QueuePriority VARCHAR(16) NOT NULL CONSTRAINT DF_CellConfig_QueuePriority DEFAULT 'normal';

ALTER TABLE CellConfig ADD CONSTRAINT FK_CellConfig_MediaAsset
    FOREIGN KEY (MediaAssetId) REFERENCES MediaAsset (Id) ON DELETE SET NULL;
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_Volume CHECK (Volume BETWEEN 0 AND 1);
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_PlaybackRate CHECK (PlaybackRate BETWEEN 0.25 AND 4);
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_PitchSemitones CHECK (PitchSemitones BETWEEN -24 AND 24);
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_QueuePriority CHECK (QueuePriority IN ('normal', 'override'));
CREATE INDEX IX_CellConfig_MediaAssetId ON CellConfig (MediaAssetId) WHERE DeletedUtc IS NULL;
