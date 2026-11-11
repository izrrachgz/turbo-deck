PRAGMA foreign_keys = ON;

ALTER TABLE CellConfig ADD COLUMN ThumbnailMediaAssetId INTEGER NULL
    REFERENCES MediaAsset (Id) ON DELETE SET NULL;

CREATE INDEX IX_CellConfig_ThumbnailMediaAssetId ON CellConfig (ThumbnailMediaAssetId) WHERE DeletedUtc IS NULL;
