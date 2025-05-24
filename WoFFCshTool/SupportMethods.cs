using System.IO.Compression;

namespace WoFFCshTool
{
    internal class SupportMethods
    {
        public static void ErrorExit(string errorMsg)
        {
            Console.WriteLine($"Error: {errorMsg}");
            Console.WriteLine("");
            Console.ReadLine();
            Environment.Exit(1);
        }


        public static byte[] ZlibDecompressBuffer(Stream cmpStreamName)
        {
            var dcmpBuffer = Array.Empty<byte>();

            using (var outStreamName = new MemoryStream())
            {
                cmpStreamName.Seek(0, SeekOrigin.Begin);

                using (var decompressor = new ZLibStream(cmpStreamName, CompressionMode.Decompress, true))
                {
                    decompressor.CopyTo(outStreamName);
                }

                outStreamName.Seek(0, SeekOrigin.Begin);
                dcmpBuffer = outStreamName.ToArray();
            }

            return dcmpBuffer;
        }


        public static string FixStringFromCsh(string cshString)
        {
            var fixedString = cshString;
            fixedString = fixedString.Replace("\0", "");
            fixedString = fixedString.Replace("\0", "").Replace("\r\n", "{NewLine}");

            return fixedString;
        }


        public static byte[] ZlibCompressBuffer(byte[] dataToCmp)
        {
            var compressedDataBuffer = Array.Empty<byte>();

            using (var zlibDataStream = new MemoryStream())
            {
                using (var compressor = new ZLibStream(zlibDataStream, CompressionLevel.SmallestSize, true))
                {
                    compressor.Write(dataToCmp);
                }

                compressedDataBuffer = new byte[zlibDataStream.Length];
                zlibDataStream.Seek(0, SeekOrigin.Begin);
                zlibDataStream.Read(compressedDataBuffer, 0, compressedDataBuffer.Length);
            }

            return compressedDataBuffer;
        }


        public static string FixStringFromCsv(string csvString)
        {
            var fixedString = csvString;
            fixedString = fixedString.Replace("{NewLine}", "\r\n");

            return fixedString;
        }
    }
}