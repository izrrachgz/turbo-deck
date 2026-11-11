PRAGMA foreign_keys = ON;

CREATE TABLE Profile (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    SortOrder INTEGER NOT NULL,
    SelectedPageNumber INTEGER NOT NULL DEFAULT 1,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT CK_Profile_SortOrder CHECK (SortOrder BETWEEN 1 AND 8),
    CONSTRAINT CK_Profile_SelectedPageNumber CHECK (SelectedPageNumber BETWEEN 1 AND 8)
);

CREATE UNIQUE INDEX UX_Profile_SortOrder ON Profile (SortOrder) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_Profile_CreatedUtc ON Profile (CreatedUtc DESC) WHERE DeletedUtc IS NULL;

CREATE TABLE DeckPage (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    ProfileId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Position INTEGER NOT NULL,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT FK_DeckPage_Profile FOREIGN KEY (ProfileId) REFERENCES Profile (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_DeckPage_Position CHECK (Position BETWEEN 1 AND 8)
);

CREATE UNIQUE INDEX UX_DeckPage_Profile_Position ON DeckPage (ProfileId, Position) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_DeckPage_ProfileId ON DeckPage (ProfileId) WHERE DeletedUtc IS NULL;

CREATE TABLE CellConfig (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    PageId INTEGER NOT NULL,
    RowIndex INTEGER NOT NULL,
    ColumnIndex INTEGER NOT NULL,
    Label TEXT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT FK_CellConfig_DeckPage FOREIGN KEY (PageId) REFERENCES DeckPage (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_CellConfig_RowIndex CHECK (RowIndex BETWEEN 0 AND 3),
    CONSTRAINT CK_CellConfig_ColumnIndex CHECK (ColumnIndex BETWEEN 0 AND 7),
    CONSTRAINT CK_CellConfig_Enabled CHECK (Enabled IN (0, 1))
);

CREATE UNIQUE INDEX UX_CellConfig_Page_Position ON CellConfig (PageId, RowIndex, ColumnIndex) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_PageId ON CellConfig (PageId) WHERE DeletedUtc IS NULL;

CREATE TABLE AppSettings (
    Id INTEGER NOT NULL PRIMARY KEY,
    ActiveProfileId INTEGER NULL,
    Culture TEXT NOT NULL,
    Theme TEXT NOT NULL,
    Palette TEXT NOT NULL,
    TimeZoneMode TEXT NOT NULL,
    UtcOffsetMinutes INTEGER NULL,
    QueueCapacity INTEGER NOT NULL,
    VisualizerSafeMargin INTEGER NOT NULL,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
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
    (1, NULL, 'en-US', 'dark', 'midnight', 'system', NULL, 100, 24,
     strftime('%Y-%m-%d %H:%M:%f000Z', 'now'), strftime('%Y-%m-%d %H:%M:%f000Z', 'now'), NULL);
