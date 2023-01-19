using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;

namespace perle.tech.benchmarking
{
    class Start
    {                
        static void Main(string[] args)
        {                        
            List<TestResult> results = new List<TestResult>();
            var FilePaths = Directory.GetFiles("./jsonFiles").Select(Path.GetFileName);
            try{
                if(File.Exists("./output.csv"))
                    File.Delete("./output.csv");
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            
            foreach(string s in FilePaths)
            {
                TestResult temp = new TestResult();
                temp.GenerateResultsFromJSON("./jsonFiles/" + s);
                //results.Print();
                //temp.WriteToCsv();
                results.Add(temp);
            }
            var groupedResults = results.GroupBy(i =>
                i.resultSet[0]
            ).Select(group => group.ToList())
            .ToList();

            groupedResults.ForEach(g =>
            {
                try{
                    WriteToCsv(g);
                }catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            });
        }

        public static void WriteToCsv(List<TestResult> testResultList)
        {
            //second check is to ensure we append to the file on loop and not add extra headers and sep command
            bool exists = (File.Exists("./output.csv"));
            string rowText = "";
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
                
                //Write all rows for each common page that does not use a list
                for(int j = 0; j < testResultList[0].resultSet.Count; j++)
                {
                    if(testResultList[0].resultSet[j].Value is not IList)
                    {
                        rowText = testResultList[0].resultSet[j].Key + "|";
                        for(int i = 0; i < testResultList.Count; i++)
                        {
                            rowText += testResultList[i].resultSet[j].Value + "|";
                        }    
                        writer.WriteLine(rowText);
                    }else if(testResultList[0].resultSet[j].Key == "Accessibility Failures")
                    {
                        
                        rowText = "";
                        for(int k = 0; k < testResultList[0].resultSet[j].Value.Count;k++)
                        {
                            for(int l = 0; l < testResultList[0].resultSet[j].Value[k].Violation.Count; l++)
                            {
                                rowText = testResultList[0].resultSet[j].Value[k].Violation[l].Key + "|";                                
                                for(int i = 0; i < testResultList.Count; i++)
                                {
                                    rowText +=testResultList[0].resultSet[j].Value[k].Violation[l].Value + "|";
                                }   
                                writer.WriteLine(rowText); 
                            }
                            
                        }
                        
                    }
                    
                }
                
            }
                
        }
    }
}