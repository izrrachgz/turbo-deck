ALTER TABLE CellConfig ADD COLUMN DisplayPermanently INTEGER NOT NULL DEFAULT 0
    CONSTRAINT CK_CellConfig_DisplayPermanently CHECK (DisplayPermanently IN (0, 1));
ALTER TABLE CellConfig ADD COLUMN DisplaySize TEXT NOT NULL DEFAULT 'medium'
    CONSTRAINT CK_CellConfig_DisplaySize CHECK (DisplaySize IN ('small', 'medium', 'large', 'fullscreen'));
ALTER TABLE CellConfig ADD COLUMN AnimationInType TEXT NOT NULL DEFAULT 'none'
    CONSTRAINT CK_CellConfig_AnimationInType CHECK (AnimationInType IN ('none', 'fade', 'slide', 'zoom'));
ALTER TABLE CellConfig ADD COLUMN AnimationInDurationMilliseconds INTEGER NOT NULL DEFAULT 0
    CONSTRAINT CK_CellConfig_AnimationInDuration CHECK (AnimationInDurationMilliseconds BETWEEN 0 AND 10000);
ALTER TABLE CellConfig ADD COLUMN AnimationOutType TEXT NOT NULL DEFAULT 'none'
    CONSTRAINT CK_CellConfig_AnimationOutType CHECK (AnimationOutType IN ('none', 'fade', 'slide', 'zoom'));
ALTER TABLE CellConfig ADD COLUMN AnimationOutDurationMilliseconds INTEGER NOT NULL DEFAULT 0
    CONSTRAINT CK_CellConfig_AnimationOutDuration CHECK (AnimationOutDurationMilliseconds BETWEEN 0 AND 10000);
