﻿//BSD 2014, WinterCore


// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using HtmlRenderer.Entities;
using HtmlRenderer.Handlers;
using HtmlRenderer.Parse;
using HtmlRenderer.Utils;
using System.Text;

namespace HtmlRenderer.Dom
{


    /// <summary>
    /// Represents a CSS Box of text or replaced elements.
    /// </summary>
    /// <remarks>
    /// The Box can contains other boxes, that's the way that the CSS Tree
    /// is composed.
    /// 
    /// To know more about boxes visit CSS spec:
    /// http://www.w3.org/TR/CSS21/box.html
    /// </remarks>
    public partial class CssBox : CssBoxBase, IDisposable
    {

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="parentBox">optional: the parent of this css box in html</param>
        /// <param name="tag">optional: the html tag associated with this css box</param>
        public CssBox(CssBox parentBox, IHtmlElement tag)
        {

            this._boxes = new CssBoxCollection(this);

            if (parentBox != null)
            {
                parentBox.Boxes.Add(this);
            }

            _htmltag = tag;
            if (tag != null)
            {
                this.WellknownTagName = tag.WellknownTagName;
            }
        }

        /// <summary>
        /// Gets the HtmlContainer of the Box.
        /// WARNING: May be null.
        /// </summary>
        public HtmlContainer HtmlContainer
        {
            get { return _htmlContainer ?? (_parentBox != null ? _htmlContainer = _parentBox.HtmlContainer : null); }

        }
        public static void SetHtmlContainer(CssBox htmlRoot, HtmlContainer container)
        {
            htmlRoot._htmlContainer = container;
        }
        /// <summary>
        /// Gets the parent box of this box
        /// </summary>
        public CssBox ParentBox
        {
            get { return _parentBox; }
        }

        /// <summary>
        /// 1. remove this box from its parent and 2. add to new parent box
        /// </summary>
        /// <param name="parentBox"></param>
        internal void SetNewParentBox(CssBox parentBox)
        {
            if (this._parentBox != null)
            {
                this._parentBox.Boxes.Remove(this);
            }
            if (parentBox != null)
            {
                parentBox.Boxes.Add(this);
                _htmlContainer = parentBox.HtmlContainer;
            }
        }
        internal void SetNewParentBox(int myIndexHint, CssBox parentBox)
        {
            if (this._parentBox != null)
            {
                this._parentBox.Boxes.RemoveAt(myIndexHint);
            }
            if (parentBox != null)
            {
                parentBox.Boxes.Add(this);
                _htmlContainer = parentBox.HtmlContainer;
            }
        }
        /// <summary>
        /// Is the box is of "br" element.
        /// </summary>
        public bool IsBrElement
        {
            get
            {
                return this.WellknownTagName == WellknownHtmlTagName.BR;
            }
        }

        /// <summary>
        /// is the box "Display" is "Inline", is this is an inline box and not block.
        /// </summary>
        public bool IsInline
        {
            get
            {
                return (this.CssDisplay == CssDisplay.Inline
                    || this.CssDisplay == CssDisplay.InlineBlock)
                    && !IsBrElement;
            }
        }


        /// <summary>
        /// is the box "Display" is "Block", is this is an block box and not inline.
        /// </summary>
        public bool IsBlock
        {
            get
            {
                return this.CssDisplay == CssDisplay.Block;
            }
        }



        /// <summary>
        /// Get the href link of the box (by default get "href" attribute)
        /// </summary>
        public virtual string HrefLink
        {
            get { return GetAttribute("href"); }
        }
        internal bool HasContainingBlockProperty
        {
            get
            {
                switch (this.CssDisplay)
                {
                    case CssDisplay.Block:
                    case CssDisplay.ListItem:
                    case CssDisplay.Table:
                    case CssDisplay.TableCell:
                        return true;
                    default:
                        return false;
                }
            }
        }
        /// <summary>
        /// Gets the containing block-box of this box. (The nearest parent box with display=block)
        /// </summary>
        public CssBox ContainingBlock
        {
            get
            {
                if (ParentBox == null)
                {
                    return this; //This is the initial containing block.
                }

                var box = ParentBox;
                while (box.CssDisplay < CssDisplay.__CONTAINER_BEGIN_HERE &&
                    box.ParentBox != null)
                {
                    box = box.ParentBox;
                }

                //Comment this following line to treat always superior box as block
                if (box == null)
                    throw new Exception("There's no containing block on the chain");

                return box;
            }
        }

        /// <summary>
        /// Gets the HTMLTag that hosts this box
        /// </summary>
        public IHtmlElement HtmlTag
        {
            get { return _htmltag; }
        }

        /// <summary>
        /// Gets if this box represents an image
        /// </summary>
        public bool IsImage
        {
            get
            {
                return this.HasRuns && this.FirstRun.IsImage;

            }
        }

