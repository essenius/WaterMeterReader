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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using WaterMeterReader;

namespace WaterMeterReaderTest
{
    [TestClass]
    public class CrcTest
    {
        [TestMethod]
        public void CrcTest1()
        {
            Assert.AreEqual(19164, Crc.Get("M,2508,0,2510,8568,2517,8016,2522,8360,2515,8368,2517,8388,2529,8364,2534,8356,2520,8368,2536,8388"));
        }
    }
}
