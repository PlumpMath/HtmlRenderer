﻿// 2015,2014 ,BSD, WinterDev 
//ArthurHub

using System;
using System.Collections.Generic;

using PixelFarm.Drawing;
using LayoutFarm.WebDom;
using LayoutFarm.HtmlBoxes;
using LayoutFarm.InternalHtmlDom;
using LayoutFarm.Composers;

namespace LayoutFarm.Composers
{

    public class HtmlDocument : WebDocument
    {
        DomElement rootNode;
        int domUpdateVersion;

        internal EventHandler DomUpdated;
        public HtmlDocument()
            : base(HtmlPredefineNames.CreateUniqueStringTableClone())
        {
            //default root
            rootNode = new RootElement(this);
        }
        internal HtmlDocument(UniqueStringTable sharedUniqueStringTable)
            : base(sharedUniqueStringTable)
        {
            //default root
            rootNode = new RootElement(this);
        }
        public override DomElement RootNode
        {
            get
            {
                return rootNode;
            }
            set
            {
                this.rootNode = value;
            }
        }
        public override int DomUpdateVersion
        {
            get { return this.domUpdateVersion; }
            set
            {
                this.domUpdateVersion = value;
                if (DomUpdated != null)
                {
                    DomUpdated(this, EventArgs.Empty);
                }
            }
        }

        public override DomElement CreateElement(string prefix, string localName)
        {
            return new HtmlElement(this,
                AddStringIfNotExists(prefix),
                AddStringIfNotExists(localName));
        }
        public DomAttribute CreateAttribute(WellknownName attrName)
        {
            return new DomAttribute(this,
                0,
                (int)attrName);
        }
        public override DomTextNode CreateTextNode(char[] strBufferForElement)
        {
            return new HtmlTextNode(this, strBufferForElement);
        }
        internal virtual ActiveCssTemplate ActiveCssTemplate
        {
            get;
            set;
        }
    }

    public class FragmentHtmlDocument : HtmlDocument
    {
        HtmlDocument primaryHtmlDoc;
        internal FragmentHtmlDocument(HtmlDocument primaryHtmlDoc)
            : base(primaryHtmlDoc.UniqueStringTable)
        {
            this.primaryHtmlDoc = primaryHtmlDoc;
        }
        internal override ActiveCssTemplate ActiveCssTemplate
        {
            get
            {
                return this.primaryHtmlDoc.ActiveCssTemplate;
            }
            set
            {

            }
        }

    }
}