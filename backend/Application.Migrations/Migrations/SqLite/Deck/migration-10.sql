CREATE TABLE CellConfig_v10 (
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
    DisplayDurationMilliseconds INTEGER NOT NULL DEFAULT 5000,
    ThumbnailMediaAssetId INTEGER NULL,
    DisplayPermanently INTEGER NOT NULL DEFAULT 0,
    DisplaySize TEXT NOT NULL DEFAULT 'medium',
    AnimationInType TEXT NOT NULL DEFAULT 'none',
    AnimationInDurationMilliseconds INTEGER NOT NULL DEFAULT 0,
    AnimationOutType TEXT NOT NULL DEFAULT 'none',
    AnimationOutDurationMilliseconds INTEGER NOT NULL DEFAULT 0,
    CONSTRAINT FK_CellConfig_DeckPage FOREIGN KEY (PageId) REFERENCES DeckPage (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_CellConfig_MediaAsset FOREIGN KEY (MediaAssetId) REFERENCES MediaAsset (Id) ON DELETE SET NULL,
    CONSTRAINT FK_CellConfig_ThumbnailMediaAsset FOREIGN KEY (ThumbnailMediaAssetId) REFERENCES MediaAsset (Id) ON DELETE SET NULL,
    CONSTRAINT CK_CellConfig_RowIndex CHECK (RowIndex BETWEEN 0 AND 3),
    CONSTRAINT CK_CellConfig_ColumnIndex CHECK (ColumnIndex BETWEEN 0 AND 7),
    CONSTRAINT CK_CellConfig_Enabled CHECK (Enabled IN (0, 1)),
    CONSTRAINT CK_CellConfig_Volume CHECK (Volume BETWEEN 0 AND 1),
    CONSTRAINT CK_CellConfig_PlaybackRate CHECK (PlaybackRate BETWEEN 0.25 AND 4),
    CONSTRAINT CK_CellConfig_PitchSemitones CHECK (PitchSemitones BETWEEN -24 AND 24),
    CONSTRAINT CK_CellConfig_QueuePriority CHECK (QueuePriority IN ('normal', 'override')),
    CONSTRAINT CK_CellConfig_DisplayDuration CHECK (DisplayDurationMilliseconds BETWEEN 250 AND 600000),
    CONSTRAINT CK_CellConfig_DisplayPermanently CHECK (DisplayPermanently IN (0, 1)),
    CONSTRAINT CK_CellConfig_DisplaySize CHECK (DisplaySize IN ('small', 'medium', 'large', 'fullscreen')),
    CONSTRAINT CK_CellConfig_AnimationInType CHECK (AnimationInType IN ('none', 'fade', 'slide', 'zoom', 'spin', 'flip', 'bounce', 'morph', 'orbit', 'blur')),
    CONSTRAINT CK_CellConfig_AnimationInDuration CHECK (AnimationInDurationMilliseconds BETWEEN 0 AND 10000),
    CONSTRAINT CK_CellConfig_AnimationOutType CHECK (AnimationOutType IN ('none', 'fade', 'slide', 'zoom', 'spin', 'flip', 'bounce', 'morph', 'orbit', 'blur')),
    CONSTRAINT CK_CellConfig_AnimationOutDuration CHECK (AnimationOutDurationMilliseconds BETWEEN 0 AND 10000)
);

INSERT INTO CellConfig_v10
    (Id, PageId, RowIndex, ColumnIndex, Label, Enabled, MediaAssetId, Volume, PlaybackRate, PitchSemitones,
     QueuePriority, CreatedUtc, ModifiedUtc, DeletedUtc, DisplayDurationMilliseconds, ThumbnailMediaAssetId,
     DisplayPermanently, DisplaySize, AnimationInType, AnimationInDurationMilliseconds, AnimationOutType,
     AnimationOutDurationMilliseconds)
SELECT
    Id, PageId, RowIndex, ColumnIndex, Label, Enabled, MediaAssetId, Volume, PlaybackRate, PitchSemitones,
    QueuePriority, CreatedUtc, ModifiedUtc, DeletedUtc, DisplayDurationMilliseconds, ThumbnailMediaAssetId,
    DisplayPermanently, DisplaySize, AnimationInType, AnimationInDurationMilliseconds, AnimationOutType,
    AnimationOutDurationMilliseconds
FROM CellConfig;

CREATE TABLE TwitchRewardBinding_v10 (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    RewardId TEXT NOT NULL,
    RewardTitle TEXT NOT NULL,
    CellConfigId INTEGER NOT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT FK_TwitchRewardBinding_CellConfig FOREIGN KEY (CellConfigId) REFERENCES CellConfig_v10 (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_TwitchRewardBinding_Enabled CHECK (Enabled IN (0, 1))
);

INSERT INTO TwitchRewardBinding_v10
    (Id, RewardId, RewardTitle, CellConfigId, Enabled, CreatedUtc, ModifiedUtc, DeletedUtc)
SELECT
    Id, RewardId, RewardTitle, CellConfigId, Enabled, CreatedUtc, ModifiedUtc, DeletedUtc
FROM TwitchRewardBinding;

DROP TABLE TwitchRewardBinding;
DROP TABLE CellConfig;
ALTER TABLE CellConfig_v10 RENAME TO CellConfig;
ALTER TABLE TwitchRewardBinding_v10 RENAME TO TwitchRewardBinding;

CREATE UNIQUE INDEX UX_CellConfig_Page_Position ON CellConfig (PageId, RowIndex, ColumnIndex) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_PageId ON CellConfig (PageId) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_MediaAssetId ON CellConfig (MediaAssetId) WHERE DeletedUtc IS NULL;
CREATE INDEX IX_CellConfig_ThumbnailMediaAssetId ON CellConfig (ThumbnailMediaAssetId) WHERE DeletedUtc IS NULL;
CREATE UNIQUE INDEX UX_TwitchRewardBinding_RewardId ON TwitchRewardBinding (RewardId);
CREATE INDEX IX_TwitchRewardBinding_CellConfigId ON TwitchRewardBinding (CellConfigId) WHERE DeletedUtc IS NULL;
