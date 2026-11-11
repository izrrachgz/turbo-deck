CREATE TABLE Profile (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Profile PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL,
    SelectedPageNumber INT NOT NULL CONSTRAINT DF_Profile_SelectedPageNumber DEFAULT 1,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
    CONSTRAINT CK_Profile_SortOrder CHECK (SortOrder BETWEEN 1 AND 8),
    CONSTRAINT CK_Profile_SelectedPageNumber CHECK (SelectedPageNumber BETWEEN 1 AND 8)
);

CREATE UNIQUE INDEX UX_Profile_SortOrder ON Profile (SortOrder) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_Profile_CreatedUtc ON Profile (CreatedUtc DESC) WHERE DeletedUtc IS NULL;

CREATE TABLE DeckPage (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeckPage PRIMARY KEY,
    ProfileId BIGINT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Position INT NOT NULL,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
    CONSTRAINT FK_DeckPage_Profile FOREIGN KEY (ProfileId) REFERENCES Profile (Id),
    CONSTRAINT CK_DeckPage_Position CHECK (Position BETWEEN 1 AND 8)
);

CREATE UNIQUE INDEX UX_DeckPage_Profile_Position ON DeckPage (ProfileId, Position) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_DeckPage_ProfileId ON DeckPage (ProfileId) WHERE DeletedUtc IS NULL;

CREATE TABLE CellConfig (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CellConfig PRIMARY KEY,
    PageId BIGINT NOT NULL,
    RowIndex INT NOT NULL,
    ColumnIndex INT NOT NULL,
    Label NVARCHAR(80) NULL,
    Enabled BIT NOT NULL CONSTRAINT DF_CellConfig_Enabled DEFAULT 1,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
    CONSTRAINT FK_CellConfig_DeckPage FOREIGN KEY (PageId) REFERENCES DeckPage (Id),
    CONSTRAINT CK_CellConfig_RowIndex CHECK (RowIndex BETWEEN 0 AND 3),
    CONSTRAINT CK_CellConfig_ColumnIndex CHECK (ColumnIndex BETWEEN 0 AND 7)
);

CREATE UNIQUE INDEX UX_CellConfig_Page_Position ON CellConfig (PageId, RowIndex, ColumnIndex) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_PageId ON CellConfig (PageId) WHERE DeletedUtc IS NULL;

CREATE TABLE AppSettings (
    Id BIGINT NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY,
    ActiveProfileId BIGINT NULL,
    Culture VARCHAR(5) NOT NULL,
    Theme VARCHAR(8) NOT NULL,
    Palette VARCHAR(32) NOT NULL,
    TimeZoneMode VARCHAR(32) NOT NULL,
    UtcOffsetMinutes INT NULL,
    QueueCapacity INT NOT NULL,
    VisualizerSafeMargin INT NOT NULL,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
    CONSTRAINT FK_AppSettings_Profile FOREIGN KEY (ActiveProfileId) REFERENCES Profile (Id) ON DELETE SET NULL,
    CONSTRAINT CK_AppSettings_Singleton CHECK (Id = 1),
    CONSTRAINT CK_AppSettings_Culture CHECK (Culture IN ('en-US', 'es-ES')),
    CONSTRAINT CK_AppSettings_Theme CHECK (Theme IN ('dark', 'light')),
    CONSTRAINT CK_AppSettings_TimeZoneMode CHECK (TimeZoneMode IN ('system', 'fixedUtcOffset')),
    CONSTRAINT CK_AppSettings_UtcOffset CHECK (UtcOffsetMinutes IS NULL OR UtcOffsetMinutes BETWEEN -840 AND 840),
    CONSTRAINT CK_AppSettings_QueueCapacity CHECK (QueueCapacity BETWEEN 1 AND 1000),
    CONSTRAINT CK_AppSettings_VisualizerSafeMargin CHECK (VisualizerSafeMargin BETWEEN 0 AND 1000)
);

INSERT INTO AppSettings
    (Id, ActiveProfileId, Culture, Theme, Palette, TimeZoneMode, UtcOffsetMinutes, QueueCapacity,
     VisualizerSafeMargin, CreatedUtc, ModifiedUtc, DeletedUtc)
VALUES
    (1, NULL, 'en-US', 'dark', 'midnight', 'system', NULL, 100, 24, SYSUTCDATETIME(), SYSUTCDATETIME(), NULL);
