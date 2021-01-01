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
    internal class MeasurementWriter
    {
        public const int DefaultFlushRate = 10; // no more than 12 due to serial speed limitations

        private readonly StreamWriter _writer;
        private string _measureList;
        private int _measureLogCount;
        private uint _desiredFlushRate;

        public MeasurementWriter(StreamWriter writer)
        {
            _writer = writer;
            DesiredFlushRate = DefaultFlushRate;
            FlushRate = DefaultFlushRate;
            Reset();
        }

        public uint DesiredFlushRate
        {
            get => _desiredFlushRate;
            set
            {
                _desiredFlushRate = value;
                // Immediately apply if we are starting a new round, or aren't logging
                // Otherwise it will be done at the next reset.
                if (FlushRate == 0 || _measureLogCount == 0)
                {
                    FlushRate = DesiredFlushRate;
                }
            }
        }

        public uint FlushRate { get; private set; }

        private static string Param(object paramValue) => "," + paramValue;

        private void Reset()
        {
            _measureList = "M";
            _measureLogCount = 0;
            FlushRate = DesiredFlushRate;
        }

        // We print the measure log every nth time, n being the flush rate. This puts less strain on the receiving end.
        // Returns whether a write action took place, so we can ensure writes are in different loops.
        public bool Write(Measurement measurement)
        {
            if (FlushRate == 0)
            {
                return false;
            }
            _measureLogCount++;
            _measureList += Param(measurement.Value);
            _measureList += Param(measurement.Delay);
            if (_measureLogCount % FlushRate != 0)
            {
                return false;
            }
            Flush();
            return true;
        }

        public void WriteHeader()
        {
            var header = "M";
            for (var i = 0; i < FlushRate; i++)
            {
                header += $",M{i},W{i}";
            }
            header += ",CRC";
            _writer.WriteLine(header);
        }

        public void Flush()
        {
            _measureList += Param(Crc.Get(_measureList));
            _writer.WriteLine(_measureList);
            Reset();
        }
    }
}
