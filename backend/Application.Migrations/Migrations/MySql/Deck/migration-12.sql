DELETE binding
FROM TwitchRewardBinding AS binding
INNER JOIN CellConfig AS cell ON cell.Id = binding.CellConfigId
WHERE cell.RowIndex > 2;

DELETE FROM CellConfig WHERE RowIndex > 2;

ALTER TABLE CellConfig
    DROP CHECK CK_CellConfig_RowIndex,
    ADD CONSTRAINT CK_CellConfig_RowIndex CHECK (RowIndex BETWEEN 0 AND 2);
