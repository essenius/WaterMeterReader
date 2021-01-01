// Copyright 2021 Rik Essenius
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
//   except in compliance with the License. You may obtain a copy of the License at
// 
//       http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software distributed under the License
//    is distributed on an "AS IS" BASIS WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and limitations under the License.

namespace WaterMeterReader
{
    internal class Crc
    {
        private static int CalcCrc(int crc, int character)
        {
            crc ^= character;
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
            return crc;
        }

        public static int Get(string input)
        {
            var crc = 0;
            foreach (var character in input)
            {
                crc = CalcCrc(crc, character);
            }
            return crc;
        }
    }
}
