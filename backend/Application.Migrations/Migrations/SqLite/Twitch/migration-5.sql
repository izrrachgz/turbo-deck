PRAGMA foreign_keys = ON;

CREATE TABLE TwitchRewardBinding (
    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    RewardId TEXT NOT NULL,
    RewardTitle TEXT NOT NULL,
    CellConfigId INTEGER NOT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    CreatedUtc TEXT NOT NULL,
    ModifiedUtc TEXT NOT NULL,
    DeletedUtc TEXT NULL,
    CONSTRAINT FK_TwitchRewardBinding_CellConfig FOREIGN KEY (CellConfigId) REFERENCES CellConfig (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_TwitchRewardBinding_Enabled CHECK (Enabled IN (0, 1))
);

CREATE UNIQUE INDEX UX_TwitchRewardBinding_RewardId ON TwitchRewardBinding (RewardId);
CREATE INDEX IX_TwitchRewardBinding_CellConfigId ON TwitchRewardBinding (CellConfigId) WHERE DeletedUtc IS NULL;
