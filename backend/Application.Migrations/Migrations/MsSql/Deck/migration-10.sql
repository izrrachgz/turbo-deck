ALTER TABLE CellConfig DROP CONSTRAINT CK_CellConfig_AnimationInType;
ALTER TABLE CellConfig DROP CONSTRAINT CK_CellConfig_AnimationOutType;

ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_AnimationInType
    CHECK (AnimationInType IN ('none', 'fade', 'slide', 'zoom', 'spin', 'flip', 'bounce', 'morph', 'orbit', 'blur'));
ALTER TABLE CellConfig ADD CONSTRAINT CK_CellConfig_AnimationOutType
    CHECK (AnimationOutType IN ('none', 'fade', 'slide', 'zoom', 'spin', 'flip', 'bounce', 'morph', 'orbit', 'blur'));
