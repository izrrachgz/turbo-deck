WITH PageNumbers(Position) AS (
    VALUES (1), (2), (3), (4), (5), (6), (7), (8)
)
INSERT INTO DeckPage (ProfileId, Name, Position, CreatedUtc, ModifiedUtc, DeletedUtc)
SELECT profile.Id,
       CAST(page.Position AS TEXT),
       page.Position,
       strftime('%Y-%m-%d %H:%M:%f000Z', 'now'),
       strftime('%Y-%m-%d %H:%M:%f000Z', 'now'),
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
