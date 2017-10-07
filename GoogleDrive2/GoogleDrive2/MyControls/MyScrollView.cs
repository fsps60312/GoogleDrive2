using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace GoogleDrive2.MyControls
{
    class MyScrollView:ScrollView
    {
        public double MyScrollY
        {
            get { return this.ScrollY; }
            set {/*Currently no good solution to this, that is no gurrenteed to be executed -> */ /*this.ScrollToAsync(0, value, false); */}
        }
    }
}
