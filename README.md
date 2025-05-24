# WoFFCshTool
This program allows you to convert the csh database files from World of Final Fantasy, to csv file as well as allows you to convert the csv file, back to csh file. 
<br>The program can be launched from a command prompt terminal with a few argument switches to perform the conversion functions or you can also simply drag n drop a file on the tool which will do the appropriate conversion function depending to the file extension. a list of valid argument switches are given below:

<br>**Tool actions:**
<br>``-csv`` Converts the csh file's data into a new csv file
<br>``-csh`` Converts the data in the csv file into a new csh file
<br>``-?`` Display the help page. will also display few argument examples too.

<br>Commandline usage examples:
<br>``WoFFCshTool.exe -? ``
<br>``WoFFCshTool.exe -csv "map_encount_table.csh" ``
<br>``WoFFCshTool.exe -csh "map_encount_table.csv" ``

## Important notes
- The program requires .net 6.0 Desktop Runtime to be installed on your PC. you can get it from this [page](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).

- The csh file or csv file has to be specified after the tool action argument switches.

- If you drag n drop a csh file on the tool, it will convert the file to csv file. similarly if you drag n drop a csv file on the tool, it will convert the file to csh.

- Floating point type values will have a `f` string character, appended after the last digit. if you are entering a floating point value, then make sure to put the `f` string character after the last digit.

- Return carriage characters (or the new line character) in strings, will be represented as ``{NewLine}``.

- Its recommended to use Libre Office suite's Calc program to view/edit the csv file. MS excel is not recommended as it screws up the data in the csv, thereby corrupting the file in the process.
 
- Please report any problems that you encounter with the converted csv/csh files by opening an issue page detailing the issue here.
