WITH PageNumbers(Position) AS (
    SELECT Position
    FROM (VALUES (1), (2), (3), (4), (5), (6), (7), (8)) AS ValuesTable(Position)
)
INSERT INTO DeckPage (ProfileId, Name, Position, CreatedUtc, ModifiedUtc, DeletedUtc)
SELECT profile.Id,
       CONVERT(VARCHAR(8), page.Position),
       page.Position,
       SYSUTCDATETIME(),
       SYSUTCDATETIME(),
       NULL
FROM Profile AS profile
CROSS JOIN PageNumbers AS page
WHERE profile.DeletedUtc IS NULL
  AND NOT EXISTS (
      SELECT 1
      FROM DeckPage AS existing
      WHERE existing.ProfileId = profile.Id
        AND existing.Position = page.Position
        AND existing.DeletedUtc IS NULL
  );
