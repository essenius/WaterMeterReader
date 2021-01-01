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
    public class SummaryWriterTest
    {
        [TestMethod]
        public void SummaryWriterExcludeAllTest()
        {
            // ExcludeAll becomes true in the third sample. That should put excluded on 3. It should finish the idle round and then start a non-idle one.
            // Then we put WaitSamples on 1 to get WaitCount filled. 
            // We do this all before sample 5 so the non idle rate kicks in early.
            var stream = new StringStream();
            var summaryWriter = new SummaryWriter(stream.Writer) {IdleFlushRate = 10, NonIdleFlushRate = 5};
            for (var i = 0; i < 15; i++)
            {
                var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, i), 2400 + (i > 7 ? 200 : 0), i == 0 ? 2500 : 7500 + i * 5);

                var result = new FlowMeter
                {
                    Amplitude = i == 0 ? 10000 : 2000, HighPass = 0, LowPassFast = 2400 - i, LowPassSlow = 2385 + i, Exclude = i == 2 || i == 3,
                    ExcludeAll = i == 2, Flow = false, LowPassOnHighPass = 10 + i, Outlier = i == 2, Drift = false
                };
                summaryWriter.AddMeasurement(measurement, result);
                summaryWriter.PrepareWrite();
                Assert.AreEqual(i < 2 || i > 4 ? 10u : 5u, summaryWriter.FlushRate,
                    "Flush rate is 10 the first two data points, and after the 5th. In between it is 5");
                summaryWriter.Write();
                if (i < 4)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(stream.Content()), "Nothing written before the 5th data point");
                }
                else if (i < 14)
                {
                    //"S,Measure,Flows,SumAmplitude,SumLPonHP,LowPassFast,LPonHP,Outliers,Waits,Drifts,AvgDelay,CRC"
                    Assert.AreEqual("S,5,0,0,0,2396,2389,14,1,0,4,6510,17220\r\n", stream.Content(), "The right data written after 5 points");
                }
                else
                {
                    Assert.AreEqual("S,5,0,0,0,2396,2389,14,1,0,4,6510,17220\r\nS,10,0,0,0,2386,2399,24,0,0,0,7548,8484\r\n", stream.Content(),
                        "Without special events, the next flush is after 10 data points");
                }
            }
        }

        [TestMethod]
        public void SummaryWriterFlowTest()
        {
            var stream = new StringStream();
            var summaryWriter = new SummaryWriter(stream.Writer) {IdleFlushRate = 10, NonIdleFlushRate = 5};
            for (var i = 0; i < 10; i++)
            {
                var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, i), 2400 + i, 8000 + i);
                var result = new FlowMeter
                {
                    Amplitude = 5 + (i > 7 ? 10 : 0), HighPass = 0, LowPassFast = 2400, LowPassSlow = 2398, Exclude = false, ExcludeAll = false,
                    Flow = i > 7,
                    LowPassOnHighPass = i, Outlier = false, Drift = false
                };
                summaryWriter.AddMeasurement(measurement, result);
                summaryWriter.PrepareWrite();
                summaryWriter.Write();
                if (i < 9)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(stream.Content()), "Nothing written the first 9 data points");
                }
                else
                {
                    //"S,Measure,Flows,SumAmplitude,SumLPonHP,LowPassFast,LowPassSlow,LPonHP,Outliers,Waits,Excludes,AvgDelay,CRC"
                    Assert.AreEqual("S,10,2,30,17,2400,2398,9,0,0,0,8005,56358\r\n", stream.Content(),
                        "After 10 points, we get a summary with 2 flows, and SumAmplitude and SumLPonP are populated");
                }
            }
            // Flush should not change anything as we have no more data.
            summaryWriter.Flush();
            Assert.AreEqual("S,10,2,30,17,2400,2398,9,0,0,0,8005,56358\r\n", stream.Content(),
                "After 10 points, we get a summary with 2 flows, and SumAmplitude and SumLPonP are populated");
        }

        [TestMethod]
        public void SummaryWriterFlushRate1Test()
        {
            var stream = new StringStream();
            var summaryWriter = new SummaryWriter(stream.Writer) {IdleFlushRate = 1, NonIdleFlushRate = 1};
            var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, 1), 2400, 8000);
            var result = new FlowMeter
            {
                Amplitude = 0,
                HighPass = 0,
                LowPassFast = 2400,
                LowPassSlow = 2402,
                Exclude = false,
                ExcludeAll = false,
                Flow = false,
                LowPassOnHighPass = 5,
                Outlier = false,
                Drift = false
            };
            summaryWriter.AddMeasurement(measurement, result);
            summaryWriter.PrepareWrite();
            summaryWriter.Write();
            Assert.AreEqual("S,2400,0,0,0,2400,2402,5,0,0,0,8000,35625\r\n", stream.Content(), "single measurement written with sample value");
        }

        [TestMethod]
        public void SummaryWriterFlushTest()
        {
            var stream = new StringStream();
            var summaryWriter = new SummaryWriter(stream.Writer) {IdleFlushRate = 10, NonIdleFlushRate = 5};
            for (var i = 0; i < 3; i++)
            {
                var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, i), 2400 + i, 8000 + i);
                var result = new FlowMeter
                {
                    Amplitude = i,
                    HighPass = 0,
                    LowPassFast = 2400,
                    LowPassSlow = 2402,
                    Exclude = false,
                    ExcludeAll = false,
                    Flow = false,
                    LowPassOnHighPass = 5 - i,
                    Outlier = false,
                    Drift = false
                };
                summaryWriter.AddMeasurement(measurement, result);
                summaryWriter.PrepareWrite();
                summaryWriter.Write();
                Assert.IsTrue(string.IsNullOrEmpty(stream.Content()), "nothing written yet");
            }
            summaryWriter.Flush();
            Assert.AreEqual("S,3,0,0,0,2400,2402,3,0,0,0,8001,50790\r\n", stream.Content(), "flush worked");
        }

        [TestMethod]
        public void SummaryWriterIdleOutlierTest()
        {
            // we insert outliers after 8 and 9. It should finish the idle round and then start a non-idle one.
            var stream = new StringStream();
            var summaryWriter = new SummaryWriter(stream.Writer) {IdleFlushRate = 10, NonIdleFlushRate = 5};
            for (var i = 0; i < 15; i++)
            {
                var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, i), 2400 + (i > 7 ? 200 : 0), i == 0 ? 2500 : 7993 + i);
                var result = new FlowMeter
                {
                    Amplitude = 5 + (i > 7 ? 200 : 0), HighPass = 0, LowPassFast = 2400 - i, LowPassSlow = 2385 + i,
                    Exclude = i > 7, ExcludeAll = false, Flow = false,
                    LowPassOnHighPass = i, Outlier = i > 7, Drift = i > 9
                };
                summaryWriter.AddMeasurement(measurement, result);
                summaryWriter.PrepareWrite();
                Assert.AreEqual(i < 8 ? 10u : 5u, summaryWriter.FlushRate,
                    " The first 8 data points FlushRate is 10, then it becomes 5 because of outliers (round {0})", i);
                if (i != 9)
                {
                    // force skipping the write at the time it was supposed to, so it gets done next time.
                    summaryWriter.Write();
                }

                if (i <= 9)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(stream.Content()), "Nothing written the first 9 points");
                }
                else if (i < 14)
                {
                    //"S,Measure,Flows,SumAmplitude,SumLPonHP,LowPassFast,LPonHP,Outliers,Waits,Excludes,AvgDelay,CRC"
                    Assert.AreEqual("S,10,0,0,0,2391,2394,9,2,0,2,7448,49755\r\n", stream.Content(),
                        "At 10 we get a summary with 2 outliers and 2 excludes");
                }
                else
                {
                    Assert.AreEqual("S,10,0,0,0,2391,2394,9,2,0,2,7448,49755\r\nS,5,0,0,0,2386,2399,14,5,5,5,8005,17350\r\n", stream.Content(),
                        "At 15 we get the next batch with 5 outliers and excludes");
                }
            }
        }

        [TestMethod]
        public void SummaryWriterIdleTest()
        {
            var stream = new StringStream();
            var summaryWriter = new SummaryWriter(stream.Writer) {IdleFlushRate = 10, NonIdleFlushRate = 5};
            for (var i = 0; i < 10; i++)
            {
                var measurement = new Measurement(new DateTime(2020, 12, 28, 0, 0, i), 2400 + i, 8000 + i);
                var result = new FlowMeter
                {
                    Amplitude = i, HighPass = 0, LowPassFast = 2400, LowPassSlow = 2402, Exclude = false, ExcludeAll = false, Flow = false,
                    LowPassOnHighPass = 5 - i,
                    Outlier = false, Drift = false
                };
                summaryWriter.AddMeasurement(measurement, result);
                if (i == 0)
                {
                    Assert.AreEqual(10u, summaryWriter.FlushRate, "Flush rate stays 10 (idle flush rate) when there is nothing special.");
                }
                summaryWriter.PrepareWrite();
                summaryWriter.Write();
                if (i < 9)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(stream.Content()), "nothing written the fist 9 data points");
                }
                else
                {
                    //"S,Measure,Flows,SumAmplitude,SumLPonHP,LowPassFast,LowPassSlow,LPonHP,Outliers,Waits,Excludes,AvgDelay,CRC"
                    Assert.AreEqual("S,10,0,0,0,2400,2402,-4,0,0,0,8005,1750\r\n", stream.Content(),
                        "10 idle points written after #10. LowPassFast and LPonHP get reported too.");
                }
            }
        }
    }
}
