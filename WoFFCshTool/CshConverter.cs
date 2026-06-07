using System.Globalization;
using System.Text;

namespace WoFFCshTool
{
    internal class CshConverter
    {
        public static void ConvertToCsh(string csvFile, bool useShiftJIS)
        {
            var cshVars = new CshVariables();

            Encoding csvEncoding;

            if (useShiftJIS)
            {
                cshVars.EncodingFlag = 1;
                csvEncoding = CodePagesEncodingProvider.Instance.GetEncoding(932);
                Console.WriteLine("Encoding: shift-jis");
            }
            else
            {
                cshVars.EncodingFlag = 0;
                csvEncoding = Encoding.UTF8;
                Console.WriteLine("Encoding: UTF8");
            }

            var csvData = File.ReadAllLines(csvFile, csvEncoding);

            if (csvData.Length == 0)
            {
                SupportMethods.ErrorExit("Specified csv file is empty!");
            }

            cshVars.FieldCount = (uint)csvData[0].Split(cshVars.CSVDelimiter).Length;
            cshVars.RowsCount = (uint)csvData.Length;

            Console.WriteLine($"Field Count: {cshVars.FieldCount}");
            Console.WriteLine($"Entries Table Count: {cshVars.RowsCount}");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Generating csh header....");
            Console.WriteLine("");

            byte[] headerData;
            long headerSize;

            using (var preCmpHeaderStream = new MemoryStream())
            {
                using (var preCmpHeaderWriter = new BinaryWriter(preCmpHeaderStream))
                {
                    _ = preCmpHeaderWriter.BaseStream.Position = 0;
                    preCmpHeaderWriter.WriteBytesUInt32(cshVars.EncodingFlag, true);
                    preCmpHeaderWriter.WriteBytesUInt32(cshVars.FieldCount, true);
                    preCmpHeaderWriter.WriteBytesUInt32(cshVars.RowsCount, true);
                    headerSize = preCmpHeaderWriter.BaseStream.Length;

                    preCmpHeaderStream.Seek(0, SeekOrigin.Begin);
                    headerData = preCmpHeaderStream.ToArray();
                }
            }

            Console.WriteLine("Generating csh entry table....");
            Console.WriteLine("");

            byte[] entryTableData;
            var entryDataDict = new Dictionary<int, List<byte>>();

            using (var preCmpEntryTableStream = new MemoryStream())
            {
                using (var preCmpEntryTableWriter = new BinaryWriter(preCmpEntryTableStream))
                {
                    _ = preCmpEntryTableWriter.BaseStream.Position = 0;

                    long offset = headerSize + (cshVars.RowsCount * (cshVars.FieldCount * 8));
                    cshVars.EntryDataOffset = 0;

                    string[] currentEntryData;
                    int dictKey = 0;

                    foreach (var entryData in csvData)
                    {
                        currentEntryData = entryData.Split(cshVars.CSVDelimiter);

                        cshVars.EntryData = new List<byte>();

                        foreach (var entryField in currentEntryData)
                        {
                            byte[] currentEntryDataBytes = Array.Empty<byte>();

                            if (string.IsNullOrEmpty(entryField))
                            {
                                // empty
                                cshVars.EntryDataOffset = 0;
                                cshVars.EntryDataType = 192;
                                cshVars.EntryDataSizeMultiplier = 0;
                            }
                            else if (entryField.EndsWith("f") && float.TryParse(entryField.Replace("f", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out cshVars.EntryFloatValue) == true)
                            {
                                // float
                                cshVars.EntryDataOffset = (uint)offset;
                                cshVars.EntryDataType = 128;
                                cshVars.EntryDataSizeMultiplier = 1;
                                currentEntryDataBytes = BitConverter.GetBytes(cshVars.EntryFloatValue);
                                Array.Reverse(currentEntryDataBytes);
                                offset += 4;
                            }
                            else if (int.TryParse(entryField, NumberStyles.Integer, CultureInfo.InvariantCulture, out cshVars.EntryIntValue) == true)
                            {
                                // int
                                cshVars.EntryDataOffset = (uint)offset;
                                cshVars.EntryDataType = 64;
                                cshVars.EntryDataSizeMultiplier = 1;
                                currentEntryDataBytes = BitConverter.GetBytes(cshVars.EntryIntValue);
                                Array.Reverse(currentEntryDataBytes);
                                offset += 4;
                            }
                            else
                            {
                                if (entryField.StartsWith("0x"))
                                {
                                    // unknown
                                    cshVars.EntryDataOffset = 0;
                                    cshVars.EntryDataType = Convert.ToByte(entryField.Substring(2), 16);
                                    cshVars.EntryDataSizeMultiplier = 0;
                                }
                                else
                                {
                                    // string
                                    cshVars.EntryDataOffset = (uint)offset;
                                    cshVars.EntryDataType = 0;

                                    var readStringVal = entryField + "\0";
                                    cshVars.EntryStringVal = SupportMethods.FixStringFromCsv(readStringVal);
                                    var currentEntryStringList = csvEncoding.GetBytes(cshVars.EntryStringVal).ToList();
                                    var currentEntryStringValSize = currentEntryStringList.Count;

                                    if (currentEntryStringValSize % 4 != 0)
                                    {
                                        var remainder = currentEntryStringValSize % 4;
                                        var increaseAmount = 4 - remainder;
                                        currentEntryStringValSize += increaseAmount;

                                        for (int p = 0; p < increaseAmount; p++)
                                        {
                                            currentEntryStringList.Add(0x00);
                                        }

                                        cshVars.EntryDataSizeMultiplier = (ushort)(currentEntryStringValSize / 4);
                                    }
                                    else
                                    {
                                        cshVars.EntryDataSizeMultiplier = (ushort)(currentEntryStringValSize / 4);
                                    }

                                    offset += currentEntryStringValSize;
                                    currentEntryDataBytes = currentEntryStringList.ToArray();
                                }
                            }

                            preCmpEntryTableWriter.WriteBytesUInt32(cshVars.EntryDataOffset, true);
                            preCmpEntryTableWriter.Write(cshVars.EntryDataType);
                            preCmpEntryTableWriter.Write(cshVars.EntryReserved);
                            preCmpEntryTableWriter.WriteBytesUInt16(cshVars.EntryDataSizeMultiplier, true);

                            cshVars.EntryData.AddRange(currentEntryDataBytes);
                        }

                        entryDataDict.Add(dictKey, cshVars.EntryData);
                        dictKey++;
                    }

                    preCmpEntryTableStream.Seek(0, SeekOrigin.Begin);
                    entryTableData = preCmpEntryTableStream.ToArray();
                }
            }

            Console.WriteLine("Packing csv data....");
            Console.WriteLine("");

            byte[] csvEntriesData;

            using (var preCmpCsvEntriesDataStream = new MemoryStream())
            {
                using (var csvEntriesDataWriter = new BinaryWriter(preCmpCsvEntriesDataStream))
                {
                    _ = csvEntriesDataWriter.BaseStream.Position = 0;

                    foreach (var csvEntryData in entryDataDict.Values)
                    {
                        csvEntriesDataWriter.Write(csvEntryData.ToArray());
                    }

                    preCmpCsvEntriesDataStream.Seek(0, SeekOrigin.Begin);
                    csvEntriesData = preCmpCsvEntriesDataStream.ToArray();
                }
            }


            Console.WriteLine("Arranging and compressing csh data....");
            Console.WriteLine("");

            byte[] dataToCmp;

            using (var dataToCmpStream = new MemoryStream())
            {
                dataToCmpStream.Write(headerData);
                dataToCmpStream.Write(entryTableData);
                dataToCmpStream.Write(csvEntriesData);

                dataToCmpStream.Seek(0, SeekOrigin.Begin);
                dataToCmp = dataToCmpStream.ToArray();
                cshVars.DcmpSize = (uint)dataToCmpStream.Length;
            }

            var cmpData = SupportMethods.ZlibCompressBuffer(dataToCmp);
            cshVars.CmpSize = (uint)cmpData.Length;

            var outCshFile = Path.Combine(Path.GetDirectoryName(csvFile), Path.GetFileNameWithoutExtension(csvFile) + ".csh");

            if (File.Exists(outCshFile))
            {
                File.Delete(outCshFile);
            }

            Console.WriteLine("Building csh file....");
            Console.WriteLine("");

            using (var cshStreamWriter = new BinaryWriter(File.Open(outCshFile, FileMode.Append, FileAccess.Write)))
            {
                cshStreamWriter.Write(Encoding.UTF8.GetBytes("\0" + cshVars.CmpMagic));
                cshStreamWriter.WriteBytesUInt32(cshVars.CmpUnkVal, true);
                cshStreamWriter.WriteBytesUInt32(cshVars.CmpUnkVal2, true);
                cshStreamWriter.WriteBytesUInt32(cshVars.CmpUnkVal3, true);
                PadReservedBytes(cshStreamWriter, 6);

                cshStreamWriter.WriteBytesUInt32(cshVars.CmpUnkVal4, true);
                PadReservedBytes(cshStreamWriter, 21);

                cshStreamWriter.Write(Encoding.UTF8.GetBytes(cshVars.CmpZLIBMagic));
                cshStreamWriter.WriteBytesUInt32(cshVars.DcmpSize, true);
                cshStreamWriter.WriteBytesUInt32(cshVars.CmpSize, true);
                cshStreamWriter.WriteBytesUInt32(cshVars.CmpUnkVal5, true);

                cshStreamWriter.Write(cmpData);
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Finished writing csv data to csh file");
        }

        private static void PadReservedBytes(BinaryWriter cshStreamWriter, int padAmount)
        {
            for (int l = 0; l < padAmount; l++)
            {
                cshStreamWriter.WriteBytesUInt32(0, false);
            }
        }
    }
}