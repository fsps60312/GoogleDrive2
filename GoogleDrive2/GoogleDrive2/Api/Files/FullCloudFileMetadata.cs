using System;
using System.Collections.Generic;

namespace GoogleDrive2.Api.Files
{
    public partial class FullCloudFileMetadata
    {
        public Trasher GetTrasher(bool trashed) { return new Trasher(this.id, trashed); }
        public MetadataUpdater GetMetadataUpdater(FullCloudFileMetadata metadata) { return new MetadataUpdater(this.id, metadata); }
#pragma warning disable 0649 // Fields are assigned to by JSON deserialization
        public const string kind = "drive#file";
        public string id, name, mimeType, description;
        public bool? starred, trashed, explicitlyTrashed;
        public class UserClass
        {
            public const string kind = "drive#user";
            public string displayName, photoLink;
            public bool? me;
            public string permissionId, emailAddress;
        }
        public UserClass trashingUser;
        public DateTime? trashedTime;
        public System.Collections.Generic.List<string> parents;
        public Dictionary<string, string> properties, appProperties;
        public enum SpacesEnum { drive, appDataFolder, photos };
        public System.Collections.Generic.List<SpacesEnum> spaces;
        public long? version;
        public string webContentLink, webViewLink, iconLink;
        public bool? hasThumbnail;
        public string thumbnailLink;
        public long? thumbnailVersion;
        public bool? viewedByMe;
        public DateTime? viewedByMeTime, createdTime, modifiedTime, modifiedByMeTime;
        public bool? modifiedByMe;
        public DateTime? sharedWithMeTime;
        public UserClass sharingUser;
        public System.Collections.Generic.List<UserClass> owners;
        public string teamDriveId;
        public UserClass lastModifyingUser;
        public bool? shared, ownedByMe;
        public class CapabilitiesClass
        {
            public bool? canAddChildren, canChangeViewersCanCopyContent, canComment, canCopy, canDelete, canDownload, canEdit, canListChildren, canMoveItemIntoTeamDrive, canMoveTeamDriveItem, canReadRevisions, canReadTeamDrive, canRemoveChildren, canRename, canShare, canTrash, canUntrash;
        }
        public CapabilitiesClass capabilities;
        public bool? viewersCanCopyContent, writersCanShare;
        public class PermissionsClass
        {
            public string kind = "drive#permission";
            public string id, type, emailAddress, domain, role;
            public bool? allowFileDiscovery;
            public string displayName, photoLink;
            public DateTime? expirationTime;
            public class TeamDrivePermissionDetailsClass
            {
                public string teamDrivePermissionType, role, inheritedFrom;
                public bool? inherited;
            }
            public System.Collections.Generic.List<TeamDrivePermissionDetailsClass> teamDrivePermissionDetails;
            public bool? deleted;
        }
        public System.Collections.Generic.List<PermissionsClass> permissions;
        public bool? hasAugmentedPermissions;
        public string folderColorRgb, originalFilename, fullFileExtension, fileExtension, md5Checksum;
        public long? size, quotaBytesUsed;
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
            public int? width, height, rotation;
            public class LocationClass
            {
                public double? latitude, longitude, altitude;
            }
            public LocationClass location;
            public string time;
            public string cameraMake, cameraModel;
            public float? exposureTime, aperture;
            public bool? flashUsed;
            public float? focalLength;
            public int? isoSpeed;
            public string meteringMode, sensor, exposureMode, colorSpace, whiteBalance;
            public float? exposureBias, maxApertureValue;
            public int? subjectDistance;
            public string lens;
        }
        public ImageMediaMetadataClass imageMediaMetadata;
        public class VideoMediaMetadataClass
        {
            public int? width, height;
            public long? durationMillis;
        }
        public VideoMediaMetadataClass videoMediaMetadata;
        public bool? isAppAuthorized;
#pragma warning restore 0649 // Fields are assigned to by JSON deserialization
    }
}