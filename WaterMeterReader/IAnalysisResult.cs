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
    internal interface IAnalysisResult
    {
        public double Amplitude { get; }
        public bool CalculatedFlow { get; }
        public bool Drift { get; }
        public bool Exclude { get; }
        public bool ExcludeAll { get; }
        public bool FirstOutlier { get; }
        public bool Flow { get; }
        public double HighPass { get; }
        public double LowPassDifference { get; }
        public double LowPassFast { get; }
        public double LowPassOnDifference { get; }
        public double LowPassOnHighPass { get; }
        public double LowPassSlow { get; }
        public bool Outlier { get; }
        public int PreviousMeasure { get; }
    }
}
