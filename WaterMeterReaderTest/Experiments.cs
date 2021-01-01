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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WaterMeterReaderTest
{
    [TestClass]
    public class Experiments
    {
        [TestMethod]
        public void TimeOverflowTest()
        {
            var micros = uint.MaxValue - 8000;
            var nextTime = micros;
            for (var i = 0; i < 200; i++)
            {
                nextTime += 10000;
                micros += 110500;
                var delayTime = (int) (nextTime - micros);
                Console.Write("I:{0}, M:{1}, D:{2}, N:{3}", i, micros, delayTime, nextTime);

                if (delayTime <= 0)
                {
                    nextTime += (uint) (-delayTime / 10000) * 10000;
                    Console.WriteLine("=>" + nextTime);
                }
                else
                {
                    micros += (uint) delayTime + 5;
                    Console.WriteLine();
                }
            }
        }
    }
}
