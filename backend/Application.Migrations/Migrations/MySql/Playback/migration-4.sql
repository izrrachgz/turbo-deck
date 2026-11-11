ALTER TABLE CellConfig
    ADD COLUMN DisplayDurationMilliseconds INT NOT NULL DEFAULT 5000,
    ADD CONSTRAINT CK_CellConfig_DisplayDuration CHECK (DisplayDurationMilliseconds BETWEEN 250 AND 600000);
