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

namespace WaterMeterReader
{
    /// <summary>
    ///     Analyze the raw magnetometer data and find out whether there is a flow. If so, get a sense from the amount of flow by summing
    ///     the amplitudes.  We identify flow by the amplitude of the data. If water flows, the sensor shows a varying vibrating signal.
    ///     If there is no flow, we just get noise.
    ///     To eliminate the noise, we need to do a bit of filtering. Also, we need to cater for outliers, e.g. when the sensor shifts.
    ///     To identify flow, we apply a high pass filter on the data and then we apply a low pass filter on the absolute value of the
    ///     high pass filter output. If this value is above a certain threshold, there is a flow.
    ///     We use a hysteresis function (trigger flow on at a higher value than triggering it off), since there can be quieter parts
    ///     in a flow signal and we don't want it to start bouncing.
    ///
    ///     We also need to identify outliers (e.g. spikes) and signal drifts (e.g. shifting sensor). That could otherwise be interpreted
    ///     as a flow. To do this, we take the difference between two low pass filters (a slower and a faster one) on the input.
    ///     As that signal can be a bit jittery (to respond fast enough), we apply another low pass filter on it.
    ///     We have drift (significant move of the average value) if this last signal is above a certain threshold.
    ///
    ///     Outliers are a bit simpler: for that we look at the absolute value between the slow low pass filter and the raw sensor value
    ///     (i.e. the amplitude). If we have a spike (i.e. a single outlier) we ignore it alltogether.
    ///     Of course all these calculations require parameters, which have been calibrated with sensor data.
    /// </summary>
    internal class FlowMeter : IAnalysisResult
    {
        private const double DriftThresholdIfIdle = 7.5;
        private const double DriftThresholdInFlow = 12;
        private const double HighPassAlpha = 0.5;
        private const double LowPassAlphaFast = 0.1;
        private const double LowPassAlphaSlow = 0.03;
        private const double LowPassOnDifferenceAlpha = 0.1;
        private const double LowPassOnHighPassAlpha = 0.2;
        private const double OutlierThreshold = 200.0;
        private const int StartupSamples = 10;
        private const double SwitchFlowOffThreshold = 6.0;
        private const double SwitchFlowOnThreshold = 10.0;

        private uint _startupSamplesLeft = StartupSamples;

        public double Amplitude { get; set; }
        public bool CalculatedFlow { get; set; }
        public bool Drift { get; set; }
        public bool Exclude { get; set; }
        public bool ExcludeAll { get; set; }
        public bool FirstOutlier { get; set; }
        public bool Flow { get; set; }
        public double HighPass { get; set; }
        public double LowPassDifference { get; set; }
        public double LowPassFast { get; set; }
        public double LowPassOnDifference { get; set; }
        public double LowPassOnHighPass { get; set; }
        public double LowPassSlow { get; set; }
        public bool Outlier { get; set; }
        public int PreviousMeasure { get; set; }

        public void AddMeasurement(Measurement measure)
        {
            var firstCall = _startupSamplesLeft == StartupSamples;
            if (_startupSamplesLeft > 0)
            {
                _startupSamplesLeft--;
            }
            // if we don't have a previous measurement yet, use defaults.
            if (firstCall)
            {
                // since the filters need initial values, set those. Also initialize the anomaly indicators.
                // _measure = measure.Value;
                ResetAnomalies();
                ResetFilters(measure.Value);
                Amplitude = 0;
                return;
            }

            DetectOutlier(measure.Value);
            // Don't include the result of single outliers (spikes) in the filters. They can have too much influence.
            // If there are multiple, the next ones are not ignored.
            if (!FirstOutlier)
            {
                DetectFlow(measure.Value);
                DetectDrift(measure.Value);
            }
            MarkAnomalies(measure.Value);
        }

        private void DetectDrift(int measure)
        {
            LowPassSlow = LowPassFilter(measure, LowPassSlow, LowPassAlphaSlow);
            LowPassFast = LowPassFilter(measure, LowPassFast, LowPassAlphaFast);
            LowPassDifference = Math.Abs(LowPassSlow - LowPassFast);
            LowPassOnDifference = LowPassFilter(LowPassDifference, LowPassOnDifference, LowPassOnDifferenceAlpha);

            // this works on the flow of the previous sample (this is why we have CalculatedFlow).
            Drift = LowPassOnDifference >= (Flow ? DriftThresholdInFlow : DriftThresholdIfIdle);
        }

        private void DetectFlow(int measure)
        {
            if (Outlier)
            {
                CalculatedFlow = false;
                return;
            }
            HighPass = HighPassFilter(measure, PreviousMeasure, HighPass, HighPassAlpha);
            LowPassOnHighPass =
                LowPassFilter(Math.Abs(HighPass), LowPassOnHighPass, LowPassOnHighPassAlpha);
            CalculatedFlow = LowPassOnHighPass >= (Flow ? SwitchFlowOffThreshold : SwitchFlowOnThreshold);
            PreviousMeasure = measure;
        }

        private void DetectOutlier(int measure)
        {
            Amplitude = Math.Abs(LowPassSlow - measure);
            var previousIsOutlier = Outlier;
            Outlier = Amplitude > OutlierThreshold;
            FirstOutlier = Outlier && !previousIsOutlier;
        }

        private static double HighPassFilter(double measure, double previous, double filterValue, double alpha) =>
            alpha * (filterValue + measure - previous);

        private static double LowPassFilter(double measure, double filterValue, double alpha) => alpha * measure + (1 - alpha) * filterValue;

        private void MarkAnomalies(int measure)
        {
            Exclude = Outlier || Drift;
            Flow = CalculatedFlow && !Exclude;
            if (!Exclude)
            {
                return;
            }
            ExcludeAll = _startupSamplesLeft > 0;
            if (!ExcludeAll)
            {
                return;
            }
            // We have a problem in the first few measurements. It might as well have been the first one (e.g. startup issue).
            // so we discard what we have so far and start again. We keep the current value as seed for the low pass filters.
            // If this was the outlier, it will be caught the next time.
            ResetFilters(measure);
            _startupSamplesLeft = StartupSamples;
        }

        private void ResetAnomalies()
        {
            CalculatedFlow = false;
            Drift = false;
            Exclude = false;
            ExcludeAll = false;
            Flow = false;
            Outlier = false;
            FirstOutlier = false;
        }

        public void ResetFilters(int initlalMeasurement)
        {
            HighPass = 0.0;
            LowPassFast = initlalMeasurement;
            LowPassDifference = 0.0;
            LowPassOnDifference = 0.0;
            LowPassOnHighPass = 0.0;
            LowPassSlow = initlalMeasurement;
            PreviousMeasure = initlalMeasurement;
        }
    }
}