        internal bool ContainsSelectedRun
        {
            get;
            set;
        }
        /// <summary>
        /// Tells if the box is empty or contains just blank spaces
        /// </summary>
        public bool IsSpaceOrEmpty
        {
            get
            {
                if ((Runs.Count != 0 || Boxes.Count != 0) && (Runs.Count != 1 || !Runs[0].IsSpaces))
                {
                    foreach (CssRun word in Runs)
                    {
                        if (!word.IsSpaces)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
        public static char[] UnsafeGetTextBuffer(CssBox box)
        {
            return box._textBuffer;
        }

        void ResetTextFlags()
        {
            int tmpFlags = this._boxCompactFlags;
            tmpFlags &= ~CssBoxFlagsConst.HAS_EVAL_WHITESPACE;
            tmpFlags &= ~CssBoxFlagsConst.TEXT_IS_ALL_WHITESPACE;
            tmpFlags &= ~CssBoxFlagsConst.TEXT_IS_EMPTY;
            this._boxCompactFlags = tmpFlags;
        }

        internal void SetTextContent(char[] chars)
        {
            this._textBuffer = chars;
            ResetTextFlags();
        }
        public bool MayHasSomeTextContent
        {
            get
            {
                return this._textBuffer != null;
            }
        }
        void EvaluateWhitespace()
        {

            this._boxCompactFlags |= CssBoxFlagsConst.HAS_EVAL_WHITESPACE;
            char[] tmp;

            if ((tmp = this._textBuffer) == null)
            {

                this._boxCompactFlags |= CssBoxFlagsConst.TEXT_IS_EMPTY;
                return;
            }
            for (int i = tmp.Length - 1; i >= 0; --i)
            {
                if (!char.IsWhiteSpace(tmp[i]))
                {
                    return;
                }
            }

            //all is whitespace
            this._boxCompactFlags |= CssBoxFlagsConst.TEXT_IS_ALL_WHITESPACE;
        }
        internal bool TextContentIsAllWhitespace
        {
            get
            {
                if ((this._boxCompactFlags & CssBoxFlagsConst.HAS_EVAL_WHITESPACE) == 0)
                {
                    EvaluateWhitespace();
                }
                return (this._boxCompactFlags & CssBoxFlagsConst.TEXT_IS_ALL_WHITESPACE) != 0;
            }
        }
        internal bool TextContentIsWhitespaceOrEmptyText
        {
            get
            {
                if ((this._boxCompactFlags & CssBoxFlagsConst.HAS_EVAL_WHITESPACE) == 0)
                {
                    EvaluateWhitespace();
                }
                return ((this._boxCompactFlags & CssBoxFlagsConst.TEXT_IS_ALL_WHITESPACE) != 0) ||
                        ((this._boxCompactFlags & CssBoxFlagsConst.TEXT_IS_EMPTY) != 0);
            }
        }
        internal string CopyTextContent()
        {
            if (this._textBuffer != null)
            {
                return new string(this._textBuffer);
            }
            else
            {
                return null;
            }
        }
        internal void AddLineBox(CssLineBox linebox)
        {
            linebox.linkedNode = this._clientLineBoxes.AddLast(linebox);
        }
        internal int LineBoxCount
        {
            get
            {
                if (this._clientLineBoxes == null)
                {
                    return 0;
                }
                else
                {
                    return this._clientLineBoxes.Count;
                }
            }
        }
        internal IEnumerable<CssLineBox> GetLineBoxIter()
        {
            var node = this._clientLineBoxes.First;
            while (node != null)
            {
                yield return node.Value;
                node = node.Next;
            }
        }
        internal CssLineBox GetFirstLineBox()
        {
            return this._clientLineBoxes.First.Value;
        }
        internal CssLineBox GetLastLineBox()
        {
            return this._clientLineBoxes.Last.Value;
        }


        /// <summary>
        /// Gets the BoxWords of text in the box
        /// </summary>
        List<CssRun> Runs
        {
            get { return _boxRuns; }
        }

        internal bool HasRuns
        {
            get
            {
                return this._boxRuns != null && this._boxRuns.Count > 0;
            }
        }

        /// <summary>
        /// Gets the first word of the box
        /// </summary>
        internal CssRun FirstRun
        {
            get { return Runs[0]; }
        }

        /// <summary>
        /// Gets or sets the first linebox where content of this box appear
        /// </summary>
        internal CssLineBox FirstHostingLineBox
        {
            get { return _firstHostingLineBox; }
            set { _firstHostingLineBox = value; }
        }

        /// <summary>
        /// Gets or sets the last linebox where content of this box appear
        /// </summary>
        internal CssLineBox LastHostingLineBox
        {
            get { return _lastHostingLineBox; }
            set { _lastHostingLineBox = value; }
        }
        /// <summary>
        /// all parts are in the same line box 
        /// </summary>
        internal bool AllPartsAreInTheSameLineBox
        {
            get
            {
                return this._firstHostingLineBox == this._lastHostingLineBox;
            }
        }

        //------------------------------------------------------------------
        /// <summary>
        /// Create new css box for the given parent with the given html tag.<br/>
        /// </summary>
        /// <param name="tag">the html tag to define the box</param>
        /// <param name="parent">the box to add the new box to it as child</param>
        /// <returns>the new box</returns>
        public static CssBox CreateBox(IHtmlElement tag, CssBox parent = null)
        {


            switch (tag.WellknownTagName)
            {
                case WellknownHtmlTagName.IMG:
                    return new CssBoxImage(parent, tag);
                case WellknownHtmlTagName.IFREAME:
                    return new CssBoxHr(parent, tag);
                case WellknownHtmlTagName.HR:
                    return new CssBoxHr(parent, tag);
                //test extension box
                case WellknownHtmlTagName.X:
                    var customBox = CreateCustomBox(tag, parent);
                    if (customBox == null)
                    {
                        return new CssBox(parent, tag);
                    }
                    else
                    {
                        return customBox;
                    }
                default:
                    return new CssBox(parent, tag);
            }
        }
        static CssBox CreateCustomBox(IHtmlElement tag, CssBox parent)
        {
            for (int i = generators.Count - 1; i >= 0; --i)
            {
                var newbox = generators[i].CreateCssBox(tag, parent);
                if (newbox != null)
                {
                    return newbox;
                }
            }
            return null;
        }
        /// <summary>
        /// Create new css block box.
        /// </summary>
        /// <returns>the new block box</returns>
        internal static CssBox CreateRootBlock()
        {
            var box = new CssBox(null, null);
            box.CssDisplay = CssDisplay.Block;
            return box;
        }
        /// <summary>
        /// Create new css box for the given parent with the given optional html tag and insert it either
        /// at the end or before the given optional box.<br/>
        /// If no html tag is given the box will be anonymous.<br/>
        /// If no before box is given the new box will be added at the end of parent boxes collection.<br/>
        /// If before box doesn't exists in parent box exception is thrown.<br/>
        /// </summary>
        /// <remarks>
        /// To learn more about anonymous inline boxes visit: http://www.w3.org/TR/CSS21/visuren.html#anonymous
        /// </remarks>
        /// <param name="parent">the box to add the new box to it as child</param>
        /// <param name="tag">optional: the html tag to define the box</param>
        /// <param name="before">optional: to insert as specific location in parent box</param>
        /// <returns>the new box</returns>
        public static CssBox CreateBox(CssBox parent, IHtmlElement tag = null, int insertAt = -1)
        {

            var newBox = new CssBox(parent, tag);
            newBox.InheritStyles(parent);
            if (insertAt > -1)
            {
                newBox.ChangeSiblingOrder(insertAt);
            }
            return newBox;
        }



        /// <summary>
        /// Create new css block box for the given parent with the given optional html tag and insert it either
        /// at the end or before the given optional box.<br/>
        /// If no html tag is given the box will be anonymous.<br/>
        /// If no before box is given the new box will be added at the end of parent boxes collection.<br/>
        /// If before box doesn't exists in parent box exception is thrown.<br/>
        /// </summary>
        /// <remarks>
        /// To learn more about anonymous block boxes visit CSS spec:
        /// http://www.w3.org/TR/CSS21/visuren.html#anonymous-block-level
        /// </remarks>
        /// <param name="parent">the box to add the new block box to it as child</param>
        /// <param name="tag">optional: the html tag to define the box</param>
        /// <param name="before">optional: to insert as specific location in parent box</param>
        /// <returns>the new block box</returns>
        internal static CssBox CreateAnonBlock(CssBox parent, int insertAt = -1)
        {

            var newBox = CreateBox(parent, null, insertAt);
            newBox.CssDisplay = CssDisplay.Block;
            return newBox;
        }

        /// <summary>
        /// Measures the bounds of box and children, recursively.<br/>
        /// Performs layout of the DOM structure creating lines by set bounds restrictions.
        /// </summary>
        /// <param name="g">Device context to use</param>
        public void PerformLayout(LayoutArgs args)
        {
            PerformContentLayout(args);
        }

        internal void ChangeSiblingOrder(int siblingIndex)
        {
            if (siblingIndex < 0)
            {
                throw new Exception("before box doesn't exist on parent");
            }
            this._parentBox.Boxes.ChangeSiblingIndex(this, siblingIndex);
        }



        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (_imageLoadHandler != null)
            {
                _imageLoadHandler.Dispose();
            }

            foreach (var childBox in Boxes)
            {
                childBox.Dispose();
            }
        }


        #region Private Methods

        /// <summary>
        /// Measures the bounds of box and children, recursively.<br/>
        /// Performs layout of the DOM structure creating lines by set bounds restrictions.<br/>
        /// </summary>
        /// <param name="g">Device context to use</param>
        protected virtual void PerformContentLayout(LayoutArgs args)
        {

            if (this.CssDisplay != CssDisplay.None)
            {
                MeasureRunsSize(args.Gfx);
            }
            //-----------------------------------------------------------
            switch (this.CssDisplay)
            {
                case Dom.CssDisplay.None:
                    {
                        return;
                    }
                case Dom.CssDisplay.Block:
                case Dom.CssDisplay.ListItem:
                case Dom.CssDisplay.Table:
                case Dom.CssDisplay.InlineTable:
                case Dom.CssDisplay.TableCell:
                    {
                        // Because their width and height are set by CssTable                                 

                        CssBox myContainingBlock = args.LatestContaingBlock;

                        if (this.CssDisplay != CssDisplay.TableCell)
                        {
                            //-------------------------
                            if (this.CssDisplay != Dom.CssDisplay.Table)
                            {
                                float availableWidth = myContainingBlock.AvailableContentWidth;

                                if (!this.Width.IsAuto && !this.Width.IsEmpty)
                                {
                                    availableWidth = CssValueParser.ParseLength(Width, availableWidth, this);
                                }
                                this.SetSize(availableWidth, this.SizeHeight);
                                // must be separate because the margin can be calculated by percentage of the width
                                this.SetSize(availableWidth - ActualMarginLeft - ActualMarginRight, this.SizeHeight);
                            }

                            //-------------------------

                            float left = myContainingBlock.GlobalClientLeft + this.ActualMarginLeft;
                            float top = 0;

                            var prevSibling = args.LatestSiblingBox;

                            if (prevSibling == null)
                            {
                                if (this.ParentBox != null)
                                {
                                    top = this.ParentBox.GlobalClientTop;
                                }
                            }
                            else
                            {
                                if (this.ParentBox == null)
                                {
                                    top = this.GlobalY;
                                }
                                top += prevSibling.GlobalActualBottom + prevSibling.ActualBorderBottomWidth;
                            }
                            top += MarginTopCollapse(prevSibling);

                            this.SetGlobalLocation(left, top, myContainingBlock.GlobalX, myContainingBlock.GlobalY);
                            this.SetHeightToZero();
                        }
                        //--------------------------------------------------------------------------
                        //If we're talking about a table here..
                        switch (this.CssDisplay)
                        {
                            case Dom.CssDisplay.Table:
                            case Dom.CssDisplay.InlineTable:
                                {
                                    CssTableLayoutEngine.PerformLayout(this, args);
                                } break;
                            default:
                                {
                                    //If there's just inline boxes, create LineBoxes
                                    if (DomUtils.ContainsInlinesOnly(this))
                                    {
                                        this.SetHeightToZero();
                                        CssLayoutEngine.FlowContentRuns(this, args); //This will automatically set the bottom of this block
                                    }
                                    else if (_boxes.Count > 0)
                                    {
                                        args.PushContaingBlock(this);

                                        var currentLevelLatestSibling = args.LatestSiblingBox;
                                        args.LatestSiblingBox = null;//reset

                                        foreach (var childBox in Boxes)
                                        {


                                            childBox.PerformLayout(args);

                                            if (childBox.CanBeRefererenceSibling)
                                            {
                                                args.LatestSiblingBox = childBox;
                                            }
                                        }

                                        args.LatestSiblingBox = currentLevelLatestSibling;
                                        args.PopContainingBlock();

                                        SetGlobalActualRight(CalculateActualRight());
                                        SetGlobalActualBottom(MarginBottomCollapse());
                                    }
                                } break;
                        }

                        //--------------------------------------------------------------------------
                    } break;
                default:
                    {
                        var prevSibling = args.LatestSiblingBox;
                        if (prevSibling != null)
                        {
                            //if (!this.HasAssignedLocation)
                            //{
                            //    this.SetLocation(prevSibling.LocationX, prevSibling.LocationY);
                            //}
                            //SetActualBottom(prevSibling.ActualBottom);
                        }
                    } break;
            }

            //----------------------------------------------------------------------------- 
            //set height 

            this.UpdateIfHigher(this.ExpectedHeight);
            this.CreateListItemBox(args);

            float newWidth = Math.Max(CalculateMinimumWidth() + CalculateWidthMarginRecursiveUp(this),
                              this.SizeWidth < CssBox.MAX_RIGHT ? GlobalActualRight : 0);
            //update back
            HtmlContainer.UpdateSizeIfWiderOrHeigher(newWidth, GlobalActualBottom - HtmlContainer.Root.GlobalY);
        }

        /// <summary>
        /// Assigns words its width and height
        /// </summary>
        /// <param name="g"></param>
        internal virtual void MeasureRunsSize(IGraphics g)
        {
            //measure once !
            if (_wordsSizeMeasured) return;
            //--------------------------------

            if (BackgroundImage != CssConstants.None && _imageLoadHandler == null)
            {
                //TODO: change to another technique !
                _imageLoadHandler = new ImageLoadHandler(HtmlContainer, OnImageLoadComplete);
                _imageLoadHandler.LoadImage(BackgroundImage, HtmlTag);
            }

            MeasureWordSpacing(g);

            if (this.HasRuns)
            {
                Font actualFont = this.ActualFont;
                float fontHeight = FontsUtils.GetFontHeight(actualFont);

                foreach (CssRun boxWord in Runs)
                {
                    boxWord.Height = fontHeight;

                    //if this is newline then width =0 ***                         
                    switch (boxWord.Kind)
                    {
                        case CssRunKind.Text:
                            {

                                CssTextRun textRun = (CssTextRun)boxWord;
                                boxWord.Width = FontsUtils.MeasureStringWidth(g,
                                    CssBox.UnsafeGetTextBuffer(this),
                                    textRun.TextStartIndex,
                                    textRun.TextLength,
                                    actualFont);

                            } break;
                        case CssRunKind.SingleSpace:
                            {
                                boxWord.Width = this.ActualWordSpacing;
                            } break;
                        case CssRunKind.Space:
                            {
                                //other space size                                     
                                boxWord.Width = this.ActualWordSpacing * ((CssTextRun)boxWord).TextLength;
                            } break;
                        case CssRunKind.LineBreak:
                            {
                                boxWord.Width = 0;
                            } break;
                    }
                }
            }
            _wordsSizeMeasured = true;//***

        }

        /// <summary>
        /// Get the parent of this css properties instance.
        /// </summary>
        /// <returns></returns>
        public sealed override CssBoxBase GetParent()
        {
            return _parentBox;
        }

        /// <summary>
        /// Gets the index of the box to be used on a (ordered) list
        /// </summary>
        /// <returns></returns>
        private int GetIndexForList()
        {
            bool reversed = !string.IsNullOrEmpty(ParentBox.GetAttribute("reversed"));
            int index;
            if (!int.TryParse(ParentBox.GetAttribute("start"), out index))
            {
                if (reversed)
                {
                    index = 0;
                    foreach (CssBox b in ParentBox.Boxes)
                    {
                        if (b.CssDisplay == CssDisplay.ListItem)
                        {
                            index++;
                        }
                    }
                }
                else
                {
                    index = 1;
                }
            }

            foreach (CssBox b in ParentBox.Boxes)
            {
                if (b.Equals(this))
                    return index;

                //if (b.Display == CssConstants.ListItem)
                if (b.CssDisplay == CssDisplay.ListItem)
                    index += reversed ? -1 : 1;
            }

            return index;
        }
        static readonly char[] discItem = new[] { '•' };
        static readonly char[] circleItem = new[] { 'o' };
        static readonly char[] squareItem = new[] { '♠' };


        /// <summary>
        /// Creates the <see cref="_listItemBox"/>
        /// </summary>
        /// <param name="g"></param>
        void CreateListItemBox(LayoutArgs layoutArgs)
        {

            if (this.CssDisplay == CssDisplay.ListItem &&
                ListStyleType != CssListStyleType.None)
            {
                if (_listItemBox == null)
                {
                    _listItemBox = new CssBox(null, null);
                    _listItemBox.InheritStyles(this);
                    _listItemBox.CssDisplay = CssDisplay.Inline;
                    _listItemBox._htmlContainer = HtmlContainer;

                    switch (this.ListStyleType)
                    {
                        case CssListStyleType.Disc:
                            {
                                _listItemBox.SetTextContent(discItem);
                            } break;
                        case CssListStyleType.Circle:
                            {
                                _listItemBox.SetTextContent(circleItem);
                            } break;
                        case CssListStyleType.Square:
                            {
                                _listItemBox.SetTextContent(squareItem);
                            } break;
                        case CssListStyleType.Decimal:
                            {
                                _listItemBox.SetTextContent((GetIndexForList().ToString(CultureInfo.InvariantCulture) + ".").ToCharArray());
                            } break;
                        case CssListStyleType.DecimalLeadingZero:
                            {
                                _listItemBox.SetTextContent((GetIndexForList().ToString("00", CultureInfo.InvariantCulture) + ".").ToCharArray());
                            } break;
                        default:
                            {
                                _listItemBox.SetTextContent((CommonUtils.ConvertToAlphaNumber(GetIndexForList(), ListStyleType) + ".").ToCharArray());
                            } break;
                    }


                    _listItemBox.ParseWordContent();

                    var prevSibling = layoutArgs.LatestSiblingBox;
                    layoutArgs.LatestSiblingBox = null;//reset
                    _listItemBox.PerformContentLayout(layoutArgs);
                    layoutArgs.LatestSiblingBox = prevSibling;


                    var fRun = _listItemBox.FirstRun;

                    _listItemBox.FirstRun.SetSize(fRun.Width, fRun.Height);
                }

                _listItemBox.FirstRun.SetLocation(this.GlobalX - _listItemBox.SizeWidth - 5, this.GlobalY + ActualPaddingTop);

            }
        }

        internal void ParseWordContent()
        {
            CssTextSplitter.DefaultSplitter.ParseWordContent(this);
        }

        /// <summary>
        /// Gets the specified Attribute, returns string.Empty if no attribute specified
        /// </summary>
        /// <param name="attribute">Attribute to retrieve</param>
        /// <returns>Attribute value or string.Empty if no attribute specified</returns>
        public string GetAttribute(string attribute)
        {
            return GetAttribute(attribute, string.Empty);
        }

        /// <summary>
        /// Gets the value of the specified attribute of the source HTML tag.
        /// </summary>
        /// <param name="attribute">Attribute to retrieve</param>
        /// <param name="defaultValue">Value to return if attribute is not specified</param>
        /// <returns>Attribute value or defaultValue if no attribute specified</returns>
        public string GetAttribute(string attribute, string defaultValue)
        {
            return HtmlTag != null ? HtmlTag.TryGetAttribute(attribute, defaultValue) : defaultValue;
        }
        /// <summary>
        /// Gets the minimum width that the box can be.
        /// *** The box can be as thin as the longest word plus padding
        /// </summary>
        /// <returns></returns>
        internal float CalculateMinimumWidth()
        {
            //use line box technique *** 
            float maxWidth = 0;
            CssRun maxWidthRun = null;
            if (this.LineBoxCount > 0)
            {
                CalculateMinimumWidthAndWidestRun(this, out maxWidth, out maxWidthRun);
            }
            //-------------------------------- 
            float padding = 0f;
            if (maxWidthRun != null)
            {
                var box = maxWidthRun.OwnerBox;

                while (box != null)
                {
                    padding += (box.ActualBorderRightWidth + box.ActualPaddingRight) +
                        (box.ActualBorderLeftWidth + box.ActualPaddingLeft);

                    if (box == this)
                    {
                        break;
                    }
                    else
                    {
                        //bubble up***
                        box = box.ParentBox;
                    }
                }
            }
            return maxWidth + padding;

        }
        static void CalculateMinimumWidthAndWidestRun(CssBox box, out float maxWidth, out CssRun maxWidthRun)
        {
            //use line-base style ***

            float maxRunWidth = 0;
            CssRun foundRun = null;
            foreach (CssLineBox lineBox in box.GetLineBoxIter())
            {
                foreach (CssRun run in lineBox.GetRunIter())
                {
                    if (run.Width >= maxRunWidth)
                    {
                        foundRun = run;
                        maxRunWidth = run.Width;
                    }
                }
            }
            maxWidth = maxRunWidth;
            maxWidthRun = foundRun;
        }


        /// <summary>
        /// Get the total margin value (left and right) from the given box to the given end box.<br/>
        /// </summary>
        /// <param name="box">the box to start calculation from.</param>
        /// <returns>the total margin</returns>
        static float CalculateWidthMarginRecursiveUp(CssBox box)
        {
            float sum = 0f;
            if (box.SizeWidth > CssBox.MAX_RIGHT || (box.ParentBox != null && box.ParentBox.SizeWidth > CssBox.MAX_RIGHT))
            {
                while (box != null)
                {
                    //to upper
                    sum += box.ActualMarginLeft + box.ActualMarginRight;
                    box = box.ParentBox;
                }
            }
            return sum;
        }

        /// <summary>
        /// Gets the maximum bottom of the boxes inside the startBox
        /// </summary>
        /// <param name="startBox"></param>
        /// <param name="currentMaxBottom"></param>
        /// <returns></returns>
        internal static float CalculateMaximumBottom(CssBox startBox, float currentMaxBottom, float parentOffset)
        {
            //recursive
            if (startBox.HasRuns)
            {
                CssLineBox lastline = null;
                foreach (var run in startBox.GetRunBackwardIter())//start from last to first
                {
                    if (lastline == null)
                    {
                        lastline = run.HostLine;
                        currentMaxBottom = Math.Max(currentMaxBottom, run.Bottom + parentOffset);
                    }
                    else if (lastline != run.HostLine)
                    {
                        //if step to upper line then stop
                        break;
                    }
                    else
                    {
                        currentMaxBottom = Math.Max(currentMaxBottom, run.Bottom + parentOffset);
                    }
                }
                return currentMaxBottom;
            }
            else
            {
                parentOffset = startBox.GlobalY;
                foreach (var b in startBox.Boxes)
                {
                    currentMaxBottom = Math.Max(currentMaxBottom, CalculateMaximumBottom(b, currentMaxBottom, parentOffset));
                }
                return currentMaxBottom;
            }


        }


        /// <summary>
        /// Inherits inheritable values from parent.
        /// </summary>
        internal new void InheritStyles(CssBoxBase box, bool clone = false)
        {
            base.InheritStyles(box, clone);
        }



        /// <summary>
        /// Calculate the actual right of the box by the actual right of the child boxes if this box actual right is not set.
        /// </summary>
        /// <returns>the calculated actual right value</returns>
        float CalculateActualRight()
        {
            if (GlobalActualRight > CssBox.MAX_RIGHT)
            {
                var maxRight = 0f;
                foreach (var box in Boxes)
                {
                    maxRight = Math.Max(maxRight, box.GlobalActualRight + box.ActualMarginRight);
                }
                return maxRight + ActualPaddingRight + ActualMarginRight + ActualBorderRightWidth;
            }
            else
            {
                return GlobalActualRight;
            }
        }
        bool IsLastChild
        {
            get
            {
                return this.ParentBox.Boxes[this.ParentBox.ChildCount - 1] == this;
            }

        }
        /// <summary>
        /// Gets the result of collapsing the vertical margins of the two boxes
        /// </summary>
        /// <param name="upperSibling">the previous box under the same parent</param>
        /// <returns>Resulting top margin</returns>
        protected float MarginTopCollapse(CssBox upperSibling)
        {
            float value;
            if (upperSibling != null)
            {
                value = Math.Max(upperSibling.ActualMarginBottom, this.ActualMarginTop);
                this.CollapsedMarginTop = value;
            }
            else if (_parentBox != null &&
                ActualPaddingTop < 0.1 &&
                ActualPaddingBottom < 0.1 &&
                _parentBox.ActualPaddingTop < 0.1 &&
                _parentBox.ActualPaddingBottom < 0.1)
            {
                value = Math.Max(0, ActualMarginTop - Math.Max(_parentBox.ActualMarginTop, _parentBox.CollapsedMarginTop));
            }
            else
            {
                value = ActualMarginTop;
            }
            return value;
        }
        /// <summary>
        /// Gets the result of collapsing the vertical margins of the two boxes
        /// </summary>
        /// <returns>Resulting bottom margin</returns>
        private float MarginBottomCollapse()
        {

            float margin = 0;
            if (ParentBox != null && this.IsLastChild && _parentBox.ActualMarginBottom < 0.1)
            {
                var lastChildBottomMargin = _boxes[_boxes.Count - 1].ActualMarginBottom;
                margin = (Height.IsAuto) ? Math.Max(ActualMarginBottom, lastChildBottomMargin) : lastChildBottomMargin;
            }
            return Math.Max(GlobalActualBottom, _boxes[_boxes.Count - 1].GlobalActualBottom + margin + ActualPaddingBottom + ActualBorderBottomWidth);
        }

        /// <summary>
        /// Deeply offsets the top of the box and its contents
        /// </summary>
        /// <param name="amount"></param>
        internal void OffsetGlobalTop(float amount)
        {
            if (amount == 0)
            {
                return;
            }
            if (this.LineBoxCount > 0)
            {
            }
            else
            {
                foreach (CssBox b in Boxes)
                {
                    b.OffsetGlobalTop(amount);
                }
            }
            if (_listItemBox != null)
            {
                _listItemBox.OffsetGlobalTop(amount);
            }

            this.OffsetOnlyGlobal(0, amount);
        }
        /// <summary>
        /// Paints the background of the box
        /// </summary>
        /// <param name="g">the device to draw into</param>
        /// <param name="rect">the bounding rectangle to draw in</param>
        /// <param name="isFirst">is it the first rectangle of the element</param>
        /// <param name="isLast">is it the last rectangle of the element</param>
        internal void PaintBackground(IGraphics g, RectangleF rect, bool isFirst, bool isLast)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                Brush brush = null;
                bool dispose = false;
                SmoothingMode smooth = g.SmoothingMode;

                if (BackgroundGradient != System.Drawing.Color.Transparent)
                {
                    brush = new LinearGradientBrush(rect,
                        ActualBackgroundColor,
                        ActualBackgroundGradient,
                        ActualBackgroundGradientAngle);

                    dispose = true;
                }
                else if (RenderUtils.IsColorVisible(ActualBackgroundColor))
                {

                    brush = RenderUtils.GetSolidBrush(ActualBackgroundColor);
                }

                if (brush != null)
                {
                    // atodo: handle it correctly (tables background)
                    // if (isLast)
                    //  rectangle.Width -= ActualWordSpacing + CssUtils.GetWordEndWhitespace(ActualFont);

                    GraphicsPath roundrect = null;
                    if (IsRounded)
                    {
                        roundrect = RenderUtils.GetRoundRect(rect, ActualCornerNW, ActualCornerNE, ActualCornerSE, ActualCornerSW);
                    }

                    if (HtmlContainer != null && !HtmlContainer.AvoidGeometryAntialias && IsRounded)
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                    }

                    if (roundrect != null)
                    {
                        g.FillPath(brush, roundrect);
                    }
                    else
                    {
                        g.FillRectangle(brush, (float)Math.Ceiling(rect.X), (float)Math.Ceiling(rect.Y), rect.Width, rect.Height);
                    }

                    g.SmoothingMode = smooth;

                    if (roundrect != null) roundrect.Dispose();
                    if (dispose) brush.Dispose();
                }

                if (_imageLoadHandler != null && _imageLoadHandler.Image != null && isFirst)
                {
                    BackgroundImageDrawHandler.DrawBackgroundImage(g, this, _imageLoadHandler, rect);
                }
            }
        }


#if DEBUG
        internal void dbugPaintTextWordArea(IGraphics g, PointF offset, CssRun word)
        {
            //g.DrawRectangle(Pens.Blue, word.Left, word.Top, word.Width, word.Height);

        }
#endif


