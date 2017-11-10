using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDrive2
{
    static class Constants
    {
        public static class Icons
        {
            public const string
                Hourglass = "⏳",
                File = "📄",
                Folder = "📁",
                Refresh = "↻",
                Warning = "⚠",
                Mushroom = "🍄",
                Info = "ℹ",
                CheckBox = "🗹",
                SelectedCheckBox = "🗷",
                Upload = "⭱",
                TrashCan = "🗑",
                Star = "🌟",
                Clear = "⎚",
                Completed = "✔",
                SubtaskCompleted = Completed + Hourglass,
                Pause = "⏸",
                Pausing = Hourglass + Pause,
                Play = "▶",
                Initial = Play + Hourglass,
                Size = "⋙",
                Magnifier = "🔍",
                Timers = "🕐🕑🕒🕓🕔🕕🕖🕗🕘🕙🕚🕛";
        }
        public const string FolderMimeType = "application/vnd.google-apps.folder";
    }
}
