/*
   RegistryHelper
   Copyright (C) 2025 Geir Gustavsen, ZeroLinez Softworx

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace ZLS.RegistryHelper
{
    internal static class Helpers
    {
        /// <summary>
        /// Determines if the current operating system is 64-bit.
        /// </summary>
        public static bool Is64Bit => Environment.Is64BitOperatingSystem;
    }
}
