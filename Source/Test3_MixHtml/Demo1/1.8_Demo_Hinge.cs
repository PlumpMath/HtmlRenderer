﻿// 2015,2014 ,Apache2, WinterDev
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PixelFarm.Drawing;
using LayoutFarm.UI;

namespace LayoutFarm
{
    [DemoNote("1.8 Hinge")]
    class Demo_Hinge : DemoBase
    {
        Bitmap arrowBmp;
        protected override void OnStartDemo(SampleViewport viewport)
        {

            var comboBox1 = CreateComboBox(20, 20);
            viewport.AddContent(comboBox1);

            var comboBox2 = CreateComboBox(50, 50);
            viewport.AddContent(comboBox2);


            //------------
            var menuItem = CreateMenuItem(50, 100);
            var menuItem2 = CreateMenuItem(5, 5);
            menuItem.AddSubMenuItem(menuItem2);


            viewport.AddContent(menuItem);

        }

        LayoutFarm.CustomWidgets.ComboBox CreateComboBox(int x, int y)
        {
            var comboBox = new CustomWidgets.ComboBox(400, 20);
            comboBox.SetLocation(x, y);
            //--------------------
            //1. create landing part
            var landPart = new LayoutFarm.CustomWidgets.Panel(400, 20);
            landPart.BackColor = Color.Green;
            comboBox.LandPart = landPart;
            //--------------------------------------
            //add small px to land part
            //image
            //load bitmap with gdi+                
            if (arrowBmp == null)
            {
                arrowBmp = LoadBitmap("../../Demo/arrow_open.png");
            }
            LayoutFarm.CustomWidgets.ImageBox imgBox = new CustomWidgets.ImageBox(arrowBmp.Width, arrowBmp.Height);
            imgBox.Image = arrowBmp;
            //--------------------------------------
            //2. float part
            var floatPart = new LayoutFarm.CustomWidgets.Panel(400, 100);
            floatPart.BackColor = Color.Blue;
            comboBox.FloatPart = floatPart;

            //--------------------------------------
            //if click on this image then
            imgBox.MouseDown += (s, e) =>
            {
                e.CancelBubbling = true;
                if (comboBox.IsOpen)
                {
                    comboBox.CloseHinge();
                }
                else
                {
                    comboBox.OpenHinge();
                }
            };
            imgBox.LostSelectedFocus += (s, e) =>
            {
                if (comboBox.IsOpen)
                {
                    comboBox.CloseHinge();
                }
            };

            landPart.AddChildBox(imgBox);
            return comboBox;
        }
        LayoutFarm.CustomWidgets.MenuItem CreateMenuItem(int x, int y)
        {
            var mnuItem = new CustomWidgets.MenuItem(150, 20);
            mnuItem.SetLocation(x, y);
            //--------------------
            //1. create landing part
            var landPart = new LayoutFarm.CustomWidgets.Panel(150, 20);
            landPart.BackColor = Color.OrangeRed;
            mnuItem.LandPart = landPart;
            //--------------------------------------
            //add small px to land part
            //image
            //load bitmap with gdi+                
            if (arrowBmp == null)
            {
                arrowBmp = LoadBitmap("../../Demo/arrow_open.png");
            }
            LayoutFarm.CustomWidgets.ImageBox imgBox = new CustomWidgets.ImageBox(arrowBmp.Width, arrowBmp.Height);
            imgBox.Image = arrowBmp;
            landPart.AddChildBox(imgBox);

            //--------------------------------------
            //if click on this image then
            imgBox.MouseDown += (s, e) =>
            {
                e.CancelBubbling = true;
                //1. maintenace parent menu***
                mnuItem.MaintenanceParentOpenState();
                //-----------------------------------------------
                if (mnuItem.IsOpened)
                {
                    mnuItem.Close();
                }
                else
                {
                    mnuItem.Open();
                }
            };
            imgBox.MouseUp += (s, e) =>
            {
                mnuItem.UnmaintenanceParentOpenState();
            };
            imgBox.LostSelectedFocus += (s, e) =>
            {
                if (!mnuItem.MaintenceOpenState)
                {
                    mnuItem.CloseRecursiveUp();
                }
            };

            //--------------------------------------
            //2. float part
            var floatPart = new LayoutFarm.CustomWidgets.MenuBox(400, 100);
            floatPart.BackColor = Color.Gray;
            mnuItem.FloatPart = floatPart;


            return mnuItem;
        }
        static Bitmap LoadBitmap(string filename)
        {
            System.Drawing.Bitmap gdiBmp = new System.Drawing.Bitmap(filename);
            Bitmap bmp = new Bitmap(gdiBmp.Width, gdiBmp.Height, gdiBmp);
            return bmp;
        }
    }
}