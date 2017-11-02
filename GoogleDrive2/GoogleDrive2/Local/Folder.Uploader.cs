using System.Threading.Tasks;

namespace GoogleDrive2.Local
{
    partial class Folder
    {
        public class Uploader:Api.AdvancedApiOperator
        {
            public Folder F { get; private set; }
            public Uploader(Folder folder)
            {
                F = folder;
            }
            protected override Task StartPrivateAsync(bool startFromScratch)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
