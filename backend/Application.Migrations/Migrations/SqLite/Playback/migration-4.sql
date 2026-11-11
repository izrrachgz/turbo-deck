ALTER TABLE CellConfig ADD COLUMN DisplayDurationMilliseconds INTEGER NOT NULL DEFAULT 5000
    CONSTRAINT CK_CellConfig_DisplayDuration CHECK (DisplayDurationMilliseconds BETWEEN 250 AND 600000);
