using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;

namespace GoogleDrive2.Pages.TestPage
{
    class GetAuthorizationCodePage:MyContentPage
    {
        MyButton BTNrun;
        MyEntry ETresult;
        MyGrid GDmain;
        MyWebView WVmain;
        public GetAuthorizationCodePage()
        {
            this.Title = "Get Authorization Code";
            {
                GDmain = new MyGrid();
                GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Auto) });
                GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Star) });
                {
                    BTNrun = new MyButton { Text = "Run" };
                    BTNrun.Clicked += async delegate
                    {
                        BTNrun.IsEnabled = false;
                        ETresult.Text = await RestRequests.RestRequestsAuthorizer.DriveAuthorizer.GetAccessTokenAsync(true);
                        //if (string.IsNullOrEmpty(ETresult.Text))
                        //{
                        //    //var ans = await Libraries.DriveAuthorizer.GetAuthorizationCode(WVmain);
                        //    var ans = await Libraries.DriveAuthorizer.GetAuthorizationCode();
                        //    ETresult.Text = ans;
                        //    //await MyLogger.Alert(ans);
                        //}
                        //else if(ETresult.Text.StartsWith("4/"))
                        //{
                        //    var ans = await Libraries.DriveAuthorizer.ExchangeCodeForTokens(ETresult.Text);
                        //    var keyWord = "\"refresh_token\" : \"";
                        //    var idx = ans.IndexOf(keyWord);
                        //    if(idx!=-1)
                        //    {
                        //        var s = ans.Substring(idx + keyWord.Length);
                        //        ETresult.Text = s.Remove(s.IndexOf('"'));
                        //    }
                        //    await MyLogger.Alert(ans);
                        //}
                        //else
                        //{
                        //    var ans = await Libraries.DriveAuthorizer.RefreshTokenAsync(ETresult.Text);
                        //    await MyLogger.Alert(ans);
                        //}
                        BTNrun.IsEnabled = true;
                    };
                    GDmain.Children.Add(BTNrun, 0, 0);
                }
                {
                    ETresult = new MyEntry();
                    GDmain.Children.Add(ETresult, 0, 1);
                }
                {
                    WVmain = new MyWebView
                    {
                        Source = "http://codingsimplifylife.blogspot.tw/"
                    };
                    GDmain.Children.Add(WVmain, 0, 2);
                }
                this.Content = GDmain;
            }
        }
    }
}
