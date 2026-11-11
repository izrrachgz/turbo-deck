ALTER TABLE CellConfig ADD DisplayDurationMilliseconds INT NOT NULL
    CONSTRAINT DF_CellConfig_DisplayDuration DEFAULT 5000;

ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_DisplayDuration
    CHECK (DisplayDurationMilliseconds BETWEEN 250 AND 600000);
