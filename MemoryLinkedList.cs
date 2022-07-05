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
using System.Collections;
using System.Collections.Generic;
using NetScriptFramework;

namespace ScreenshotLocationAndMetadata
{
    /// <summary>
    /// Provides a simple way of enumerating linked lists in memory where the game library only provides an address. In
    /// memory the linked list is stored as two bytes: the pointer to the value and a pointer to the next node. An empty
    /// list may consist of a single node with an empty value.
    /// </summary>
    /// <typeparam name="TValue">The data type of the values in the list.</typeparam>
    class MemoryLinkedList<TValue> : IEnumerable<TValue> where TValue : IMemoryObject
    {
        private MemoryLinkedList(IntPtr address)
        {
            Address = address;
        }

        public static MemoryLinkedList<TValue> FromAddress(IntPtr address) => new MemoryLinkedList<TValue>(address);

        public IntPtr Address { get; }

        public IEnumerator<TValue> GetEnumerator()
        {
            IntPtr currentNodePtr = Address;

            while (currentNodePtr != IntPtr.Zero)
            {
                var valuePtr = Memory.ReadPointer(currentNodePtr);
                var nextNodePtr = Memory.ReadPointer(currentNodePtr + 8);

                if (valuePtr != IntPtr.Zero)
                {
                    yield return MemoryObject.FromAddress<TValue>(valuePtr);
                }

                currentNodePtr = nextNodePtr;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
