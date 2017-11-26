using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace GoogleDrive2.Pages.AboutPage
{
    class AboutPage:MyContentPage
    {
        MyWebView WVmain;
        MyGrid GDmain;
        MyStackPanel SPmain;
        MyLabel LBinfo;
        MyEntry ETurl;
        private void AddButtons()
        {
            WVmain.Source = "https://codingsimplifylife.blogspot.tw/";
            AddButton("My Blog", "Welcome to code風景區, my blog! :D", "https://codingsimplifylife.blogspot.tw/");
            AddButton("The Source Code", "This app is open-source, if you're a developer and interested in improving this app, the source code is here. Welcome! Note: This App is created with Xamarin.Forms ;)", "https://github.com/fsps60312/GoogleDrive2");
            AddButton("Let Talk!", "The Facebook Page, Code風景區, is meant to be more interactive and noisy(?) comparing to my blog, code風景區. Welcome talk to me at Code風景區 and ask anything you want! ^_^", "https://www.facebook.com/CodingSimplifyLife/");
            AddButton("Hacker's Notice", "I know, I know. My client secret was made public here. You can create your own through your Google Account for free too, so do not abuse this one please. No benefits for you to do so! ><", "https://github.com/fsps60312/GoogleDrive2/blob/master/GoogleDrive2/GoogleDrive2/RestRequests/RestRequestsAuthorizer.DriveAuthorizer.cs");
            AddButton("Private Policy", "The Private Policy, which is required in order to publish this App. So simple, huh? XD", "https://codingsimplifylife.blogspot.tw/2017/11/fsps60312-drive-private-policy.html");
        }
        private void AddButton(string text,string title,string url)
        {
            MyButton btn = new MyButton { Text = text };
            btn.Clicked +=async delegate
              {
                  btn.IsEnabled = false;
                  LBinfo.Text = title;
                  WVmain.Source = url;
                  await Task.Delay(500);
                  btn.IsEnabled = true;
              };
            SPmain.Children.Add(btn);
        }
        private void RegisterEvents()
        {
            WVmain.Navigated += (sender, e) => { ETurl.Text = e.Url; };
            WVmain.Navigating += (sender, e) => { ETurl.Text = e.Url; };
        }
        private void InitializeViews()
        {
            this.Title = "About";
            {
                GDmain = new MyGrid();
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                {
                    SPmain = new MyStackPanel(ScrollOrientation.Vertical);
                    GDmain.Children.Add(SPmain, 0, 2);
                }
                {
                    LBinfo = new MyLabel { Text = "Welcome to Ask me Questions or Provide Feedback! Select buttons below to know more!" };
                    GDmain.Children.Add(LBinfo, 0, 0);
                    MyGrid.SetColumnSpan(LBinfo, 2);
                }
                {
                    ETurl = new MyEntry();
                    GDmain.Children.Add(ETurl, 0, 1);
                    MyGrid.SetColumnSpan(ETurl, 2);
                }
                {
                    WVmain = new MyWebView();
                    GDmain.Children.Add(WVmain, 1, 2);
                }
                this.Content = GDmain;
            }
        }
        public AboutPage()
        {
            InitializeViews();
            RegisterEvents();
            AddButtons();
        }
    }
}
