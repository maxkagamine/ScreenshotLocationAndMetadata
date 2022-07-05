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

namespace ScreenshotLocationAndMetadata
{
    /// <summary>
    /// A simple wrapper for the log file writer.
    /// </summary>
    static class Log
    {
        public static string Name { get; set; }

        public static LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;

        private static void Write(LogLevel level, string message)
        {
            if (level >= MinimumLogLevel)
            {
                NetScriptFramework.Main.Log.AppendLine($"{Name}: {message}");
            }
        }

        public static void Verbose(string message) => Write(LogLevel.Verbose, message);
        public static void Info(string message) => Write(LogLevel.Info, message);
        public static void Error(Exception ex, string message) => Write(LogLevel.Error, $"{message}\n{ex}");
    }

    enum LogLevel
    {
        Verbose,
        Info,
        Error,
    }
}
