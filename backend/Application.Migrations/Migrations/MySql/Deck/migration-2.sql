CREATE TABLE Profile (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    SortOrder INT NOT NULL,
    SelectedPageNumber INT NOT NULL DEFAULT 1,
    CreatedUtc DATETIME(6) NOT NULL,
    ModifiedUtc DATETIME(6) NOT NULL,
    DeletedUtc DATETIME(6) NULL,
    ActiveSortOrder INT GENERATED ALWAYS AS (CASE WHEN DeletedUtc IS NULL THEN SortOrder ELSE NULL END) STORED,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_Profile_SortOrder (ActiveSortOrder),
    KEY IX_Profile_CreatedUtc (CreatedUtc),
    CONSTRAINT CK_Profile_SortOrder CHECK (SortOrder BETWEEN 1 AND 8),
    CONSTRAINT CK_Profile_SelectedPageNumber CHECK (SelectedPageNumber BETWEEN 1 AND 8)
) ENGINE=InnoDB;

CREATE TABLE DeckPage (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    ProfileId BIGINT NOT NULL,
    Name VARCHAR(100) NOT NULL,
    Position INT NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL,
    ModifiedUtc DATETIME(6) NOT NULL,
    DeletedUtc DATETIME(6) NULL,
    ActivePosition INT GENERATED ALWAYS AS (CASE WHEN DeletedUtc IS NULL THEN Position ELSE NULL END) STORED,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_DeckPage_Profile_Position (ProfileId, ActivePosition),
    KEY IX_DeckPage_ProfileId (ProfileId),
    CONSTRAINT FK_DeckPage_Profile FOREIGN KEY (ProfileId) REFERENCES Profile (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_DeckPage_Position CHECK (Position BETWEEN 1 AND 8)
) ENGINE=InnoDB;

CREATE TABLE CellConfig (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    PageId BIGINT NOT NULL,
    RowIndex INT NOT NULL,
    ColumnIndex INT NOT NULL,
    Label VARCHAR(80) NULL,
    Enabled BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedUtc DATETIME(6) NOT NULL,
    ModifiedUtc DATETIME(6) NOT NULL,
    DeletedUtc DATETIME(6) NULL,
    ActivePosition VARCHAR(32) GENERATED ALWAYS AS (
        CASE WHEN DeletedUtc IS NULL THEN CONCAT(RowIndex, ':', ColumnIndex) ELSE NULL END
    ) STORED,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_CellConfig_Page_Position (PageId, ActivePosition),
    KEY IX_CellConfig_PageId (PageId),
    CONSTRAINT FK_CellConfig_DeckPage FOREIGN KEY (PageId) REFERENCES DeckPage (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_CellConfig_RowIndex CHECK (RowIndex BETWEEN 0 AND 3),
    CONSTRAINT CK_CellConfig_ColumnIndex CHECK (ColumnIndex BETWEEN 0 AND 7)
) ENGINE=InnoDB;

CREATE TABLE AppSettings (
    Id BIGINT NOT NULL,
    ActiveProfileId BIGINT NULL,
    Culture VARCHAR(5) NOT NULL,
    Theme VARCHAR(8) NOT NULL,
    Palette VARCHAR(32) NOT NULL,
    TimeZoneMode VARCHAR(32) NOT NULL,
    UtcOffsetMinutes INT NULL,
    QueueCapacity INT NOT NULL,
    VisualizerSafeMargin INT NOT NULL,
    CreatedUtc DATETIME(6) NOT NULL,
    ModifiedUtc DATETIME(6) NOT NULL,
    DeletedUtc DATETIME(6) NULL,
    PRIMARY KEY (Id),
    KEY IX_AppSettings_ActiveProfileId (ActiveProfileId),
    CONSTRAINT FK_AppSettings_Profile FOREIGN KEY (ActiveProfileId) REFERENCES Profile (Id) ON DELETE SET NULL,
    CONSTRAINT CK_AppSettings_Singleton CHECK (Id = 1),
    CONSTRAINT CK_AppSettings_Culture CHECK (Culture IN ('en-US', 'es-ES')),
    CONSTRAINT CK_AppSettings_Theme CHECK (Theme IN ('dark', 'light')),
    CONSTRAINT CK_AppSettings_TimeZoneMode CHECK (TimeZoneMode IN ('system', 'fixedUtcOffset')),
    CONSTRAINT CK_AppSettings_UtcOffset CHECK (UtcOffsetMinutes IS NULL OR UtcOffsetMinutes BETWEEN -840 AND 840),
    CONSTRAINT CK_AppSettings_QueueCapacity CHECK (QueueCapacity BETWEEN 1 AND 1000),
    CONSTRAINT CK_AppSettings_VisualizerSafeMargin CHECK (VisualizerSafeMargin BETWEEN 0 AND 1000)
) ENGINE=InnoDB;

INSERT INTO AppSettings
    (Id, ActiveProfileId, Culture, Theme, Palette, TimeZoneMode, UtcOffsetMinutes, QueueCapacity,
     VisualizerSafeMargin, CreatedUtc, ModifiedUtc, DeletedUtc)
VALUES
    (1, NULL, 'en-US', 'dark', 'midnight', 'system', NULL, 100, 24, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL);