        internal void PaintDecoration(IGraphics g, RectangleF rectangle, bool isFirst, bool isLast)
        {
            float y = 0f;
            switch (this.TextDecoration)
            {
                default:
                    return;
                case CssTextDecoration.Underline:
                    {
                        var h = g.MeasureString(" ", ActualFont).Height;
                        float desc = FontsUtils.GetDescent(ActualFont, g);
                        y = (float)Math.Round(rectangle.Top + h - desc + 0.5);
                    } break;
                case CssTextDecoration.LineThrough:
                    {
                        y = rectangle.Top + rectangle.Height / 2f;
                    } break;
                case CssTextDecoration.Overline:
                    {
                        y = rectangle.Top;
                    } break;
            }


            y -= ActualPaddingBottom - ActualBorderBottomWidth;

            float x1 = rectangle.X;
            if (isFirst)
            {
                x1 += ActualPaddingLeft + ActualBorderLeftWidth;
            }


            float x2 = rectangle.Right;
            if (isLast)
            {
                x2 -= ActualPaddingRight + ActualBorderRightWidth;
            }

            var pen = RenderUtils.GetPen(ActualColor);

            g.DrawLine(pen, x1, y, x2, y);
        }


        /// <summary>
        /// On image load process complete with image request refresh for it to be painted.
        /// </summary>
        /// <param name="image">the image loaded or null if failed</param>
        /// <param name="rectangle">the source rectangle to draw in the image (empty - draw everything)</param>
        /// <param name="async">is the callback was called async to load image call</param>
        private void OnImageLoadComplete(Image image, Rectangle rectangle, bool async)
        {
            if (image != null && async)
            {
                HtmlContainer.RequestRefresh(false);
            }
        }



