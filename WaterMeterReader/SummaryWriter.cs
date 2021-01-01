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

using System.IO;

namespace WaterMeterReader
{
    internal class SummaryWriter
    {
        private const uint FlushRateIfIdle = 1;
        private const uint FlushRateIfInteresting = 1;
        private readonly StreamWriter _writer;
        private uint _driftCount;
        private uint _excludeCount;

        private uint _flowCount;

        private Measurement _measure;
        private uint _outlierCount;
        private IAnalysisResult _result;
        private double _sumAmplitude;
        private long _sumDelay;
        private double _sumLowPassOnHighPass;
        private string _summary;
        private uint _summaryCount;

        public SummaryWriter(StreamWriter writer)
        {
            _writer = writer;
            IdleFlushRate = FlushRateIfIdle;
            NonIdleFlushRate = FlushRateIfInteresting;
            Reset();
        }

        public uint FlushRate { get; private set; }

        public uint IdleFlushRate { get; set; }
        public uint NonIdleFlushRate { get; set; }

        public void AddMeasurement(Measurement measure, IAnalysisResult result)
        {
            // we do the reset here, so flush rates can be set just before the first call to AddMeasurement
            if (_summaryCount == 0)
            {
                Reset();
            }
            _summaryCount++;
            _measure = measure;
            _result = result;
            _sumDelay += measure.Delay;
            if (result.Flow)
            {
                _flowCount++;
                _sumAmplitude += result.Amplitude;
                _sumLowPassOnHighPass += result.LowPassOnHighPass;
            }
            if (result.Outlier)
            {
                _outlierCount++;
            }
            if (result.Drift)
            {
                _driftCount++;
            }
            if (result.ExcludeAll)
            {
                _excludeCount = _summaryCount;
            }
            else if (result.Exclude)
            {
                _excludeCount++;
            }
        }

        public void Flush()
        {
            Write();
            PrepareWrite(true);
            Write();
        }

        private static string Param(object paramValue) => "," + paramValue;

        public void PrepareWrite(bool endOfFile = false)
        {
            // We set the flush rate regardless of whether we still need to write something.
            // can probably do this smarter. Once set in a batch, no need to set again.
            var isInteresting = _flowCount > 0 || _excludeCount > 0;
            if (isInteresting)
            {
                FlushRate = NonIdleFlushRate;
            }

            // If we didn't write the summary the previous run, don't overwrite it.
            if (!string.IsNullOrEmpty(_summary))
            {
                return;
            }

            // only write every nth sample unless we have no more data
            if (_summaryCount % FlushRate != 0 && !endOfFile)
            {
                return;
            }

            if (_summaryCount == 0 && endOfFile)
            {
                return;
            }

            var averageDelay = (_sumDelay * 10 / _summaryCount + 5) / 10;
            _summary = "S";
            // If flush rate is 1, we're debugging. Store the measure instead of the measure count.
            _summary += Param(FlushRate == 1 ? _measure.Value : (int) _summaryCount);
            _summary += Param(_flowCount);
            _summary += Param(_sumAmplitude);
            _summary += Param(_sumLowPassOnHighPass);
            _summary += Param(_result.LowPassFast);
            _summary += Param(_result.LowPassSlow);
            _summary += Param(_result.LowPassOnHighPass);
            _summary += Param(_outlierCount);
            _summary += Param(_driftCount);
            _summary += Param(_excludeCount);
            _summary += Param(averageDelay);
            _summary += Param(Crc.Get(_summary));

            _summaryCount = 0;
            // As we can't go faster than 115200 baud, we need to limit the amount of data sent each mesurement. Hence the csv format.
            // The use of multiple print commands is slower than concatenating a string and printing once. 
        }

        private void Reset()
        {
            _flowCount = 0;
            _driftCount = 0;
            _outlierCount = 0;
            _excludeCount = 0;
            _sumAmplitude = 0.0;
            _sumLowPassOnHighPass = 0.0;
            FlushRate = IdleFlushRate;
            _sumDelay = 0;
        }

        public void Write()
        {
            if (string.IsNullOrEmpty(_summary))
            {
                return;
            }
            _writer.WriteLine(_summary);
            _summary = string.Empty;
        }

        public void WriteHeader()
        {
            _writer.WriteLine("S,Measure,Flows,SumAmplitude,SumLPonHP,LowPassFast,LowPassSlow,LPonHP,Outliers,Drifts,Excludes,AvgDelay,CRC");
        }
    }
}
