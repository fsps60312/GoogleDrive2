using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace GoogleDrive2.MyControls
{
    class MyUnwipableView : ContentView
    {
        public MyUnwipableView()
        {
            this.GestureRecognizers.Add(new PinchGestureRecognizer());
        }
    }
}
