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

using NetScriptFramework.Tools;

namespace ScreenshotLocationAndMetadata
{
    class Configuration
    {
        private readonly ConfigFile config = new ConfigFile(nameof(ScreenshotLocationAndMetadata));

        public Configuration()
        {
            config.AddSetting(nameof(ScreenshotDirectory), new Value(""), description: "If set, screenshots will be moved to this folder.");
            config.AddSetting(nameof(AddMetadata), new Value(true), description: "Whether to add EXIF metadata to screenshots.\nFormat must be set to JPG (set ScreenshotFormat=2 under [FILE] in enblocal.ini)");
            config.AddSetting(nameof(Verbose), new Value(false), description: "Enable additional logging. Default is to log only errors.");

            if (!config.Load())
            {
                config.Save();
            }
        }

        public string ScreenshotDirectory => config.GetValue(nameof(ScreenshotDirectory))?.ToString();

        public bool AddMetadata => config.GetValue(nameof(AddMetadata))?.ToBoolean() ?? true;

        public bool Verbose => config.GetValue(nameof(Verbose))?.ToBoolean() ?? false;
    }
}
