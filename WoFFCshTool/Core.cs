namespace WoFFCshTool
{
    internal class Core
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("");

            try
            {
                if (args.Length == 0)
                {
                    SupportMethods.ErrorExit("Enough arguments not specified. please launch the program with -? switch for more info.");
                }

                if (args[0] == "-?")
                {
                    Console.WriteLine("Tool actions:");
                    Console.WriteLine("-? = Display this help page");
                    Console.WriteLine("-csv = Converts the csh data into a new csv file");
                    Console.WriteLine("-csh = Converts the csv data into a new csh file");

                    Console.WriteLine("");

                    Console.WriteLine("Examples:");
                    Console.WriteLine("WoFFCshTool.exe -?");
                    Console.WriteLine("WoFFCshTool.exe -csv \"map_encount_table.csh\"");
                    Console.WriteLine("WoFFCshTool.exe -csh \"map_encount_table.csv\"");

                    Console.ReadLine();
                    Environment.Exit(0);
                }

                if (args.Length < 2)
                {
                    SupportMethods.ErrorExit("Enough arguments not specified for this process");
                }

                var toolAction = args[0];
                var inFile = args[1];

                if (!File.Exists(inFile))
                {
                    SupportMethods.ErrorExit("Specified file in the argument is missing!");
                }

                switch (toolAction)
                {
                    case "-csh":
                        ConversionHelpers.ConvertToCsh(inFile);
                        break;

                    case "-csv":
                        ConversionHelpers.ConvertToCsv(inFile);
                        break;

                    default:
                        SupportMethods.ErrorExit("Specified tool action is invalid");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("An exception has occured!");
                Console.WriteLine("");
                Console.WriteLine($"{ex}");

                Environment.Exit(1);
            }
        }
    }
}