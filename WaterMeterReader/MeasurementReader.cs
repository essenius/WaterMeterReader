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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WaterMeterReader
{
    internal class MeasurementReader
    {
        private readonly StreamReader _reader;
        private int _index;
        private DateTime _timestamp;
        private List<int> _values;

        public MeasurementReader(StreamReader reader)
        {
            _reader = reader;
            CrcOk = true;
        }

        public bool CrcOk { get; private set; }

        public bool HasNext() => _values != null && _index < _values.Count || !_reader.EndOfStream;

        public Measurement NextMeasurement()
        {
            if (_values == null)
            {
                // skip the header line
                _reader.ReadLine();
            }
            if (_values == null || _index >= _values.Count)
            {
                ReadLine();
            }
            var nextTimeStamp = _timestamp + TimeSpan.FromMilliseconds(0.1) * _index / 2;
            return new Measurement(nextTimeStamp, _values[_index++], _values[_index++]);
        }

        private void ReadLine()
        {
            var line = _reader.ReadLine();
            //if (line.EndsWith(",1") || line.EndsWith(",0"))
            //{
            //    line = line[..^2];
            //}
            var entries = line.Split(',').ToList();
            // skip header rows etc.
            if (!int.TryParse(entries.Last(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var crc))
            {
                ReadLine();
                return;
            }
            //var crc = Convert.ToInt32(entries.Last());
            var crcTarget = line[line.IndexOf('M')..];
            crcTarget = crcTarget.Remove(crcTarget.LastIndexOf(','));
            var crcCalculated = BatchWriter.Crc(crcTarget);
            CrcOk &= crcCalculated == crc;
            _timestamp = DateTime.Parse(entries[0]);

            // copy over the measurement values and delays
            _values = new List<int>();
            foreach (var entry in entries.Skip(2).SkipLast(1))
            {
                _values.Add(Convert.ToInt32(entry));
            }
            _index = 0;
        }
    }
}
