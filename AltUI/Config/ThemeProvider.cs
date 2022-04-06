﻿using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace AltUI.Config
{
    public static class ThemeProvider
    {
        private static object RegistryCGV(string keyName, string valueName, object defaultValue)
        {
            try
            {
                return Registry.GetValue(keyName, valueName, defaultValue);
            }
            catch { return defaultValue; }
        }
        public static bool LightMode = (int)RegistryCGV("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 0) == 1;
        public static bool TransparencyMode = (int)RegistryCGV("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 0) == 1;
        public static int WindowsVersion = int.Parse((string)RegistryCGV("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuild", 0));
        public static Color BackgroundColour
        {
            get
            {
                if (LightMode)
                {
                    if (TransparencyMode & WindowsVersion >= 22000)
                    { return Color.FromArgb(243, 243, 243); }
                    else { return Color.FromArgb(255, 255, 255); }
                }
                else if (TransparencyMode & WindowsVersion >= 22000)
                { return Color.FromArgb(31, 31, 32); }
                else { return Color.FromArgb(16, 16, 17); }

            }
        }
        private static readonly int AccentColour = (int)RegistryCGV("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\DWM", "AccentColor", 0);
        public static Color GetAccentColor(int brighten)
        {
            if (WindowsVersion >= 22000)
            {
                return ParseDWordColor(AccentColour, brighten);
            }
            else
            {
                if (brighten != 0)
                    return Color.FromArgb(0, 120, 215);
                else
                    return Color.FromArgb(30, 71, 112);
            }
        }
        private static Color ParseDWordColor(int color, int brighten)
        {
            int
                a = (color >> 24) & 0xFF, r = (color >> 0) & 0xFF, g = (color >> 8) & 0xFF, b = (color >> 16) & 0xFF;
            if (b + brighten > 255) { b = 255; }
            else { b += brighten; }
            if (g + brighten > 255) { g = 255; }
            else { g += brighten; }
            if (r + brighten > 255) { r = 255; }
            else { r += brighten; }
            return Color.FromArgb(a,r,g,b);
        }
        private static ITheme theme;
        public static ITheme Theme
        {
            get
            {
                if (LightMode)
                    theme = new LightTheme();
                else
                    theme = new DarkTheme();
                return theme;
            }
            set
            {
                theme = value;
            }
        }
        private static int ColorToHex(Color c) => Convert.ToInt32($"{c.B:X2}{c.G:X2}{c.R:X2}", 16);
        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, int[] attrValue, int attrSize);
        public static void SetupWindow(IntPtr Handle, int Corners)
        {
            var MSOTOI = Marshal.SizeOf(typeof(int));
            // Use rounded corners on 22000+
            DwmSetWindowAttribute(Handle, 33, new[] { Corners }, MSOTOI);
            // Set Window border to match control border
            DwmSetWindowAttribute(Handle, 34, new[] { ColorToHex(Theme.Colors.GreySelection) }, MSOTOI);
            // Set Window Caption to match background
            if (WindowsVersion < 22523 || !TransparencyMode)
                DwmSetWindowAttribute(Handle, 35, new[] { ColorToHex(Theme.Colors.GreyBackground) }, MSOTOI);
            // Set Titlebar Font to match Theme
            DwmSetWindowAttribute(Handle, 36, new[] { ColorToHex(Theme.Colors.LightText) }, MSOTOI);
            // Apply immersive dark mode if it's used by system
            DwmSetWindowAttribute(Handle, 20, new[] { LightMode ? 0 : 1 }, MSOTOI);
            if (TransparencyMode & WindowsVersion >= 22000)
            {
                // Mica for below 22523
                DwmSetWindowAttribute(Handle, 1029, new[] { 1 }, MSOTOI);
                // Mica for 22523 and up
                DwmSetWindowAttribute(Handle, 38, new[] { 2 }, MSOTOI);
            }
        }
    }
    public static class RoundRects
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius, bool flatBottom)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            if (flatBottom)
            {
                // bottom line
                PointF br = new PointF(bounds.Right, bounds.Bottom + 1);
                PointF bl = new PointF(bounds.Left, bounds.Bottom + 1);
                path.AddLine(br, bl);
            }
            else
            {       
                // bottom right arc
                arc.Y = bounds.Bottom - diameter;
                path.AddArc(arc, 0, 90);

                // bottom left arc 
                arc.X = bounds.Left;
                path.AddArc(arc, 90, 90);
            }
            path.CloseFigure();
            return path;
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius, bool flatBottom)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius, flatBottom))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius, bool flatBottom)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (brush == null)
                throw new ArgumentNullException("brush");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius, flatBottom))
            {
                graphics.FillPath(brush, path);
            }
        }
    }

}
