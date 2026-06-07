using System.Text;

namespace WoFFCshTool
{
    internal class CsvConverter
    {
        public static void ConvertToCsv(string cshFile)
        {
            var cshVars = new CshVariables();

            byte[] dcmpCshData;

            Console.WriteLine("Decompressing csh file....");
            Console.WriteLine("");

            using (var cmpCshReader = new BinaryReader(File.Open(cshFile, FileMode.Open, FileAccess.Read)))
            {
                // Read cmp header values and
                // do basic checks
                if (cmpCshReader.BaseStream.Length < 144)
                {
                    SupportMethods.ErrorExit("csh file is too small in size!");
                }

                _ = cmpCshReader.BaseStream.Position = 0;

                if (cmpCshReader.ReadBytesString(4, false) != cshVars.CmpMagic)
                {
                    SupportMethods.ErrorExit("Not a valid csh file!");
                }

                _ = cmpCshReader.BaseStream.Position += 124;

                if (cmpCshReader.ReadBytesString(4, false) != cshVars.CmpZLIBMagic)
                {
                    SupportMethods.ErrorExit("Not a valid csh file!");
                }

                cshVars.DcmpSize = cmpCshReader.ReadBytesUInt32(true);
                cshVars.CmpSize = cmpCshReader.ReadBytesUInt32(true);
                cshVars.CmpUnkVal5 = cmpCshReader.ReadBytesUInt32(true);

                var cmpData = new byte[cshVars.CmpSize];
                cmpData = cmpCshReader.ReadBytes((int)cshVars.CmpSize);

                using (var cmpStream = new MemoryStream())
                {
                    cmpStream.Write(cmpData, 0, (int)cshVars.CmpSize);
                    cmpStream.Seek(0, SeekOrigin.Begin);

                    dcmpCshData = SupportMethods.ZlibDecompressBuffer(cmpStream);
                }
            }

            using (var dcmpStream = new MemoryStream())
            {
                dcmpStream.Write(dcmpCshData, 0, (int)cshVars.DcmpSize);
                dcmpStream.Seek(0, SeekOrigin.Begin);

                Console.WriteLine("Reading csh entries....");
                Console.WriteLine("");

                using (var dcmpCshReader = new BinaryReader(dcmpStream))
                {
                    // Read dcmp header values
                    // and do basic checks
                    if (dcmpCshReader.BaseStream.Length < 12)
                    {
                        SupportMethods.ErrorExit("csh file is too small in size!");
                    }

                    _ = dcmpCshReader.BaseStream.Position = 0;
                    cshVars.EncodingFlag = dcmpCshReader.ReadBytesUInt32(true);

                    Encoding csvEncoding;

                    if (cshVars.EncodingFlag == 1)
                    {
                        Console.WriteLine("Encoding detected: shift-jis");
                        csvEncoding = CodePagesEncodingProvider.Instance.GetEncoding(932);
                        Console.OutputEncoding = csvEncoding;
                    }
                    else
                    {
                        Console.WriteLine("Encoding detected: UTF8");
                        csvEncoding = Encoding.UTF8;
                    }

                    cshVars.FieldCount = dcmpCshReader.ReadBytesUInt32(true);
                    cshVars.RowsCount = dcmpCshReader.ReadBytesUInt32(true);

                    Console.WriteLine($"Field Count: {cshVars.FieldCount}");
                    Console.WriteLine($"Rows Count: {cshVars.RowsCount}");
                    Console.WriteLine("");

                    var entryTablePos = dcmpCshReader.BaseStream.Position;

                    // Read each entry and write to a csv file
                    var outCsvFile = Path.Combine(Path.GetDirectoryName(cshFile), Path.GetFileNameWithoutExtension(cshFile) + ".csv");

                    if (File.Exists(outCsvFile))
                    {
                        File.Delete(outCsvFile);
                    }

                    using (var csvWriter = new StreamWriter(outCsvFile, true, csvEncoding))
                    {
                        var currentPos = entryTablePos;

                        for (int i = 0; i < cshVars.RowsCount; i++)
                        {
                            for (int j = 0; j < cshVars.FieldCount; j++)
                            {
                                _ = dcmpCshReader.BaseStream.Position = currentPos;

                                cshVars.EntryDataOffset = dcmpCshReader.ReadBytesUInt32(true);
                                cshVars.EntryDataType = dcmpCshReader.ReadByte();
                                _ = dcmpCshReader.ReadByte();
                                cshVars.EntryDataSizeMultiplier = dcmpCshReader.ReadBytesUInt16(true);

                                _ = dcmpCshReader.BaseStream.Position = cshVars.EntryDataOffset;
                                cshVars.EntryData.AddRange(dcmpCshReader.ReadBytes(cshVars.EntryDataSizeMultiplier * 4));

                                switch (cshVars.EntryDataType)
                                {
                                    // string
                                    case 0:
                                        var readStringValue = SupportMethods.FixStringFromCsh(csvEncoding.GetString(cshVars.EntryData.ToArray()));
                                        cshVars.EntryOnCSV += readStringValue;
                                        break;

                                    // int
                                    case 64:
                                        if (cshVars.EntryDataSizeMultiplier * 4 == 4)
                                        {
                                            var readValueArray = cshVars.EntryData.ToArray();
                                            Array.Reverse(readValueArray);

                                            cshVars.EntryIntValue = BitConverter.ToInt32(readValueArray, 0);
                                            cshVars.EntryOnCSV += cshVars.EntryIntValue.ToString();
                                        }
                                        break;

                                    // float
                                    case 128:
                                        if (cshVars.EntryDataSizeMultiplier * 4 == 4)
                                        {
                                            var readValueArray = cshVars.EntryData.ToArray();
                                            Array.Reverse(readValueArray);

                                            cshVars.EntryFloatValue = BitConverter.ToSingle(readValueArray, 0);
                                            cshVars.EntryOnCSV += cshVars.EntryFloatValue.ToString() + "f";
                                        }
                                        break;

                                    // empty
                                    case 192:
                                        break;

                                    // unknown
                                    default:
                                        cshVars.EntryOnCSV += $"0x{cshVars.EntryDataType.ToString("X2")}";
                                        break;
                                }

                                if (j != cshVars.FieldCount - 1)
                                {
                                    cshVars.EntryOnCSV += cshVars.CSVDelimiter;
                                }

                                cshVars.EntryData.Clear();

                                currentPos += 8;
                            }

                            Console.WriteLine($"Entry_{i}: {cshVars.EntryOnCSV}");
                            csvWriter.WriteLine(cshVars.EntryOnCSV);
                            cshVars.EntryOnCSV = string.Empty;
                        }
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Finished writing csh data to csv file");
        }
    }
}