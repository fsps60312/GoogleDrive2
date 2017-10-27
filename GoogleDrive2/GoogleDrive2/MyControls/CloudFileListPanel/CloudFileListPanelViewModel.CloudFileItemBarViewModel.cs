using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Color = Xamarin.Forms.Color;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanelViewModel
    {
        public class CloudFileItemBarViewModel : BarsListPanel.MyDisposable
        {
            private System.Windows.Input.ICommand __Clicked__;
            private string __Text__;
            public System.Windows.Input.ICommand Clicked
            {
                get { return __Clicked__; }
                set
                {
                    if (__Clicked__ == value) return;
                    __Clicked__ = value;
                    OnPropertyChanged("Clicked");
                }
            }
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
            public Api.Files.FullCloudFileMetadata File { get; private set; }
            Color originColor,focusedColor,selectedColor;
            private bool __Selected__ = false;
            public bool Selected
            {
                get { return __Selected__; }
                set
                {
                    if (__Selected__ == value) return;
                    __Selected__ = value;
                    UpdateBackgroundColor();
                }
            }
            private bool __Focused__ = false;
            public bool Focused
            {
                get { return __Focused__; }
                set
                {
                    if (__Focused__ == value) return;
                    __Focused__ = value;
                    UpdateBackgroundColor();
                }
            }
            private bool __IsToggled__ = false;
            public bool IsToggled
            {
                get { return __IsToggled__; }
                set
                {
                    if (__IsToggled__ == value) return;
                    __IsToggled__ = value;
                    BorderColor = __IsToggled__ ? Color.Black : Color.Transparent;
                    Toggled?.Invoke(this);
                }
            }
            private double __Opacity__ = 1;
            public double Opacity
            {
                get { return __Opacity__; }
                set
                {
                    if (__Opacity__ == value) return;
                    __Opacity__ = value;
                    OnPropertyChanged("Opacity");
                }
            }
            bool __UnderVerification__ = false;
            public bool UnderVerification
            {
                get { return __UnderVerification__; }
                set
                {
                    if (__UnderVerification__ == value) return;
                    __UnderVerification__ = value;
                    Opacity = value ? 0.5 : 1.0;
                }
            }
            public event Libraries.Events.MyEventHandler<CloudFileItemBarViewModel> Toggled;
            private Color __BorderColor__ = Color.Transparent;
            public Color BorderColor
            {
                get { return __BorderColor__; }
                set
                {
                    if (__BorderColor__ == value) return;
                    __BorderColor__ = value;
                    OnPropertyChanged("BorderColor");
                }
            }
            void UpdateBackgroundColor()
            {
                if (Selected) BackgroundColor = selectedColor;
                else if (Focused) BackgroundColor = focusedColor;
                else BackgroundColor = originColor;
            }
            public void Initialize(Api.Files.FullCloudFileMetadata file)
            {
                File = file;
                bool isFolder = (File.mimeType == Constants.FolderMimeType);
                this.Text = (File.starred.Value ? Constants.Icons.Star : "") + (isFolder ? Constants.Icons.Folder : Constants.Icons.File) + File.name;
                BackgroundColor = originColor = (isFolder ? Color.LightGoldenrodYellow : Color.GreenYellow);
                focusedColor = Color.FromRgb(originColor.R - 0.1, originColor.G - 0.1, originColor.B - 0.1);
                selectedColor = Color.FromRgb(focusedColor.R - 0.1, focusedColor.G - 0.1, focusedColor.B - 0.1);
            }
            public CloudFileItemBarViewModel(Api.Files.FullCloudFileMetadata file)
            {
                Initialize(file);
            }
        }
    }
}
