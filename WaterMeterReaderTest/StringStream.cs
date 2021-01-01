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
using System.Text;

namespace WaterMeterReaderTest
{
    public class StringStream
    {
        private readonly MemoryStream _memoryStream;

        public StringStream()
        {
            _memoryStream = new MemoryStream();
            Writer = new StreamWriter(_memoryStream);
        }

        public StringStream(string input)
        {
            _memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(input));
            Reader = new StreamReader(_memoryStream);
        }

        public StreamReader Reader { get; set; }
        public StreamWriter Writer { get; }

        public string Content()
        {
            Writer.Flush();
            return Encoding.ASCII.GetString(_memoryStream.ToArray());
        }
    }
}
