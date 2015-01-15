﻿// 2015,2014 ,Apache2, WinterDev
using System;
using System.Collections;
using System.Collections.Generic; 
using PixelFarm.Drawing;
using LayoutFarm.RenderBoxes;

namespace LayoutFarm.RenderBoxes 
{

    public interface IParentLink
    {
        bool MayHasOverlapChild { get; }
        RenderElement ParentRenderElement { get; } 
        void AdjustLocation(ref Point p);

        RenderElement FindOverlapedChildElementAtPoint(RenderElement afterThisChild, Point point);
        RenderElement NotifyParentToInvalidate(out bool goToFinalExit

#if DEBUG
, RenderElement ve
#endif
);

#if DEBUG
        string dbugGetLinkInfo();
#endif

    }


  
}