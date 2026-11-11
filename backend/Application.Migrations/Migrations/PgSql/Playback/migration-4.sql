ALTER TABLE "CellConfig" ADD COLUMN "DisplayDurationMilliseconds" INTEGER NOT NULL DEFAULT 5000;

ALTER TABLE "CellConfig" ADD CONSTRAINT "CK_CellConfig_DisplayDuration"
    CHECK ("DisplayDurationMilliseconds" BETWEEN 250 AND 600000);
