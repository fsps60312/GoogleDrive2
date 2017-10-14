using System;
using System.Collections.Generic;
using System.Text;
using GoogleDrive2.MyControls;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Pages.TestPage
{
    class SearchFileListPage:MyContentPage
    {
        MyGrid GDmain;
        MyEntry ETq;
        MyControls.CloudFileListPanel.CloudFileListPanel.CloudFolderSearchList CFSmain;
        async Task ItemClicked(MyControls.CloudFileListPanel.CloudFileListPanelViewModel.CloudFileItemBarViewModel f)
        {
            await MyLogger.Alert(JsonConvert.SerializeObject(f.File));
            ChangeKeyWord($"'{f.File.id}' in parents");
        }
        private void InitializeViews()
        {
            GDmain = new MyGrid();
            GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Auto) });
            GDmain.RowDefinitions.Add(new Xamarin.Forms.RowDefinition { Height = new Xamarin.Forms.GridLength(1, Xamarin.Forms.GridUnitType.Star) });
            {
                ETq = new MyEntry();
                GDmain.Children.Add(ETq, 0, 0);
            }
            {
                CFSmain = new MyControls.CloudFileListPanel.CloudFileListPanel.CloudFolderSearchList("'root' in parents", new List<string>());
                CFSmain.ItemClicked += async (f) => { await ItemClicked(f); };
                GDmain.Children.Add(CFSmain, 0, 1);
            }
            this.Content = GDmain;
        }
        private void ChangeKeyWord(string q)
        {
            CFSmain.Stop();
            GDmain.Children.Remove(CFSmain);
            CFSmain = new MyControls.CloudFileListPanel.CloudFileListPanel.CloudFolderSearchList(q, new List<string>());
            CFSmain.ItemClicked += async (f) => { await ItemClicked(f); };
            GDmain.Children.Add(CFSmain, 0, 1);
        }
        private void RegisterEvents()
        {
            ETq.Completed += (o, args) =>
              {
                  ChangeKeyWord(ETq.Text);
              };
        }
        public SearchFileListPage()
        {
            this.Title = "Search Files";
            InitializeViews();
            RegisterEvents();
        }
    }
}
