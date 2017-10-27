using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using System.Reflection;
using System.ComponentModel;
using Color = Xamarin.Forms.Color;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace GoogleDrive2.Pages.TestPage
{
    class ColorTestPage:MyContentPage
    {
        public class ColorItemBarViewModel : MyControls.BarsListPanel.MyDisposable
        {
            private string __Text__;
            public string Text
            {
                get { return __Text__; }
                set
                {
                    if (__Text__ == value) return;
                    __Text__ = value;
                    OnPropertyChanged("Text");
                }
            }
            private Xamarin.Forms.Color __BackgroundColor__;
            public Xamarin.Forms.Color BackgroundColor
            {
                get { return __BackgroundColor__; }
                set
                {
                    if (__BackgroundColor__ == value) return;
                    __BackgroundColor__ = value;
                    OnPropertyChanged("BackgroundColor");
                }
            }
            private Xamarin.Forms.Color __TextColor__;
            public Xamarin.Forms.Color TextColor
            {
                get { return __TextColor__; }
                set
                {
                    if (__TextColor__ == value) return;
                    __TextColor__ = value;
                    OnPropertyChanged("TextColor");
                }
            }
            private Color GetTextColor(Color backgroundColor)
            {
                if ((backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3 > 0.5) return Color.Black;
                else return Color.White;
            }
            public ColorItemBarViewModel(Tuple<Color, string> c)
            {
                Text = c.Item2;
                BackgroundColor = c.Item1;
                TextColor = GetTextColor(c.Item1);
            }
        }
        class ColorItemBar : MyControls.BarsListPanel.DataBindedLabel<ColorItemBarViewModel>
        {
            public ColorItemBar()
            {
                HeightRequest = 40;
                FontFamily = "Consolas";
                this.SetBinding(MyLabel.TextProperty, "Text");
                this.SetBinding(MyLabel.BackgroundColorProperty, "BackgroundColor");
                this.SetBinding(MyLabel.TextColorProperty, "TextColor");
                {
                    var r = new TapGestureRecognizer
                    {
                        NumberOfTapsRequired = 1
                    };
                    r.Tapped += delegate
                      {
                          (this.Parent as Layout).BackgroundColor = this.BackgroundColor;
                      };
                    this.GestureRecognizers.Add(r);
                }
                this.Margin = new Thickness(5);
                //{
                //    var r = new Xamarin.Forms.TapGestureRecognizer
                //    {
                //        NumberOfTapsRequired = 1
                //    };
                //    bool bolded = false;
                //    r.Tapped += delegate
                //    {
                //        bolded ^= true;
                //        this.FontAttributes = bolded ? FontAttributes.Bold : FontAttributes.None;
                //    };
                //    this.GestureRecognizers.Add(r);
                //}
            }
        }
        MyGrid GDmain;
        MyEntry ETsearch;
        MySwitch SWonlyName;
        MyControls.BarsListPanel.BarsListPanel<ColorItemBar,ColorItemBarViewModel> BLmain;
        List<Tuple<Color, string>> AllLBs = new List<Tuple<Color, string>>(), LBs = new List<Tuple<Color, string>>();
        async Task RefreshLabels()
        {
            await BLmain.ClearAsync();
            foreach (var l in this.LBs)
            {
                BLmain.PushBack(new ColorItemBarViewModel(l));
            }
        }
        void SortLabels(bool sortByName)
        {
            var cp = sortByName ? new Comparison<Tuple<Color, string>>((a, b) =>
               {
                   return a.Item2.CompareTo(b.Item2);
               }) : new Comparison<Tuple<Color, string>>((a, b) =>
             {
                   return (a.Item1.R + a.Item1.G + a.Item1.B).CompareTo(b.Item1.R + b.Item1.G + b.Item1.B);
                //if (a.Item1.R != b.Item1.R) return a.Item1.R.CompareTo(b.Item1.R);
                //if (a.Item1.G != b.Item1.G) return a.Item1.G.CompareTo(b.Item1.G);
                //if (a.Item1.B != b.Item1.B) return a.Item1.B.CompareTo(b.Item1.B);
                //return 0;
            });
            LBs.Sort(cp);
            AllLBs.Sort(cp);
            BLmain.Sort(new Comparison<ColorItemBarViewModel>((a, b) => { return cp(new Tuple<Color, string>(a.BackgroundColor, a.Text), new Tuple<Color, string>(b.BackgroundColor, b.Text)); }));
            //await RefreshLabels();
        }
        private void InitializeViews()
        {
            this.Title = "Color Test";
            GDmain = new MyGrid();
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            GDmain.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            GDmain.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            {
                ETsearch = new MyEntry();
                GDmain.Children.Add(ETsearch, 0, 0);
            }
            {
                SWonlyName = new MySwitch("Search in Name", "Search in Full Text");
                GDmain.Children.Add(SWonlyName, 1, 0);
            }
            {
                BLmain = new MyControls.BarsListPanel.BarsListPanel<ColorItemBar, ColorItemBarViewModel> { ItemHeight = 40 };
                {
                    var fs = new List<FieldInfo>(typeof(Color).GetTypeInfo().DeclaredFields);
                    //MyLogger.LogError($"field number={fs.Count}");
                    foreach (var f in fs)
                    {
                        if (f.IsStatic)
                        {
                            var c = (Color)f.GetValue(null);
                            var o = new Tuple<Color, string>(c, $"{f.Name.PadRight(25)}{c}");
                            AllLBs.Add(o);
                            LBs.Add(o);
                        }
                        //MyLogger.LogError($"{f.GetValue(null)}");
                        //MyLogger.LogError($"{f.Name}");
                    }
                }
                GDmain.Children.Add(BLmain, 0, 1);
                MyGrid.SetColumnSpan(BLmain, GDmain.ColumnDefinitions.Count);
            }
            this.Content = GDmain;
        }
        

        private void RegisterEvents()
        {
            var searchAction = new Func<Task>(async () =>
              {
                  LBs.Clear();
                  foreach (var o in AllLBs)
                  {
                      var txt = SWonlyName.IsToggled ? o.Item2.Remove(o.Item2.IndexOf('[')) : o.Item2;
                      if (txt.ToLower().IndexOf((ETsearch.Text ?? "").ToLower()) != -1) LBs.Add(o);
                  }
                  await RefreshLabels();
              });
            SWonlyName.Toggled += async delegate
              {
                  await searchAction();
              };
            ETsearch.Completed +=async delegate
              {
                  await searchAction();
              };
            ETsearch.TextChanged += async delegate
            {
                await searchAction();
            };
            {
                var r = new TapGestureRecognizer
                {
                    NumberOfTapsRequired = 2
                };
                bool sortByName=false;
                r.Tapped += delegate
                {
                    sortByName ^= true;
                    SortLabels(sortByName);
                };
                this.BLmain.GestureRecognizers.Add(r);
            }
        }
        private async void DoAsyncInitializationTasks()
        {
            await RefreshLabels();
        }
        public ColorTestPage()
        {
            InitializeViews();
            RegisterEvents();
            DoAsyncInitializationTasks();
        }
    }
}
