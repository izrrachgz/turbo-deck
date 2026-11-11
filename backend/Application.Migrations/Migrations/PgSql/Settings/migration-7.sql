ALTER TABLE "AppSettings" ADD COLUMN "TimeZone" VARCHAR(64) NOT NULL DEFAULT 'UTC';

ALTER TABLE "AppSettings" ADD CONSTRAINT "CK_AppSettings_TimeZone"
    CHECK (char_length("TimeZone") BETWEEN 1 AND 64);
