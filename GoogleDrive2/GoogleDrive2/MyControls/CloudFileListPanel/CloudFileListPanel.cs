using System.Text;
using GoogleDrive2.MyControls;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanel:MyUnwipableView
    {
        MyStackPanel SPmain;
        MyLabel BVpadding;
        private void InitializeViews()
        {
            SPmain = new MyStackPanel(Xamarin.Forms.ScrollOrientation.Horizontal);
            {
                BVpadding = new MyLabel
                {
                    WidthRequest = 3000// this.Width;
                };
                SPmain.Children.Add(BVpadding);
            }
            this.Content = SPmain;
        }
        private void RegisterEvents()
        {
            //this.SizeChanged += delegate
            //{
            //    BVpadding.WidthRequest = this.Width;
            //    MyLogger.LogError(this.Width.ToString());
            //};
        }
        private void AddList(ChainedSearchList list)
        {
            SPmain.Children.Remove(BVpadding);
            list.ListAdded += async(l) =>
            {
                AddList(l);
                //if (l.leftChild != null)
                {
                    await Task.Delay(100);
                    try
                    {
                        await SPmain.ScrollToAsync(l, Xamarin.Forms.ScrollToPosition.Center, true);
                    }
                    catch(System.ArgumentException error)
                    {
                        if (error.Message != "element does not belong to this ScrollVIew\r\nParameter name: element") throw error;
                    }
                }
            };
            list.ListRemoved += (l) => { SPmain.Children.Remove(l); };
            SPmain.Children.Add(list);
            SPmain.Children.Add(BVpadding);
        }
        public CloudFileListPanel()
        {
            InitializeViews();
            AddList(new ChainedSearchList("'root' in parents and not trashed", new System.Collections.Generic.List<string>(), new System.Func<Api.Files.FullList.FullProperties, System.Threading.Tasks.Task<System.Tuple<string, System.Collections.Generic.List<string>>>>((f) =>
            {
                return Task.FromResult(new System.Tuple<string, System.Collections.Generic.List<string>>($"'{f.id}' in parents and not trashed", new System.Collections.Generic.List<string>()));
            })));
        }
    }
}
