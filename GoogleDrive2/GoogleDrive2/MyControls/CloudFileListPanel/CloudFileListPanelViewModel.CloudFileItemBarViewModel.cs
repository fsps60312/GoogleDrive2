using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Color = Xamarin.Forms.Color;

namespace GoogleDrive2.MyControls.CloudFileListPanel
{
    partial class CloudFileListPanelViewModel
    {
        public class CloudFileItemBarViewModel : BarsListPanel.MyDisposable, INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            private System.Windows.Input.ICommand __Clicked__;
            private string __Text__;
            public System.Windows.Input.ICommand Clicked
            {
                get { return __Clicked__; }
                private set
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
            public Api.Files.FullList.FullProperties File { get; private set; }
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
            public CloudFileItemBarViewModel(Api.Files.FullList.FullProperties file,Func<CloudFileItemBarViewModel, Task>callBack)
            {
                File = file;
                bool isFolder = (File.mimeType == Constants.FolderMimeType);
                this.Text =(isFolder?"📁":"📄")+ File.name;
                this.Clicked = new Xamarin.Forms.Command(async () => { await callBack(this); });
                BackgroundColor = originColor = ( isFolder? Color.LightGoldenrodYellow : Color.GreenYellow);
                focusedColor = Color.FromRgb(originColor.R - 0.1, originColor.G - 0.1, originColor.B - 0.1);
                selectedColor= Color.FromRgb(focusedColor.R - 0.1, focusedColor.G - 0.1, focusedColor.B - 0.1);
            }
        }
    }
}
