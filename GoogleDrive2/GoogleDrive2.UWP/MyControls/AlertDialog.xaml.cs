﻿using System;
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
        public Button BTNok;
        public Grid GDmain;
        public event Libraries.Events.EmptyEventHandler OKClicked;
        public AlertDialog()
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
                BTNok = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(50, 10, 50, 10) };
                BTNok.Click += delegate
                {
                    OKClicked?.Invoke();
                };
                GDmain.Children.Add(BTNok);
                Grid.SetRow(BTNok, 1);
            }
            this.Content = GDmain;
        }
    }
}
