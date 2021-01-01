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
    public class MeasurementReaderTest
    {
        [TestMethod]
        public void MeasurementReaderSingleLineInputTest()
        {
            const string input =
                "2020-12-27T23:40:29.571091,M,M0,W0,M1,W1,M2,W2,M3,W3,M4,W4,M5,W5,M6,W6,M7,W7,M8,W8,M9,W9,CRC,0\n" +
                "2020-12-27T23:40:29.676928,M,2506,0,2496,8572,2503,8028,2506,8356,2506,8360,2508,8356,2501,8344,2503,8372,2491,8372,2498,8388,61392,1\n";
            var stream = new StringStream(input);
            var measureReader = new MeasurementReader(stream.Reader);
            Assert.IsTrue(measureReader.HasNext(), "before asking for a measurement, HasNext is true");
            var measurement = measureReader.NextMeasurement();
            Assert.IsTrue(measureReader.CrcOk, "CRC is OK");
            Assert.AreEqual(DateTime.Parse("2020-12-27T23:40:29.676928"), measurement.Timestamp, "Date parsed correctly");
            Assert.AreEqual(2506, measurement.Value, "Correct value selected for #1");
            Assert.AreEqual(0, measurement.Delay, "Correct delay selected for #1");
            measurement = measureReader.NextMeasurement();
            Assert.AreEqual(DateTime.Parse("2020-12-27T23:40:29.677028"), measurement.Timestamp, "TImestamp calculated correctly");
            Assert.AreEqual(2496, measurement.Value, "Correct value selected for #2");
            Assert.AreEqual(8572, measurement.Delay, "Correct ValueTuple selected for #2");
            var measurementCount = 0;
            while (measureReader.HasNext())
            {
                measurement = measureReader.NextMeasurement();
                measurementCount++;
            }
            Assert.AreEqual(8, measurementCount, "8 more data points read");
            Assert.AreEqual(DateTime.Parse("2020-12-27T23:40:29.677828"), measurement.Timestamp, "Last timestamp correctly calculated");
            Assert.AreEqual(2498, measurement.Value, "Last value correctly selected");
            Assert.AreEqual(8388, measurement.Delay, "Last delay correctly selected");
        }
    }
}
