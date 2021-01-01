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
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WaterMeterReaderTest")]

namespace WaterMeterReader
{
    public class Program
    {
        public static void Main()
        {
            const string inFile = @"error.txt";
            using var reader = new StreamReader(inFile);
            var measureOutFile = Path.GetFileNameWithoutExtension(inFile);
            var summaryOutfile = measureOutFile + "_s.csv";
            measureOutFile += "_m.csv";
            var measureReader = new MeasurementReader(reader);
            using var measureStreamWriter = new StreamWriter(measureOutFile);
            using var summaryStreamWriter = new StreamWriter(summaryOutfile);

            var flowMeter = new FlowMeter();
            var summaryWriter = new SummaryWriter(summaryStreamWriter);
            var measureWriter = new MeasurementWriter(measureStreamWriter);
            summaryWriter.WriteHeader();
            measureWriter.WriteHeader();
            while (measureReader.HasNext())
            {
                var measurement = measureReader.NextMeasurement();
                flowMeter.AddMeasurement(measurement);
                summaryWriter.AddMeasurement(measurement, flowMeter);
                summaryWriter.PrepareWrite();
                if (!measureWriter.Write(measurement) || measureWriter.FlushRate == 1 || summaryWriter.FlushRate == 1)
                {
                    summaryWriter.Write();
                }
            }
            measureWriter.Flush();
            summaryWriter.Flush();
            reader.Close();
            measureStreamWriter.Close();
            summaryStreamWriter.Close();
        }
    }
}
