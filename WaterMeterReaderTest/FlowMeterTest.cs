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
    public class FlowMeterTest
    {
        private static void AssertDoubleEqual(double expected, double actual, string message = "")
        {
            Assert.IsTrue(Math.Abs(expected - actual) < 0.000001, "{0}:{1} != {2}", message, expected, actual);
        }

        private static void AssertResultsAreEqual(IAnalysisResult expected, IAnalysisResult actual, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
            {
                message += ": ";
            }
            AssertDoubleEqual(expected.Amplitude, actual.Amplitude, message + "Amplitude");
            Assert.AreEqual(expected.Outlier, actual.Outlier, message + "Outlier");
            Assert.AreEqual(expected.FirstOutlier, actual.FirstOutlier, message + "FirstOutlier");
            AssertDoubleEqual(expected.HighPass, actual.HighPass, message + "HighPass");
            AssertDoubleEqual(expected.LowPassOnHighPass, actual.LowPassOnHighPass, message + "LowPassOnHighPass");
            Assert.AreEqual(expected.CalculatedFlow, actual.CalculatedFlow, message + "Calculated flow");
            AssertDoubleEqual(expected.LowPassSlow, actual.LowPassSlow, message + "LowPassSlow");
            AssertDoubleEqual(expected.LowPassFast, actual.LowPassFast, message + "LowPassFast");
            AssertDoubleEqual(expected.LowPassDifference, actual.LowPassDifference, message + "LowPassDifference");
            AssertDoubleEqual(expected.LowPassOnDifference, actual.LowPassOnDifference, message + "LowPassOnDifference");
            Assert.AreEqual(expected.Drift, actual.Drift, message + "Drift");
            Assert.AreEqual(expected.Flow, actual.Flow, message + "Flow");
            Assert.AreEqual(expected.Exclude, actual.Exclude, message + "Exclude");
            Assert.AreEqual(expected.ExcludeAll, actual.ExcludeAll, message + "ExcludeAll");
        }

        private static void AssessAfterSeries(FlowMeter flowMeter, int count, Func<int, int> value, string description, IAnalysisResult expected)
        {
            for (var i = 0; i < count; i++)
            {
                flowMeter.AddMeasurement(new Measurement(DateTime.Now, value(i), 1500));
            }
            AssertResultsAreEqual(expected, flowMeter, description);
        }

        [TestMethod]
        public void FlowMeterDriftTest()
        {
            var flowMeter = new FlowMeter();

            var expectedIdle = new FlowMeter
            {
                Amplitude = 0,
                Outlier = false,
                FirstOutlier = false,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 1000,
                LowPassFast = 1000,
                LowPassDifference = 0.0,
                LowPassOnDifference = 0.0,
                Drift = false,
                Exclude = false,
                Flow = false
            };
            AssessAfterSeries(flowMeter, 10, i => 1000, "OK after 10 good samples to skip Startup, filters active", expectedIdle);

            var expectedHighOutlier = new FlowMeter
            {
                Amplitude = 970,
                Outlier = true,
                FirstOutlier = false,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 1059.1,
                LowPassFast = 1190,
                LowPassDifference = 130.9,
                LowPassOnDifference = 19.39,
                Drift = true,
                Exclude = true,
                Flow = false
            };
            AssessAfterSeries(flowMeter, 3, i => 2000, "3 high outliers trigger Drift and Outlier", expectedHighOutlier);

            var expectedLowOutlier = new FlowMeter
            {
                Amplitude = 1027.327,
                Outlier = true,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 996.50719,
                LowPassFast = 963.9,
                LowPassDifference = 32.60719,
                LowPassOnDifference = 22.897189,
                Drift = true,
                Exclude = true,
                Flow = false
            };
            AssessAfterSeries(flowMeter, 2, i => 0, "2 low outliers bring low pass results close", expectedLowOutlier);

            var expectedLagPeriod = new FlowMeter
            {
                Amplitude = 1.842389209,
                Outlier = false,
                FirstOutlier = false,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 998.2128825,
                LowPassFast = 996.444977,
                LowPassDifference = 1.767905424,
                LowPassOnDifference = 8.076097488,
                Drift = true,
                Exclude = true,
                Flow = false
            };
            AssessAfterSeries(flowMeter, 22, i => 1000, "Waiting until last drift point...", expectedLagPeriod);


            var expectedFirstNormal = new FlowMeter
            {
                Amplitude = 1.787117533,
                Outlier = false,
                FirstOutlier = false,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 998.266496,
                LowPassFast = 996.8004793,
                LowPassDifference = 1.466016654,
                LowPassOnDifference = 7.415089405,
                Drift = false,
                Exclude = false,
                Flow = false
            };
            AssessAfterSeries(flowMeter, 1, i => 1000, "No more drifting", expectedFirstNormal);
        }

        [TestMethod]
        public void FlowMeterEarlyOutlierTest()
        {
            var flowMeter = new FlowMeter();

            flowMeter.AddMeasurement(new Measurement(DateTime.Now, 5000, 0));
            flowMeter.AddMeasurement(new Measurement(DateTime.Now, 2500, 0));
            var expected = new FlowMeter
            {
                Amplitude = 2500,
                Outlier = true,
                FirstOutlier = true,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 2500,
                LowPassFast = 2500,
                LowPassDifference = 0,
                LowPassOnDifference = 0,
                Drift = false,
                Exclude = true,
                Flow = false,
                ExcludeAll = true
            };
            AssertResultsAreEqual(expected, flowMeter, "Early outlier reset");

            flowMeter.AddMeasurement(new Measurement(DateTime.Now, 2510, 0));
            expected.Amplitude = 0;
            expected.Outlier = false;
            expected.FirstOutlier = false;
            expected.LowPassSlow = 2510;
            expected.LowPassFast = 2510;
            expected.Exclude = false;
            expected.ExcludeAll = false;
            AssertResultsAreEqual(expected, flowMeter, "First after outlier reset");

            flowMeter.AddMeasurement(new Measurement(DateTime.Now, 2490, 0));
            expected.Amplitude = 20;
            expected.HighPass = -10;
            expected.LowPassOnHighPass = 2;
            expected.LowPassSlow = 2509.4;
            expected.LowPassFast = 2508;
            expected.LowPassDifference = 1.4;
            expected.LowPassOnDifference = 0.14;

            AssertResultsAreEqual(expected, flowMeter, "Second after outlier reset");
        }

        [TestMethod]
        public void FlowMeterManyOutliersAfterStartupTest()
        {
            var flowMeter = new FlowMeter();

            var expected = new FlowMeter
            {
                Amplitude = 1.787036608,
                Outlier = false,
                FirstOutlier = false,
                HighPass = 0.66796875,
                LowPassOnHighPass = 0.584171302,
                CalculatedFlow = false,
                LowPassSlow = 999.2665745,
                LowPassFast = 999.6856016,
                LowPassDifference = 0.419027152,
                LowPassOnDifference = 0.181146106,
                Drift = false,
                Exclude = false,
                Flow = false
            };

            AssessAfterSeries(flowMeter, 10, i => 999 + i % 2 * 2, "10 good samples to skip Startup", expected);

            expected.Amplitude = 1000.7334255095383;
            expected.Exclude = true;
            expected.Outlier = true;
            expected.FirstOutlier = true;

            AssessAfterSeries(flowMeter, 1, i => 2000, "First outlier raises FirstOutlier, Outlier and Exclude", expected);

            expected.Amplitude = 784.3181768;
            expected.FirstOutlier = false;
            expected.LowPassSlow = 1239.211368;
            expected.LowPassFast = 1612.457707;
            expected.LowPassDifference = 373.2463381;
            expected.LowPassOnDifference = 168.2699421;
            expected.Drift = true;

            AssessAfterSeries(flowMeter, 9, i => 2000, "9 more outliers, high pass filter inactive, low pass work", expected);
        }

        [TestMethod]
        public void FlowMeterStartFlowTest()
        {
            // a flow of three vibrations, then back to flat. It takes a while to switch off the flow, but in practice 10 points is about 0.1s

            var flowMeter = new FlowMeter();

            var expected = new FlowMeter
            {
                Amplitude = 0,
                Outlier = false,
                FirstOutlier = false,
                HighPass = 0,
                LowPassOnHighPass = 0,
                CalculatedFlow = false,
                LowPassSlow = 2500,
                LowPassFast = 2500,
                LowPassDifference = 0.0,
                LowPassOnDifference = 0.0,
                Drift = false,
                Exclude = false,
                Flow = false
            };
            AssessAfterSeries(flowMeter, 2, i => 2500, "baseline", expected);

            expected.Amplitude = 102.8305594;
            expected.HighPass = -67.1875;
            expected.LowPassOnHighPass = 48.5583;
            expected.CalculatedFlow = true;
            expected.LowPassSlow = 2499.745643;
            expected.LowPassFast = 2497.5339;
            expected.LowPassDifference = 2.211742647;
            expected.LowPassOnDifference = 1.776377025;
            expected.Flow = true;
            AssessAfterSeries(flowMeter, 6, i => 2600 - i % 2 * 200, "flow", expected);

            expected.Amplitude = 0.187569249;
            expected.HighPass = 0.016021729;
            expected.LowPassOnHighPass = 5.105309729;
            expected.CalculatedFlow = false;
            expected.LowPassSlow = 2499.818058;
            expected.LowPassFast = 2499.226112;
            expected.LowPassDifference = 0.59194614;
            expected.LowPassOnDifference = 1.267210968;
            expected.Flow = false;
            AssessAfterSeries(flowMeter, 11, i => 2500, "idle", expected);
        }
    }
}
