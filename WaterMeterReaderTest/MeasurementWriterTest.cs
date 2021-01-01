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
using WaterMeterReader;

namespace WaterMeterReaderTest
{
    [TestClass]
    public class MeasurementWriterTest
    {
        [TestMethod]
        public void MeasurementWriterHeaderAndFlushRateTest()
        {
            var stream = new StringStream();
            var writer = new MeasurementWriter(stream.Writer);
            Assert.AreEqual(10u, writer.FlushRate, "Default flush rate is 10");
            writer.DesiredFlushRate = 2;
            Assert.AreEqual(2u, writer.FlushRate, "Changing desired flush rate before printing in a line changes FlushRate");
            writer.WriteHeader();
            Assert.AreEqual("M,M0,W0,M1,W1,CRC\r\n", stream.Content(), "Header has 2 data points");
            writer.Write(new Measurement(DateTime.Now, 2400, 6000));
            writer.DesiredFlushRate = 0;
            Assert.AreEqual(2u, writer.FlushRate, "Changing desired flush rate while in a line doesn't immediately change FlushRate");
            writer.Write(new Measurement(DateTime.Now, 2375, 6200));
            Assert.AreEqual("M,M0,W0,M1,W1,CRC\r\nM,2400,6000,2375,6200,46910\r\n", stream.Content(), "Flush happens after 2 data points");
            Assert.AreEqual(0u, writer.FlushRate, "The desired flush rate gets applied now");
            // This measurement should get ignored
            writer.Write(new Measurement(DateTime.Now, 2450, 5800));
            writer.DesiredFlushRate = 1;
            Assert.AreEqual(1u, writer.FlushRate, "When not logging, FlushRrate gets applied immediately");
            writer.Write(new Measurement(DateTime.Now, 2425, 6400));
            Assert.AreEqual("M,M0,W0,M1,W1,CRC\r\nM,2400,6000,2375,6200,46910\r\nM,2425,6400,19517\r\n", stream.Content(),
                "data point sent while FlushRate was 0 is not included");
        }

        [TestMethod]
        public void MeasurementWriterSimpleTest()
        {
            var stream = new StringStream();
            var writer = new MeasurementWriter(stream.Writer) {DesiredFlushRate = 5};
            for (var i = 0; i < 6; i++)
            {
                var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, i), 2600 + 5 * i, 3000 + 1000 * i);
                Assert.AreEqual(i == 4, writer.Write(measurement), "Flush happens after 5 data points");
                if (i == 5)
                {
                    writer.Flush();
                }
                if (i < 4)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(stream.Content()), "Nothing written the first 4 data points");
                }
                else if (i == 4)
                {
                    Assert.AreEqual("M,2600,3000,2605,4000,2610,5000,2615,6000,2620,7000,46194\r\n", stream.Content(),
                        "The first flush contains 5 data points");
                }
                else
                {
                    Assert.AreEqual("M,2600,3000,2605,4000,2610,5000,2615,6000,2620,7000,46194\r\nM,2625,8000,48383\r\n", stream.Content(),
                        "After an explicit flush, data gets written immediately.");
                }
            }
        }
    }
}