        /// <summary>
        /// Get brush for selection background depending if it has external and if alpha is required for images.
        /// </summary>
        /// <param name="forceAlpha">used for images so they will have alpha effect</param>
        protected Brush GetSelectionBackBrush(bool forceAlpha)
        {
            var backColor = HtmlContainer.SelectionBackColor;
            if (backColor != System.Drawing.Color.Empty)
            {
                if (forceAlpha && backColor.A > 180)
                    return RenderUtils.GetSolidBrush(System.Drawing.Color.FromArgb(180, backColor.R, backColor.G, backColor.B));
                else
                    return RenderUtils.GetSolidBrush(backColor);
            }
            else
            {
                return CssUtils.DefaultSelectionBackcolor;
            }
        }


        internal bool CanBeRefererenceSibling
        {
            get { return this.CssDisplay != Dom.CssDisplay.None && !this.IsAbsolutePosition; }
        }


        /// <summary>
        /// ToString override.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var tag = HtmlTag != null ? string.Format("<{0}>", HtmlTag.Name) : "anon";

            if (IsBlock)
            {
                return string.Format("{0}{1} Block {2}, Children:{3}", ParentBox == null ? "Root: " : string.Empty, tag, FontSize, Boxes.Count);
            }
            else if (this.CssDisplay == CssDisplay.None)
            {
                return string.Format("{0}{1} None", ParentBox == null ? "Root: " : string.Empty, tag);
            }
            else
            {
                if (this.MayHasSomeTextContent)
                {
                    return string.Format("{0}{1} {2}: {3}", ParentBox == null ? "Root: " : string.Empty, tag,
                        this.CssDisplay.ToCssStringValue(), this.CopyTextContent());
                }
                else
                {
                    return string.Format("{0}{1} {2}: {3}", ParentBox == null ? "Root: " : string.Empty, tag,
                        this.CssDisplay.ToCssStringValue(), "");
                }
            }
        }

        #endregion



        //---------------------------------------
        internal void UseExpectedHeight()
        {
            this.SetHeight(this.ExpectedHeight);
        }
        internal void SetHeightToZero()
        {
            this.SetHeight(0);
        }
        internal float AvailableContentWidth
        {
            get
            {
                return this.SizeWidth - this.ActualPaddingLeft - this.ActualPaddingRight - this.ActualBorderLeftWidth - this.ActualBorderRightWidth;
            }
        }
        internal bool IsPointInClientArea(float x, float y)
        {
            return x >= this.GlobalClientLeft && x < this.GlobalClientRight &&
                    y >= this.GlobalClientTop && y < this.GlobalClientBottom;
        }
    }
}