CREATE TABLE TwitchRewardBinding (
    Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TwitchRewardBinding PRIMARY KEY,
    RewardId VARCHAR(128) NOT NULL,
    RewardTitle NVARCHAR(160) NOT NULL,
    CellConfigId BIGINT NOT NULL,
    Enabled BIT NOT NULL CONSTRAINT DF_TwitchRewardBinding_Enabled DEFAULT 1,
    CreatedUtc DATETIME2(6) NOT NULL,
    ModifiedUtc DATETIME2(6) NOT NULL,
    DeletedUtc DATETIME2(6) NULL,
    CONSTRAINT FK_TwitchRewardBinding_CellConfig FOREIGN KEY (CellConfigId) REFERENCES CellConfig (Id)
);

CREATE UNIQUE INDEX UX_TwitchRewardBinding_RewardId ON TwitchRewardBinding (RewardId);
CREATE INDEX IX_TwitchRewardBinding_CellConfigId ON TwitchRewardBinding (CellConfigId) WHERE DeletedUtc IS NULL;
