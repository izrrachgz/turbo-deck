ALTER TABLE CellConfig
    ADD COLUMN ThumbnailMediaAssetId BIGINT NULL,
    ADD KEY IX_CellConfig_ThumbnailMediaAssetId (ThumbnailMediaAssetId),
    ADD CONSTRAINT FK_CellConfig_ThumbnailMediaAsset
        FOREIGN KEY (ThumbnailMediaAssetId) REFERENCES MediaAsset (Id) ON DELETE SET NULL;
