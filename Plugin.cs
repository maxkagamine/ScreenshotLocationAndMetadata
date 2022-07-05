// This file is part of Screenshot Location and Metadata.
//
// Screenshot Location and Metadata is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public License as
// published by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
//
// Screenshot Location and Metadata is distributed in the hope that it will be
// useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// Screenshot Location and Metadata. If not, see https://www.gnu.org/licenses/.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using NetScriptFramework.SkyrimSE;

namespace ScreenshotLocationAndMetadata
{
    public class Plugin : NetScriptFramework.Plugin
    {
        private static readonly Regex ScreenshotRegex = new Regex(@"^(enb|ScreenShot).*\.(png|jpg)$");

        private readonly Configuration config = new Configuration();

        public override string Key => nameof(ScreenshotLocationAndMetadata);
        public override string Name => "Screenshot Location and Metadata";
        public override string Author => "Max Kagamine";
        public override int Version => AssemblyVersionToInt();

        protected override bool Initialize(bool loadedAny)
        {
            Log.Name = Name;
            Log.MinimumLogLevel = config.Verbose ? LogLevel.Verbose : LogLevel.Info;

            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var exeDirectory = Path.GetDirectoryName(exePath);
            var watcher = new FileSystemWatcher(exeDirectory);

            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;

            return true;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            // Check if created file is a screenshot
            string filename = Path.GetFileName(e.FullPath);
            if (!ScreenshotRegex.IsMatch(filename))
            {
                return;
            }

            try
            {
                WaitForFile(e.FullPath);
                AddMetadata(MoveScreenshot(e.FullPath));
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error handling screenshot {filename}");
            }
        }

        private string MoveScreenshot(string originalPath)
        {
            if (string.IsNullOrEmpty(config.ScreenshotDirectory))
            {
                return originalPath;
            }

            string filename = Path.GetFileName(originalPath);
            string newPath = Path.Combine(config.ScreenshotDirectory, filename);

            Log.Verbose($"Moving {filename} to {config.ScreenshotDirectory}");

            File.Move(originalPath, newPath);
            return newPath;
        }

        private void AddMetadata(string imagePath)
        {
            if (!config.AddMetadata || MenuManager.Instance.IsMenuOpen("Main Menu"))
            {
                return;
            }

            if (Path.GetExtension(imagePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                Log.Info($"Screenshot format must be set to JPG to add EXIF metadata (set {nameof(config.AddMetadata)} to False to turn off this warning)");
                return;
            }

            var player = PlayerCharacter.Instance;
            var metadata = new ScreenshotMetadata()
            {
                PlayerName = player.BaseActor.Name,
                PlayerLevel = player.Level,
                PlayerRace = player.Race.Name,
                LocationName = MapNameHelper.GetMapName(player.ParentCell),
                LocationConsoleCommand = GetConsoleCommandForCell(player.ParentCell),
                FOV = PlayerCamera.Instance.ThirdPersonFOV, // Apparently FirstPersonFOV is only for the 1st person arms/weapons overlay
            };

            metadata.Save(imagePath);
        }

        /// <summary>
        /// Get a coc (Center on Cell) or cow (Center on World) console command that can be used to return to the
        /// screenshot location (or nearby). Returns null if not possible.
        /// </summary>
        /// <param name="cell">The cell in which the screenshot was taken. May be null.</param>
        private string GetConsoleCommandForCell(TESObjectCELL cell)
        {
            if (!string.IsNullOrEmpty(cell?.EditorId) && cell.EditorId != "Wilderness")
            {
                return $"coc {cell.EditorId}";
            }

            if (!string.IsNullOrEmpty(cell?.WorldSpace?.EditorId))
            {
                return $"cow {cell.WorldSpace.EditorId} {cell.CoordinateX} {cell.CoordinateY}";
            }

            return null;
        }

        /// <summary>
        /// Waits for the file to finish being written. (This will block the thread, but the FileSystemWatcher event
        /// handler should be running on a separate thread anyway.)
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <exception cref="TimeoutException">Timed out while waiting for the file to unlock.</exception>
        private void WaitForFile(string filePath)
        {
            int timeoutSeconds = 10;

            while (true)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        if (stream.Length > 0)
                        {
                            return;
                        }
                    }
                }
                catch (IOException ex)
                {
                    if (timeoutSeconds-- == 0)
                    {
                        throw new TimeoutException("Either the screenshot is taking an exceptionally long time to save, or something is preventing us from accessing the file. Perhaps the inner exception below will be of use.", ex);
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }

        private static int AssemblyVersionToInt()
        {
            var version = typeof(Plugin).Assembly.GetName().Version;
            return version.Major * 100 + version.Minor;
        }
    }
}
