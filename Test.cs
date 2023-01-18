using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;


namespace perle.tech.benchmarking
{
    class TestResult
    {
        public Dictionary<string, dynamic?>? resultSet {get;set;}

        public TestResult()
        {
            resultSet = new Dictionary<string, dynamic?>();
        }

        public void GenerateResultsFromJSON(string JsonFilePath)
        {            
            string jsonString = File.ReadAllText(JsonFilePath);
            JObject jsonsData = JObject.Parse(jsonString);
            resultSet.Add("Page", jsonsData.SelectToken("data.url"));
            resultSet.Add("Test Location", jsonsData.SelectToken("data.from"));
            resultSet.Add("loadTime", jsonsData.SelectToken("data.average.firstView.loadTime"));
            resultSet.Add("First Contentful Paint", jsonsData.SelectToken("data.average.firstView.firstContentfulPaint"));            
            resultSet.Add("Largest Contentful Paint", jsonsData.SelectToken("data.average.firstView['chromeUserTiming.LargestContentfulPaint']"));
            resultSet.Add("Cumulative Layout Shift", jsonsData.SelectToken("data.average.firstView['chromeUserTiming.CumulativeLayoutShift']"));
            resultSet.Add("Time To First Byte(TTFB)", jsonsData.SelectToken("data.average.firstView.TTFB"));
            resultSet.Add("Time To Interactive", jsonsData.SelectToken("data.average.firstView.TimeToInteractive"));
            resultSet.Add("Speed Index", jsonsData.SelectToken("data.average.firstView.TTFB"));
            resultSet.Add("CPU Load", jsonsData.SelectToken("data.average.firstView.fullyLoadedCPUpct"));
            resultSet.Add("Total Blocking Time", jsonsData.SelectToken("data.average.firstView.firstContentfulPaint"));
            // resultSet.Add("Blocking CSS", jsonsData.SelectToken("data.average.firstView.renderBlockingCSS"));
            // resultSet.Add("Blocking JS", jsonsData.SelectToken("data.average.firstView.renderBlockingJS"));            
            List<AccessibilityViolations> violations = new List<AccessibilityViolations>();
            // need to add list for audit_issues
            foreach(var s in jsonsData.SelectToken("data.median.firstView.axe.violations"))
            {                
                AccessibilityViolations temp =  new AccessibilityViolations();
                temp.Violation.Add(new KeyValuePair<string, string>("ID", s.SelectToken("id").ToString()));
                temp.Violation.Add(new KeyValuePair<string, string>("IMPACT", s.SelectToken("impact").ToString()));
                temp.Violation.Add(new KeyValuePair<string, string>("DESC", s.SelectToken("description").ToString()));
                temp.Violation.Add(new KeyValuePair<string, string>("HELP", s.SelectToken("help").ToString()));
                    
                foreach(var node in s.SelectToken("nodes"))
                {
                    string elemString = Regex.Unescape(node.SelectToken("target").ToString());
                    elemString = Regex.Replace((elemString), @"\t|\n|\r", "");     
                    elemString = elemString.Substring(1, elemString.Length - 2);
                    temp.Elements.Add(elemString.Trim().Replace("\"", ""));
                }
                violations.Add(temp);
            }
            //add resource blocking list here
            List<string> blockingFiles =  new List<string>();
            foreach(var s in jsonsData.SelectToken("data.median.firstView.requests"))
            {
                if( s.SelectToken("host").ToString().Contains("www.perle")
                && (s.SelectToken("url").ToString().Contains(".css") ||  s.SelectToken("url").ToString().Contains(".js") )
                && s.SelectToken("renderBlocking").ToString().Contains("blocking") )
                {
                    blockingFiles.Add(s.SelectToken("url").ToString());
                }
            }
            resultSet.Add("Render Blocking Resources", blockingFiles.ConvertAll(s=> s.ToLower()).Distinct().ToList());            
            resultSet.Add("Accessibility Failures", violations);            
        }

        public void Print()
        {
            resultSet.Where( i => i.Key != "Accessibility Failures")
            .ToList()
            .ForEach(i => Console.WriteLine($"{i.Key}: {i.Value}"));
            
            Console.WriteLine();    
            
            resultSet.Where( i => i.Key == "Accessibility Failures").ToList()
            .ForEach(i => {Console.WriteLine(i.Key); foreach(AccessibilityViolations v in i.Value)
            {                
                v.Violation.ForEach(i =>
                {
                    Console.WriteLine($"{i.Key}: {i.Value}");
                });                                
                Console.WriteLine("AFFECTED ELEMENTS \n");
                v.Elements.ForEach(e => Console.WriteLine(e));
                Console.WriteLine();
            }});
        }

        public void WriteToCsv()
        {
            //second check is to ensure we append to the file on loop and not add extra headers and sep command
            bool exists = (File.Exists("./output.csv"));
            using (var writer = new StreamWriter("./output.csv", true))
            {
                if(!exists)
                {                                     
                    writer.WriteLine("sep=|");
                    writer.WriteLine("Property|Value");
                }
                if(exists)
                    writer.WriteLine();                
                foreach(var r in resultSet)
                {
                    if(r.Value is not IList) 
                        writer.WriteLine("{0}|{1}", r.Key, r.Value);
                    else
                    {
                        if(r.Value is IList<AccessibilityViolations>)
                        {
                            writer.WriteLine();
                            writer.WriteLine("{0}",r.Key);
                            foreach(var item in r.Value)
                            {
                                foreach(var val in item.Violation)
                                    writer.WriteLine("{0}|{1}", val.Key, val.Value);
                                writer.WriteLine("Affected Elements");
                                foreach(var elem in item.Elements)
                                    writer.WriteLine("{0}", elem);
                                writer.WriteLine();
                            }
                        }else
                        {                            
                            writer.WriteLine("{0}",r.Key);
                            foreach(string s in r.Value)
                            {
                                writer.WriteLine("{0}", s);
                            }
                        }
                    }
                }
            }
        }        
    }

    

    class AccessibilityViolations
    {
        public List<KeyValuePair<string, string>> Violation {get;set;}        
        public List<string> Elements {get;set;} 

        public AccessibilityViolations()
        {
            Elements = new List<string>();
            Violation = new List<KeyValuePair<string, string>>();
        }        
    }




}
