﻿using System;
using System.Collections.Generic;
using System.Text;

namespace JT1078.Flv.Enums
{
    public enum FrameType:byte
    {
        KeyFrame = 1,
        InterFrame,
        DisposableInterFrame,
        GeneratedKeyFrame,
        VideoInfoOrCommandFrame
    }
}