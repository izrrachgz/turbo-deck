ALTER TABLE CellConfig ADD ThumbnailMediaAssetId BIGINT NULL;

ALTER TABLE CellConfig ADD CONSTRAINT FK_CellConfig_ThumbnailMediaAsset
    FOREIGN KEY (ThumbnailMediaAssetId) REFERENCES MediaAsset (Id) ON DELETE SET NULL;

CREATE INDEX IX_CellConfig_ThumbnailMediaAssetId ON CellConfig (ThumbnailMediaAssetId) WHERE DeletedUtc IS NULL;
