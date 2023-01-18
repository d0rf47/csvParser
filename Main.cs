using System;
using System.Collections;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace perle.tech.benchmarking
{
    class Start
    {                
        static void Main(string[] args)
        {                        
            List<TestResult> results = new List<TestResult>();
            var FilePaths = Directory.GetFiles("./jsonFiles").Select(Path.GetFileName);
            if(File.Exists("./output.csv"))
                File.Delete("./output.csv");
            foreach(string s in FilePaths)
            {
                TestResult temp = new TestResult();
                temp.GenerateResultsFromJSON("./jsonFiles/" + s);
                //results.Print();
                temp.WriteToCsv();
                results.Add(temp);
            }
            var groupedResults = results.GroupBy(i =>
                i.resultSet["Page"]
            ).Select(group => group.ToList())
            .ToList();

            groupedResults.ForEach(g =>
            {
                //WriteToCsv(g);
            });
        }

        public static void WriteToCsv(List<TestResult> testResultList)
        {
            //second check is to ensure we append to the file on loop and not add extra headers and sep command
            bool exists = (File.Exists("./output.csv"));
            using (var writer = new StreamWriter("./output.csv", true))
            {
                if(!exists)
                {                                     
                    writer.WriteLine("sep=|");
                    string header = "Property";
                    testResultList.ForEach(t => header += "|Value");
                    writer.WriteLine(header);
                }
                if(exists)
                    writer.WriteLine();        
                foreach(var rs in testResultList)
                {
                    //write everything not in a list   
                }
            }
                
        }
    }
}