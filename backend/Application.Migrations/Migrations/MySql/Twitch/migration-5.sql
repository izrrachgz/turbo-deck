CREATE TABLE TwitchRewardBinding (
    Id BIGINT NOT NULL AUTO_INCREMENT,
    RewardId VARCHAR(128) NOT NULL,
    RewardTitle VARCHAR(160) NOT NULL,
    CellConfigId BIGINT NOT NULL,
    Enabled BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedUtc DATETIME(6) NOT NULL,
    ModifiedUtc DATETIME(6) NOT NULL,
    DeletedUtc DATETIME(6) NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_TwitchRewardBinding_RewardId (RewardId),
    KEY IX_TwitchRewardBinding_CellConfigId (CellConfigId),
    CONSTRAINT FK_TwitchRewardBinding_CellConfig FOREIGN KEY (CellConfigId) REFERENCES CellConfig (Id) ON DELETE RESTRICT
) ENGINE=InnoDB;
