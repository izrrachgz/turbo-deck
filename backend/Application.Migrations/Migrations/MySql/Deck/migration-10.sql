ALTER TABLE CellConfig
    DROP CHECK CK_CellConfig_AnimationInType,
    DROP CHECK CK_CellConfig_AnimationOutType;

ALTER TABLE CellConfig
    ADD CONSTRAINT CK_CellConfig_AnimationInType
        CHECK (AnimationInType IN ('none', 'fade', 'slide', 'zoom', 'spin', 'flip', 'bounce', 'morph', 'orbit', 'blur')),
    ADD CONSTRAINT CK_CellConfig_AnimationOutType
        CHECK (AnimationOutType IN ('none', 'fade', 'slide', 'zoom', 'spin', 'flip', 'bounce', 'morph', 'orbit', 'blur'));
