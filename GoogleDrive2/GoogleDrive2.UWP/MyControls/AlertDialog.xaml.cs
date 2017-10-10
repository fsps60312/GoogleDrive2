using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace GoogleDrive2.UWP.MyControls
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlertDialog : Page
    {
        public static AlertDialog Instance = null;
        public TextBox TXBmain;
        public Grid GDmain;
        public event Libraries.Events.MyEventHandler<string> OKClicked;
        public AlertDialog():this(new List<string> { "OK" }) { }
        public AlertDialog(List<string>buttons)
        {
            Instance = this;
            GDmain = new Grid();
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            {
                TXBmain = new TextBox { TextWrapping = TextWrapping.NoWrap, AcceptsReturn = true,FontFamily=new FontFamily("Consolas") };
                ScrollViewer.SetVerticalScrollBarVisibility(TXBmain, ScrollBarVisibility.Auto);
                ScrollViewer.SetHorizontalScrollBarVisibility(TXBmain, ScrollBarVisibility.Auto);
                GDmain.Children.Add(TXBmain);
            }
            {
                int col = 0;
                foreach (var txt in buttons)
                {
                    GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var btn = new Button { Content = txt, HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(30, 10, 30, 10) };
                    btn.Click += delegate
                    {
                        OKClicked?.Invoke(txt);
                    };
                    GDmain.Children.Add(btn);
                    Grid.SetRow(btn, 1);
                    Grid.SetColumn(btn, col++);
                }
            }
            this.Content = GDmain;
        }
    }
}
