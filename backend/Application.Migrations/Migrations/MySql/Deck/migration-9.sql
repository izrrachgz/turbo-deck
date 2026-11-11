ALTER TABLE CellConfig
    ADD COLUMN DisplayPermanently BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN DisplaySize VARCHAR(16) NOT NULL DEFAULT 'medium',
    ADD COLUMN AnimationInType VARCHAR(16) NOT NULL DEFAULT 'none',
    ADD COLUMN AnimationInDurationMilliseconds INT NOT NULL DEFAULT 0,
    ADD COLUMN AnimationOutType VARCHAR(16) NOT NULL DEFAULT 'none',
    ADD COLUMN AnimationOutDurationMilliseconds INT NOT NULL DEFAULT 0,
    ADD CONSTRAINT CK_CellConfig_DisplaySize CHECK (DisplaySize IN ('small', 'medium', 'large', 'fullscreen')),
    ADD CONSTRAINT CK_CellConfig_AnimationInType CHECK (AnimationInType IN ('none', 'fade', 'slide', 'zoom')),
    ADD CONSTRAINT CK_CellConfig_AnimationOutType CHECK (AnimationOutType IN ('none', 'fade', 'slide', 'zoom')),
    ADD CONSTRAINT CK_CellConfig_AnimationInDuration CHECK (AnimationInDurationMilliseconds BETWEEN 0 AND 10000),
    ADD CONSTRAINT CK_CellConfig_AnimationOutDuration CHECK (AnimationOutDurationMilliseconds BETWEEN 0 AND 10000);
