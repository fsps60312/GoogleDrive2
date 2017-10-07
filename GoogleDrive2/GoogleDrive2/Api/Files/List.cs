using System;
using System.Collections.Generic;
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
        public bool includeTeamDriveItems = false;
        public System.Collections.Generic.List<string> orderBy = new System.Collections.Generic.List<string>();
        public int pageSize = 100;
        public string pageToken = null;
        public string q = "";
        public System.Collections.Generic.List<string> spaces = new System.Collections.Generic.List<string> { SpacesEnum.drive };
        public bool supportsTeamDrives = false;
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
        ListRequest listRequest = new ListRequest();
        public async Task StartAsync(bool startFromScratch)
        {
            if (startFromScratch)
            {
                listRequest.Parameters.pageToken = null;
                CloudFileListCleared?.Invoke();
            }
            OperationStarted?.Invoke();
            while (true)
            {
                var response = await listRequest.GetHttpResponseAsync();
                if (response?.StatusCode != HttpStatusCode.OK)
                {
                    ErrorOccurred?.Invoke(await RestRequests.RestRequester.LogHttpWebResponse(response, true));
                    return;
                }
                var text = await listRequest.GetResponseTextAsync(response);
                var result = JsonConvert.DeserializeObject<Api.Files.ListRequest.ListResponse<T>>(text);
                if (result.incompleteSearch)
                {
                    MyLogger.LogError($"Incomplete search: {await RestRequests.RestRequester.LogHttpWebResponse(response, false)}\r\n{text}");
                }
                CloudFilesAdded?.Invoke(result.files);
                if (result.nextPageToken == null) break;
                listRequest.Parameters.pageToken = result.nextPageToken;
            }
            OperationEnded?.Invoke();
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
    class FullList : List<FullList.FullProperties>
    {
        public class FullProperties
        {
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
            public const string kind= "drive#file";
            public string id, name, mimeType, description;
            public bool starred, trashed, explicitlyTrashed;
            public class UserClass
            {
                public const string kind = "drive#user";
                public string displayName, photoLink;
                public bool me;
                public string permissionId, emailAddress;
            }
            public UserClass trashingUser;
            public DateTime trashedTime;
            public System.Collections.Generic.List<string> parents;
            public Dictionary<string, string> properties, appProperties;
            public enum SpacesEnum {drive, appDataFolder, photos };
            public System.Collections.Generic.List<SpacesEnum> spaces;
            public long version;
            public string webContentLink, webViewLink, iconLink;
            public bool hasThumbnail;
            public string thumbnailLink;
            public long thumbnailVersion;
            public bool viewedByMe;
            public DateTime viewedByMeTime, createdTime, modifiedTime, modifiedByMeTime;
            public bool modifiedByMe;
            public DateTime sharedWithMeTime;
            public UserClass sharingUser;
            public System.Collections.Generic.List<UserClass> owners;
            public string teamDriveId;
            public UserClass lastModifyingUser;
            public bool shared, ownedByMe;
            public class CapabilitiesClass
            {
                public bool canAddChildren, canChangeViewersCanCopyContent, canComment, canCopy, canDelete, canDownload, canEdit, canListChildren, canMoveItemIntoTeamDrive, canMoveTeamDriveItem, canReadRevisions, canReadTeamDrive, canRemoveChildren, canRename, canShare, canTrash, canUntrash;
            }
            public CapabilitiesClass capabilities;
            public bool viewersCanCopyContent, writersCanShare;
            public class PermissionsClass
            {
                public string kind = "drive#permission";
                public string id, type, emailAddress, domain, role;
                public bool allowFileDiscovery;
                public string displayName, photoLink;
                public DateTime expirationTime;
                public class TeamDrivePermissionDetailsClass
                {
                    public string teamDrivePermissionType, role, inheritedFrom;
                    public bool inherited;
                }
                public System.Collections.Generic.List<TeamDrivePermissionDetailsClass> teamDrivePermissionDetails;
                public bool deleted;
            }
            public System.Collections.Generic.List<PermissionsClass> permissions;
            public bool hasAugmentedPermissions;
            public string folderColorRgb, originalFilename, fullFileExtension, fileExtension, md5Checksum;
            public long size, quotaBytesUsed;
            public string headRevisionId;
            public class ContentHintsClass
            {
                public class ThumbnailClass
                {
                    public byte[] image;
                    public string mimeType;
                }
                public string indexableText;
            }
            public ContentHintsClass contentHints;
            public class ImageMediaMetadataClass
            {
                public int width, height, rotation;
                public class LocationClass
                {
                    public double latitude, longitude, altitude;
                }
                public LocationClass location;
                public string time, cameraMake, cameraModel;
                public float exposureTime, aperture;
                public bool flashUsed;
                public float focalLength;
                public int isoSpeed;
                public string meteringMode, sensor, exposureMode, colorSpace, whiteBalance;
                public float exposureBias, maxApertureValue;
                public int subjectDistance;
                public string lens;
            }
            public ImageMediaMetadataClass imageMediaMetadata;
            public class VideoMediaMetadataClass
            {
                public int width, height;
                public long durationMillis;
            }
            public VideoMediaMetadataClass videoMediaMetadata;
            public bool isAppAuthorized;
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
        }
        public FullList(string q, System.Collections.Generic.List<string> orderBy) : base(q, orderBy)
        {
            this.Fields = "kind,nextPageToken,incompleteSearch,files";
        }
    }
}