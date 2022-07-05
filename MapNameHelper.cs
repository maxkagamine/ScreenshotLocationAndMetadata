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

using System.Linq;
using NetScriptFramework.SkyrimSE;

namespace ScreenshotLocationAndMetadata
{
    static class MapNameHelper
    {
        /// <summary>
        /// Attempts to get the cell name as displayed on the map, load doors, and save games. For interior cells, this
        /// is the cell name itself. For exterior worldspace cells, the game seems to look at all of the cell's Regions
        /// for the Region Data Entry of type Map with the highest Priority and uses its Map Name. The Override flag
        /// seems to be irrelevant. The order of Regions also does not matter (xEdit simply sorts them by form id).
        ///
        /// <para />
        ///
        /// For most places this ends up being BorderRegionSkyrim which has a Map entry named "Skyrim". With Unique
        /// Region Names installed, hold-specific Regions are added to every worldspace cell that override the former
        /// with names like "Haafingar" or "Whiterun Hold".
        ///
        /// <para />
        ///
        /// Note that this is different from using CurrentLocation, as that refers to the Location record which serves
        /// other purposes. As an example, if that were used, the Winterhold "Hall of Countenance" would become
        /// "Dormitory" (that being the name of its LCTN record). On the other hand, KatlasFarmExterior (the Solitude
        /// stables) has the Location "Katla's Farm," while a save game in that cell, and the door leaving the stables
        /// building, simply says "Haafingar" (or "Skyrim").
        /// </summary>
        /// <remarks>
        /// The local map appears to always show the worldspace name when outside (always "Skyrim"), not the region name
        /// even with Unique Region Names. For outside saves to use the region name, Regional Save Names is required, as
        /// apparently the naming behavior was changed in SSE.
        /// </remarks>
        /// <param name="cell">The cell for which to get the appropriate map name.</param>
        public static string GetMapName(TESObjectCELL cell)
        {
            if (cell is null)
            {
                return null;
            }

            // For interior cells, the name used is simply the cell name
            if (cell.IsInterior)
            {
                return cell.Name;
            }

            // Look for the cell record's list of Regions
            TESRegionList regionList = FindRegionList(cell.ExtraDataList);
            if (regionList is null)
            {
                return cell.Name;
            }

            // Find the Map data with the highest priority
            TESRegionDataMap mapData = regionList
                .SelectMany(r => MemoryLinkedList<TESRegionData>.FromAddress(r.DataList))
                .OfType<TESRegionDataMap>()
                .OrderBy(d => d.Header.Priority)
                .LastOrDefault();

            // Return its map name, or fall back again to cell name if there are no regions with map data
            return mapData?.MapName.Text ?? cell.Name;
        }

        /// <summary>
        /// Searches the ExtraDataList for the list of Regions. For some reason this isn't just a property on
        /// TESObjectCell but hidden within a linked list of various data types which we need to traverse by hand.
        /// </summary>
        /// <param name="linkedList">The <see cref="TESObjectCELL.ExtraDataList"/>.</param>
        /// <returns>The <see cref="TESRegionList"/>, or null if none was found.</returns>
        private static TESRegionList FindRegionList(BSExtraDataList linkedList)
        {
            var item = linkedList.First;

            while (!(item is null))
            {
                if (item is ExtraRegionList regionListItem)
                {
                    return regionListItem.RegionList;
                }

                item = item.Next;
            }

            return null;
        }
    }
}
