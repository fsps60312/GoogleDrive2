using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GoogleDrive2.Api.Files
{
    class ListParameters : ParametersClass
    {
        public class CorporaEnum
        {
            public const string
                user = "user",
                domain = "domain",
                teamDrive = "teamDrive",
                allTeamDrives = "allTeamDrives";
        }
        public class OrderByEnum
        {
            public const string
                createdTime = "createdTime",
                folder = "folder",
                modifiedByMeTime = "modifiedByMeTime",
                modifiedTime = "modifiedTime",
                name = "name",
                quotaBytesUsed = "quotaBytesUsed",
                recency = "recency",
                sharedWithMeTime = "sharedWithMeTime",
                starred = "starred",
                viewedByMeTime = "viewedByMeTime";
            public string Reverse(string s) { return s + " desc"; }
        }
        public class SpacesEnum
        {
            public const string
                drive = "drive",
                appDataFolder = "appDataFolder",
                photos = "photos";
        }
        public System.Collections.Generic.List<string> corpora = new System.Collections.Generic.List<string> { CorporaEnum.user };
        public bool? includeTeamDriveItems = false;
        public System.Collections.Generic.List<string> orderBy = new System.Collections.Generic.List<string>();
        public int? pageSize = 100;
        public string pageToken = null;
        public string q = "";
        public System.Collections.Generic.List<string> spaces = new System.Collections.Generic.List<string> { SpacesEnum.drive };
        public bool? supportsTeamDrives = false;
        public string teamDriveId = null;
    }
    class ListRequest : RequesterP<ListParameters>
    {
        public class ListResponse<T>
        {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
            public string nextPageToken;
            public bool incompleteSearch;
            public System.Collections.Generic.List<T> files;
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
        }
        public ListRequest() : base("GET", "https://www.googleapis.com/drive/v3/files", true) { }
    }
    class List<T> : MyLoggerClass
    {
        public event Libraries.Events.MyEventHandler<string> ErrorOccurred;
        public event Libraries.Events.EmptyEventHandler CloudFileListCleared,OperationStarted,OperationEnded;
        public event Libraries.Events.MyEventHandler<System.Collections.Generic.List<T>> CloudFilesAdded;
        public string Fields
        {
            get { return listRequest.Parameters.fields; }
            set { listRequest.Parameters.fields = value; }
        }
        protected ListRequest listRequest = new ListRequest();
        private bool stopRequest = false;
        public void Stop()
        {
            stopRequest = true;
        }
        private Libraries.MySemaphore semaphore = new Libraries.MySemaphore(1);
        public async Task StartAsync(bool startFromScratch)
        {
            stopRequest = true;
            await semaphore.WaitAsync();
            //await MyLogger.Alert(listRequest.Parameters.q);
            try
            {
                stopRequest = false;
                if (startFromScratch)
                {
                    listRequest.Parameters.pageToken = null;
                    CloudFileListCleared?.Invoke();
                }
                OperationStarted?.Invoke();
                while (true)
                {
                    if (stopRequest) break;
                    using (var response = await listRequest.GetHttpResponseAsync())
                    {
                        if (stopRequest) break;
                        if (response?.StatusCode != HttpStatusCode.OK)
                        {
                            ErrorOccurred?.Invoke(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                            return;
                        }
                        var text = await listRequest.GetResponseTextAsync(response);
                        var result = JsonConvert.DeserializeObject<Api.Files.ListRequest.ListResponse<T>>(text);
                        if (result.incompleteSearch)
                        {
                            MyLogger.LogError($"Incomplete search: {await RestRequests.RestRequester.LogHttpWebResponse(response, true)}\r\n{text}");
                        }
                        CloudFilesAdded?.Invoke(result.files);
                        if (result.nextPageToken == null) break;
                        listRequest.Parameters.pageToken = result.nextPageToken;
                    }
                }
                OperationEnded?.Invoke();
            }
            finally { semaphore.Release(); }
        }
        public List(string q, System.Collections.Generic.List<string> orderBy = null)
        {
            listRequest.Parameters.q = q;
            if (orderBy != null)
            {
                listRequest.Parameters.orderBy = orderBy;
            }
        }
    }
    class FullList : List<FullCloudFileMetadata>
    {
        public FullList(string q, System.Collections.Generic.List<string> orderBy) : base(q, orderBy)
        {
            this.Fields = "kind,nextPageToken,incompleteSearch,files";
        }
    }
}