using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace GoogleDrive2.Pages.TestPage
{
    class TouchEventImplementPage : MyContentPage
    {
        MyAbsoluteLayout ALmain;
        MyLabel LBtxt;
        BoxView BX;
        private void InitializeViews()
        {
            this.Title = "Touch Events";
            {
                ALmain = new MyAbsoluteLayout();
                {
                    LBtxt = new MyLabel { Text = "Status" };
                    ALmain.Children.Add(LBtxt, new Point(0, 0));
                }
                {
                    BX = new BoxView { BackgroundColor = Color.Red, WidthRequest = 100, HeightRequest = 100 };
                    ALmain.Children.Add(BX, new Rectangle(10, 10, -1, -1), AbsoluteLayoutFlags.None);
                }
                this.Content = ALmain;
            }
        }
        private int statusCnt = 0;
        private void ShowStatus(string status) { LBtxt.Text = $"{status} #{++statusCnt}"; }
        Random rand = new Random();
        private void RegisterEvents()
        {
            {
                var r = new TapGestureRecognizer
                {
                    NumberOfTapsRequired = 1
                };
                r.Tapped += new EventHandler((o, args) =>
                {
                    ShowStatus("Tap " + args.ToString());
                    BX.BackgroundColor = Color.FromRgba(rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble());
                });
                BX.GestureRecognizers.Add(r);
            }
            {
                var r = new TapGestureRecognizer
                {
                    NumberOfTapsRequired = 2
                };
                r.Tapped += new EventHandler((o, args) =>
                {
                    ShowStatus("Double Tap");
                    BX.WidthRequest *= 1.5;
                    BX.HeightRequest *= 1.5;
                });
                BX.GestureRecognizers.Add(r);
            }
            {
                var r = new PinchGestureRecognizer();
                //double x0 = double.NaN, y0 = 0;
                r.PinchUpdated += new EventHandler<PinchGestureUpdatedEventArgs>((o, args) =>
                {
                    ShowStatus($"Status: {args.Status}, Scale: {args.Scale}, Origin: {args.ScaleOrigin}");
                    {
                        var b = MyAbsoluteLayout.GetLayoutBounds(BX);
                        double x1 = b.X, x2 = b.X + BX.WidthRequest, y1 = b.Y, y2 = b.Y + BX.HeightRequest;
                        double x = b.X + BX.WidthRequest * args.ScaleOrigin.X, y = b.Y + BX.HeightRequest * args.ScaleOrigin.Y, s = args.Scale;
                        x1 = x + (x1 - x) * s;
                        x2 = x + (x2 - x) * s;
                        y1 = y + (y1 - y) * s;
                        y2 = y + (y2 - y) * s;
                        MyAbsoluteLayout.SetLayoutBounds(BX, new Rectangle(x1, y1, -1, -1));
                        BX.WidthRequest = x2 - x1;
                        BX.HeightRequest = y2 - y1;
                    }
                    //if (args.Status == GestureStatus.Completed) x0 = double.NaN;
                    //else
                    //{
                    //    if (!double.IsNaN(x0))
                    //    {
                    //        var b = MyAbsoluteLayout.GetLayoutBounds(BX);
                    //        MyAbsoluteLayout.SetLayoutBounds(BX, new Rectangle(b.X + BX.WidthRequest * (args.ScaleOrigin.X - x0), b.Y + BX.HeightRequest * (args.ScaleOrigin.Y - y0), -1, -1));
                    //    }
                    //    x0 = args.ScaleOrigin.X;
                    //    y0 = args.ScaleOrigin.Y;
                    //}
                });
                BX.GestureRecognizers.Add(r);
            }
            {
                var r = new PanGestureRecognizer();
                double x = double.NaN, y = 0;
                r.PanUpdated += new EventHandler<PanUpdatedEventArgs>((o, args) =>
               {
                    //await MyLogger.Alert("hi");
                    ShowStatus($"Status: {args.StatusType}, TotalX: {args.TotalX}, TotalY: {args.TotalY}");
                   if (args.StatusType == GestureStatus.Completed)
                   {
                       x = double.NaN;
                       return;
                   }
                   if (double.IsNaN(x))
                   {
                       var b = MyAbsoluteLayout.GetLayoutBounds(BX);
                       x = b.X;
                       y = b.Y;
                        //await MyLogger.Alert($"{x} {y}");
                    }
                   MyAbsoluteLayout.SetLayoutBounds(BX, new Rectangle(x + args.TotalX, y + args.TotalY, -1, -1));
               });
                BX.GestureRecognizers.Add(r);
            }
            //LBtxt.GestureRecognizers.Add(new TapGestureRecognizer
            //{
            //    Command = new Command(() => { ShowStatus("Tap"); }),
            //    NumberOfTapsRequired = 1
            //});
            //LBtxt.GestureRecognizers.Add(new TapGestureRecognizer
            //{
            //    Command = new Command(() => { ShowStatus("Double Tap"); }),
            //    NumberOfTapsRequired = 2
            //});
        }
        public TouchEventImplementPage()
        {
            InitializeViews();
            RegisterEvents();
        }
    }
}
