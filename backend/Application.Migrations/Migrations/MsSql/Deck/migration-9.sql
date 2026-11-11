ALTER TABLE CellConfig ADD
    DisplayPermanently BIT NOT NULL CONSTRAINT DF_CellConfig_DisplayPermanently DEFAULT 0,
    DisplaySize VARCHAR(16) NOT NULL CONSTRAINT DF_CellConfig_DisplaySize DEFAULT 'medium',
    AnimationInType VARCHAR(16) NOT NULL CONSTRAINT DF_CellConfig_AnimationInType DEFAULT 'none',
    AnimationInDurationMilliseconds INT NOT NULL CONSTRAINT DF_CellConfig_AnimationInDuration DEFAULT 0,
    AnimationOutType VARCHAR(16) NOT NULL CONSTRAINT DF_CellConfig_AnimationOutType DEFAULT 'none',
    AnimationOutDurationMilliseconds INT NOT NULL CONSTRAINT DF_CellConfig_AnimationOutDuration DEFAULT 0;

ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_DisplaySize
    CHECK (DisplaySize IN ('small', 'medium', 'large', 'fullscreen'));
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_AnimationInType
    CHECK (AnimationInType IN ('none', 'fade', 'slide', 'zoom'));
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_AnimationOutType
    CHECK (AnimationOutType IN ('none', 'fade', 'slide', 'zoom'));
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_AnimationInDuration
    CHECK (AnimationInDurationMilliseconds BETWEEN 0 AND 10000);
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_AnimationOutDuration
    CHECK (AnimationOutDurationMilliseconds BETWEEN 0 AND 10000);
