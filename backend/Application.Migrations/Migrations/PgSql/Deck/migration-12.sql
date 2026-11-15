DELETE FROM "TwitchRewardBinding" AS binding
USING "CellConfig" AS cell
WHERE binding."CellConfigId" = cell."Id"
  AND cell."RowIndex" > 2;

DELETE FROM "CellConfig" WHERE "RowIndex" > 2;

ALTER TABLE "CellConfig" DROP CONSTRAINT "CK_CellConfig_RowIndex";
ALTER TABLE "CellConfig" ADD CONSTRAINT "CK_CellConfig_RowIndex" CHECK ("RowIndex" BETWEEN 0 AND 2);
