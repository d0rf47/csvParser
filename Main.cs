using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;


namespace perle.tech.benchmarking
{
    class Start
    {
        /**
            Args
            1-JSON file directory
            2-output result filepath
        */
        static void Main(string[] args)       
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            string inputDir = "";
            string outputFile = "";
            bool Continue = InitProgram(ref inputDir, ref outputFile);
            if (!Continue)
                return;

            List <TestResult> results = new List<TestResult>();
            var FilePaths = Directory.GetFiles(inputDir).Select(Path.GetFileName);

            try
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("press any key to close");
                Console.Read();
                return;
            }

            //iterate over all files in dir
            //add file data to result set
            foreach (string s in FilePaths)
            {
                TestResult temp = new TestResult();
                temp.GenerateResultsFromJSON(inputDir + s);
                results.Add(temp);
            }

            //create list of lists grouped by the pageName [0]
            var groupedResults = results.GroupBy(i =>
                i.resultSet[0]
            ).Select(group => group.ToList())
            .ToList();

            //output results to csv file
            groupedResults.ForEach(g =>
            {
                try
                {
                    TestResult.WriteToCsv(g, outputFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("press any key to close");
                    Console.Read();
                }
            });

            Console.Beep();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Program Completed Successfully! {results.Count} files have been processed \nfor a total of {groupedResults.Count} pages. \nThe processed csv file can be found a {outputFile}");
            Console.WriteLine("Press any key to terminate this window");
            Console.Read();            
            return;
        }

        public static bool ValidateUserArgs(string[] args, ref string errMsg)
        {
            if (args.Length != 2)
            {
                errMsg = "Insufficent Arguments";
                return false;
            }
            if (!Directory.Exists(args[0])) 
            {
                errMsg = "Provided Directory Does Not Exist!";
                return false;
            }
            if (Directory.GetFiles(args[0]).Length < 1)
            {
                errMsg = "There are 0 Files in this directory!";
                return false;
            }
            if(args.Any( a => string.IsNullOrEmpty(a)))
            {
                errMsg = "One of your Inputs is an Empty String!";
                return false;
            }

            return true;
        }

        public static bool InitProgram(ref string inputDir, ref string outputFile)
        {
            List<string> arg = new List<string>();
            string errMsg = "";
            Console.WriteLine("Please Input Directory of Files for processing");
            inputDir = Console.ReadLine();
            arg.Add(inputDir);
            Console.WriteLine("Please Input The filepath for output");
            outputFile = Console.ReadLine();
            arg.Add(outputFile);

            if (!ValidateUserArgs(arg.ToArray(), ref errMsg))
            {
                Console.Beep();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("One of your inputs is invalid! Please check them and try again!\nPress Any Y to Try Again or X or Close the program");
                Console.WriteLine(errMsg);
                string x = Console.ReadLine();
                if (x == "y" || x == "Y")
                    Main(Array.Empty<string>());
                return false;
            }

            Console.WriteLine("Input Accepted! \nBeginning Processing.");
            return true;
        }
    }
}