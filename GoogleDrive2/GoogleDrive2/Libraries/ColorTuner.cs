using System;
using System.Collections.Generic;
using System.Text;
using Color = Xamarin.Forms.Color;

namespace GoogleDrive2.Libraries
{
    static class ColorTuner
    {
        public static Color Darken(Color c, double v = 0.1)
        {
            return Color.FromRgb(c.R + v, c.G + v, c.B + v);
        }
    }
}
