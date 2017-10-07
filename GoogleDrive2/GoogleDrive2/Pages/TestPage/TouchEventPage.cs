using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.TestPage
{
    class TouchEventPage:MyContentPage
    {
        MyGrid view;
        MyLabel LBtxt;
        MyActivityIndicator AIidle;
        private void InitializeViews()
        {
            this.Title = "Touch Events";
            {
                view = new MyGrid();
                view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                {
                    LBtxt = new MyLabel {Text="Touch status" };
                    view.Children.Add(LBtxt,0,0);
                }
                {
                    AIidle = new MyActivityIndicator();
                    view.Children.Add(AIidle, 0, 1);
                }
                this.Content = view;
            }
        }
        private int statusCnt = 0;
        private void ShowStatus(string status) { LBtxt.Text = $"{status} #{++statusCnt}"; }
        private void RegisterEvents()
        {
            view.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { ShowStatus("Tap"); }),
                NumberOfTapsRequired = 1
            });
            view.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => { ShowStatus("Double Tap"); }),
                NumberOfTapsRequired = 2
            });
            {
                var r = new PinchGestureRecognizer();
                r.PinchUpdated += new EventHandler<PinchGestureUpdatedEventArgs>((o, args) =>
                  {
                      ShowStatus($"Status: {args.Status}, Scale: {args.Scale}, Origin: {args.ScaleOrigin}");
                  });
                view.GestureRecognizers.Add(r);
            }
            {
                var r = new PanGestureRecognizer();
                r.PanUpdated += new EventHandler<PanUpdatedEventArgs>((o, args) =>
                  {
                      ShowStatus($"Status: {args.StatusType}, TotalX: {args.TotalX}, TotalY: {args.TotalY}");
                  });
                view.GestureRecognizers.Add(r);
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
        public TouchEventPage()
        {
            InitializeViews();
            RegisterEvents();
        }
    }
}
