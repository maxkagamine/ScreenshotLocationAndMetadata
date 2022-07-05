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
using System.IO;
using ExifLibrary;
using ImageSizeReader;
using ImageSizeReader.Model;

namespace ScreenshotLocationAndMetadata
{
    class ScreenshotMetadata
    {
        const string CameraMake = "The Elder Scrolls V";
        const string CameraModel = "Skyrim Special Edition";

        public string PlayerName { get; set; }

        public int PlayerLevel { get; set; }

        public string PlayerRace { get; set; }

        public string LocationName { get; set; }

        public string LocationConsoleCommand { get; set; }

        public float FOV { get; set; }

        public void Save(string imagePath)
        {
            var size = GetImageSize(imagePath);
            var exif = ImageFile.FromFile(imagePath);

            string title = LocationName ?? "";
            string author = $"{PlayerName}, Level {PlayerLevel} {PlayerRace}";
            double focalLength = CalculateFocalLength(FOV, size.Width, size.Height);
            DateTime dateTaken = File.GetCreationTime(imagePath);

            Log.Verbose($"Adding metadata to {Path.GetFileName(imagePath)}:\n" +
                $"  Title: {title}\n" +
                $"  Author: {author}\n" +
                $"  Comment: {LocationConsoleCommand}\n" +
                $"  Focal length: {focalLength} mm (FOV {FOV})\n" +
                $"  Date taken: {dateTaken}\n" +
                $"  Camera: {CameraMake} {CameraModel}");

            // "Title" and "Author" in the Explorer details pane are actually non-standard properties, but after trying
            // different combinations of those, Subject, and ImageDescription, this ended up looking the best.
            exif.Properties.Set(ExifTag.WindowsTitle, title);
            exif.Properties.Set(ExifTag.WindowsAuthor, author);

            // FocalLength is stored as a fraction, which will reverse-calculate (in Hugin etc.) closer to the original
            // FOV than FocalLengthIn35mmFilm, which is an integer, but to use it we also need to indicate the sensor
            // size as being full-frame (36x24mm), which exif does in the roundabout manner of DPI (we'll use px/cm).
            exif.Properties.Set(ExifTag.FocalLength, focalLength);
            exif.Properties.Set(ExifTag.FocalPlaneXResolution, Math.Round(size.Width / 3.6, 2)); // The conversion to Rational does not like repeating decimals
            exif.Properties.Set(ExifTag.FocalPlaneYResolution, Math.Round(size.Height / 2.4, 2));
            exif.Properties.Set(ExifTag.FocalPlaneResolutionUnit, ResolutionUnit.Centimeters);

            exif.Properties.Set(ExifTag.DateTimeOriginal, dateTaken);
            exif.Properties.Set(ExifTag.Make, CameraMake);
            exif.Properties.Set(ExifTag.Model, CameraModel);

            // For some reason Windows doesn't show UserComment when set via this library, but since Hugin appends
            // projection info to UserComment (but ignores Windows properties), it's best to set both
            exif.Properties.Set(ExifTag.UserComment, LocationConsoleCommand ?? "");
            exif.Properties.Set(ExifTag.WindowsComment, LocationConsoleCommand ?? "");

            // Eventually it would be nice to figure out which NPCs are in view of (and close to) the camera
            // exif.Properties.Set(ExifTag.WindowsSubject, "Ysolda; Nazeem");

            // Save the exif data using a temp file since the exif library will leave a 0 byte file behind if it throws
            string tempPath = $"{imagePath}.save";
            try
            {
                exif.Save(tempPath);
                File.Copy(tempPath, imagePath, true);
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private static Size GetImageSize(string imagePath)
        {
            using (var stream = File.OpenRead(imagePath))
            {
                return new ImageSizeReaderUtil().GetDimensions(stream);
            }
        }

        private static double CalculateFocalLength(float hfov, int width, int height)
        {
            // https://sourceforge.net/p/hugin/hugin/ci/490baa16aae6680792d31316be12a75b50236baa/tree/src/hugin_base/panodata/SrcPanoImage.cpp#l863

            double diagonal = Math.Sqrt(36 * 36 + 24 * 24); // 35mm sensor size
            double ratio = (double)width / height;
            double sensorWidth = diagonal / Math.Sqrt(1 + 1 / (ratio * ratio));
            double focalLength = (sensorWidth / 2) / Math.Tan(hfov / 180.0 * Math.PI / 2);
            return Math.Round(focalLength, 2);
        }
    }
}
