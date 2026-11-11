INSERT INTO DeckPage (ProfileId, Name, Position, CreatedUtc, ModifiedUtc, DeletedUtc)
SELECT profile.Id,
       CAST(page.Position AS CHAR),
       page.Position,
       UTC_TIMESTAMP(6),
       UTC_TIMESTAMP(6),
       NULL
FROM Profile AS profile
CROSS JOIN (
    SELECT 1 AS Position UNION ALL
    SELECT 2 UNION ALL
    SELECT 3 UNION ALL
    SELECT 4 UNION ALL
    SELECT 5 UNION ALL
    SELECT 6 UNION ALL
    SELECT 7 UNION ALL
    SELECT 8
) AS page
WHERE profile.DeletedUtc IS NULL
  AND NOT EXISTS (
      SELECT 1
      FROM DeckPage AS existing
      WHERE existing.ProfileId = profile.Id
        AND existing.Position = page.Position
        AND existing.DeletedUtc IS NULL
  );
