using System.IO;


namespace WaterMeterReader
{
    internal abstract class BatchWriter
    {
        protected readonly StreamWriter Writer;

        protected BatchWriter(StreamWriter writer)
        {
            Writer = writer;
        }

        public uint FlushRate { get; protected set; }

        protected static string Param(object paramValue) => "," + paramValue;

        public abstract void Write();

        public abstract void WriteHeader();

        public abstract void Flush();

        private static int CalcCrc(int crc, int character)
        {
            crc ^= character;
            for (var i = 0; i < 8; i++)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
            return crc;
        }

        internal static int Crc(string input)
        {
            var crc = 0;
            foreach (var character in input)
            {
                crc = CalcCrc(crc, character);
            }
            return crc;
        }
    }

}
