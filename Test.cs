using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;


namespace perle.tech.benchmarking
{
    class TestResult
    {
        public List<KeyValuePair<string, dynamic?>> resultSet { get; set; }

        public TestResult()
        {
            resultSet = new List<KeyValuePair<string, dynamic?>>();
        }

        public void GenerateResultsFromJSON(string JsonFilePath)
        {
            string jsonString = File.ReadAllText(JsonFilePath);
            JObject jsonsData = JObject.Parse(jsonString);
            resultSet.Add(new KeyValuePair<string, dynamic?>("Page", jsonsData.SelectToken("data.url")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Test Location", jsonsData.SelectToken("data.from")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("loadTime", jsonsData.SelectToken("data.average.firstView.loadTime")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("First Contentful Paint", jsonsData.SelectToken("data.average.firstView.firstContentfulPaint")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Largest Contentful Paint", jsonsData.SelectToken("data.average.firstView['chromeUserTiming.LargestContentfulPaint']")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Cumulative Layout Shift", jsonsData.SelectToken("data.average.firstView['chromeUserTiming.CumulativeLayoutShift']")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Time To First Byte(TTFB)", jsonsData.SelectToken("data.average.firstView.TTFB")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Time To Interactive", jsonsData.SelectToken("data.average.firstView.TimeToInteractive")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Speed Index", jsonsData.SelectToken("data.average.firstView.TTFB")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("CPU Load", jsonsData.SelectToken("data.average.firstView.fullyLoadedCPUpct")));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Total Blocking Time", jsonsData.SelectToken("data.average.firstView.firstContentfulPaint")));

            List<AccessibilityViolations> violations = new List<AccessibilityViolations>();

            foreach (var s in jsonsData.SelectToken("data.median.firstView.axe.violations"))
            {
                AccessibilityViolations temp = new AccessibilityViolations();
                temp.Violation.Add(new KeyValuePair<string, string>("ID", s.SelectToken("id").ToString()));
                temp.Violation.Add(new KeyValuePair<string, string>("IMPACT", s.SelectToken("impact").ToString()));
                temp.Violation.Add(new KeyValuePair<string, string>("DESC", s.SelectToken("description").ToString()));
                temp.Violation.Add(new KeyValuePair<string, string>("HELP", s.SelectToken("help").ToString()));

                foreach (var node in s.SelectToken("nodes"))
                {
                    string elemString = Regex.Unescape(node.SelectToken("target").ToString());
                    elemString = Regex.Replace((elemString), @"\t|\n|\r", "");
                    elemString = elemString.Substring(1, elemString.Length - 2);
                    temp.Elements.Add(elemString.Trim().Replace("\"", ""));
                }
                violations.Add(temp);
            }
            //add resource blocking list here
            List<string> blockingFiles = new List<string>();
            foreach (var s in jsonsData.SelectToken("data.median.firstView.requests"))
            {
                if (s.SelectToken("host").ToString().Contains("www.perle")
                && (s.SelectToken("url").ToString().Contains(".css") || s.SelectToken("url").ToString().Contains(".js"))
                && s.SelectToken("renderBlocking").ToString().Contains("blocking"))
                {
                    blockingFiles.Add(s.SelectToken("url").ToString());
                }
            }
            resultSet.Add(new KeyValuePair<string, dynamic?>("Render Blocking Resources", blockingFiles.ConvertAll(s => s.ToLower()).Distinct().ToList()));
            resultSet.Add(new KeyValuePair<string, dynamic?>("Accessibility Failures", violations));
        }

        public void Print()
        {
            resultSet.Where(i => i.Key != "Accessibility Failures")
            .ToList()
            .ForEach(i => Console.WriteLine($"{i.Key}: {i.Value}"));

            Console.WriteLine();

            resultSet.Where(i => i.Key == "Accessibility Failures").ToList()
            .ForEach(i => {
                Console.WriteLine(i.Key); foreach (AccessibilityViolations v in i.Value)
                {
                    v.Violation.ForEach(i =>
                    {
                        Console.WriteLine($"{i.Key}: {i.Value}");
                    });
                    Console.WriteLine("AFFECTED ELEMENTS \n");
                    v.Elements.ForEach(e => Console.WriteLine(e));
                    Console.WriteLine();
                }
            });
        }

        public void WriteToCsv()
        {
            //second check is to ensure we append to the file on loop and not add extra headers and sep command
            bool exists = (File.Exists("./output.csv"));
            using (var writer = new StreamWriter("./output.csv", true))
            {
                if (!exists)
                {
                    writer.WriteLine("sep=|");
                    writer.WriteLine("Property|Value");
                }
                if (exists)
                    writer.WriteLine();
                foreach (var r in resultSet)
                {
                    if (r.Value is not IList)
                        writer.WriteLine("{0}|{1}", r.Key, r.Value);
                    else
                    {
                        if (r.Value is IList<AccessibilityViolations>)
                        {
                            writer.WriteLine();
                            writer.WriteLine("{0}", r.Key);
                            foreach (var item in r.Value)
                            {
                                foreach (var val in item.Violation)
                                    writer.WriteLine("{0}|{1}", val.Key, val.Value);
                                writer.WriteLine("Affected Elements");
                                foreach (var elem in item.Elements)
                                    writer.WriteLine("{0}", elem);
                                writer.WriteLine();
                            }
                        }
                        else
                        {
                            writer.WriteLine("{0}", r.Key);
                            foreach (string s in r.Value)
                            {
                                writer.WriteLine("{0}", s);
                            }
                        }
                    }
                }
            }
        }

        public static void WriteToCsv(List<TestResult> testResultList, string outputFileName)
        {
            //second check is to ensure we append to the file on loop and not add extra headers and sep command
            bool exists = (File.Exists(outputFileName));
            string rowText = "";
            try
            {
                using (var writer = new StreamWriter(outputFileName, true))
                {
                    
                        if (!exists)
                        {
                            writer.WriteLine("sep=|");
                            string header = "Property";
                            testResultList.ForEach(t => header += "|Value");
                            writer.WriteLine(header);
                        }
                        if (exists)
                            writer.WriteLine();
                    
                    //Write all rows for each common page that does not use a list
                    for (int j = 0; j < testResultList[0].resultSet.Count; j++)
                    {
                        if (testResultList[0].resultSet[j].Value is not IList)
                        {
                            rowText = testResultList[0].resultSet[j].Key + "|";
                            for (int i = 0; i < testResultList.Count; i++)
                            {
                                rowText += testResultList[i].resultSet[j].Value + "|";
                            }
                            writer.WriteLine(rowText);
                        }
                        else if (testResultList[0].resultSet[j].Key == "Accessibility Failures")
                        {
                            rowText = "";
                            for (int k = 0; k < testResultList[0].resultSet[j].Value.Count; k++)
                            {
                                for (int l = 0; l < testResultList[0].resultSet[j].Value[k].Violation.Count; l++)
                                {
                                    rowText = testResultList[0].resultSet[j].Value[k].Violation[l].Key + "|";
                                    for (int i = 0; i < testResultList.Count; i++)
                                    {
                                        rowText += testResultList[0].resultSet[j].Value[k].Violation[l].Value + "|";
                                    }
                                    writer.WriteLine(rowText);
                                }
                                writer.WriteLine();
                            }
                        }
                        else if (testResultList[0].resultSet[j].Key == "Render Blocking Resources")
                        {
                            rowText = "Render Blocking Resources";
                            writer.WriteLine(rowText);
                            for (int k = 0; k < testResultList[0].resultSet[j].Value.Count; k++)
                            {
                                rowText = testResultList[0].resultSet[j].Value[k];
                                writer.WriteLine(rowText);
                            }
                            writer.WriteLine();
                        }
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("press any key to close");
                Console.Read();
            }
        }
    }

    class AccessibilityViolations
    {
        public List<KeyValuePair<string, string>> Violation { get; set; }
        public List<string> Elements { get; set; }

        public AccessibilityViolations()
        {
            Elements = new List<string>();
            Violation = new List<KeyValuePair<string, string>>();
        }
    }
}
