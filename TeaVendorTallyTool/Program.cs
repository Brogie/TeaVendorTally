﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using RedditSharp;
using ConsoleProgressBar;

namespace TeaVendorTallyTool {

    internal class Program {

        private static void Main(string[] args) {
            string resultsFileLocation = string.Empty;
            string quotesFileLocation = "quotes.csv";

            //Process command line arguments
            if (args.Length == 1) {
                resultsFileLocation = args[0];
            } else {
                for (int i = 0; i < args.Length; i++) {
                    if (args[i].ToLower() == "-r") {
                        resultsFileLocation = args[i + 1];
                    } else if (args[i].ToLower() == "-q") {
                        quotesFileLocation = args[i + 1];
                    }
                }
            }

            if (resultsFileLocation != string.Empty && File.Exists(resultsFileLocation) && File.Exists(quotesFileLocation)) {
                List<Vendor> allVendors = new List<Vendor>();

                //Load in the vendor poll CSV and shop quotes
                ReadPollingData(allVendors, resultsFileLocation);
                MapQuoteData(allVendors, quotesFileLocation);

                //Sort the vendor lists first alphabetically, then by points so that drawn vendors are organised alphabetically
                allVendors = allVendors.OrderBy(Vendor => Vendor.VendorName).ToList();
                allVendors = allVendors.OrderByDescending(Vendor => Vendor.VendorPoints).ToList();

                WriteTable(allVendors);
            }
        }

        private static void MapQuoteData(List<Vendor> MapTo, string quoteFileLocation) {
            Dictionary<string, Tuple<string, string>> quotes = new Dictionary<string, Tuple<string, string>>();
            //Read in the quote csv
            using (var reader = new StreamReader(quoteFileLocation)) {
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var values = line.Split('|');

                    //the first column has markup denoting links, remove these to get just then vendor name to use as a key 
                    string key = Regex.Replace(values[0], @" ?\(.*?\)", string.Empty);
                    key = key.Replace("[", string.Empty);
                    key = key.Replace("]", string.Empty);
                    key = key.Trim();

                    //add the values to the quote list, the key is the vendor name, the [0] is the vendor name with link markup and [1] is the user/vendor quote
                    quotes.Add(key, new Tuple<string, string>(values[0].Trim(), values[1].Trim()));
                }
            }

            //Map the quotes from the quote csv with the vendors from the results csv using the vendor name to match with the quote key
            for (int i = 0; i < MapTo.Count; i++) {
                if (quotes.ContainsKey(MapTo[i].VendorName)) {
                    MapTo[i].VendorLinks = quotes[MapTo[i].VendorName].Item1;
                    MapTo[i].VendorQuote = quotes[MapTo[i].VendorName].Item2;
                } else {
                    Console.WriteLine(MapTo[i].VendorName + " Does not have a map, please manually add links and quote or fix the value in the quote file.");
                    MapTo[i].VendorLinks = "[" + MapTo[i].VendorName + "]";
                    MapTo[i].VendorQuote = "*QUOTE MISSING*";
                }
            }
        }

        private static void ReadPollingData(List<Vendor> vendors, string fileName) {
            using (var reader = new StreamReader(fileName)) {
                //Read header and format vendor names
                //Grabheader
                var line = reader.ReadLine();
                var values = line.Split(',');

                //start at 1 to remove "Timestamp" and "Username" column
                for (int i = 2; i < values.Length; i++) {
                    //Clean the name
                    var pattern = @"\[(.*?)\]";
                    string vendorName = Regex.Match(values[i], pattern).ToString();
                    vendorName = vendorName.Replace("[", string.Empty);
                    vendorName = vendorName.Replace("]", string.Empty);


                    Vendor temp = new Vendor();
                    temp.VendorName = vendorName;
                    temp.VendorPoints = 0;
                    vendors.Add(temp);
                }

                List<string[]> votes = new List<string[]>();
                List<string> usernames = new List<string>();
                List<string> bannedUsers = new List<string>();
                //validate one vote per user
                while (!reader.EndOfStream) {
                    values = reader.ReadLine().Split(',');
                    //add username to dictionary
                    if (!usernames.Contains(values[1].ToLower())) {
                        usernames.Add(values[1].ToLower());
                    } else {
                        if (!bannedUsers.Contains(values[1].ToLower())) { 
                            bannedUsers.Add(values[1].ToLower());
                        }
                    }
                    //add all votes at this point
                    votes.Add(values);
                }

                votes.RemoveAll(x => bannedUsers.Contains(x[1].ToLower()));



                using (var pb = new ProgressBar()) {
                    using (var p1 = pb.Progress.Fork(1, "Parsing votes")) {
                        //Read in results
                        for (int i = 0; i < votes.Count; i++) {
                            p1.Report((double)i / votes.Count, $"Vote: {i}/{votes.Count}");
                            Thread.Sleep(100);
                            
                            try {
                                //Validate user
                                var redditInterface = new Reddit();
                                var user = redditInterface.GetUser(votes[i][1]);
                                //if user doesn't have enough points or isn't old enough then don't add their results
                                if (user.Created < DateTimeOffset.Now.AddDays(7) && (user.CommentKarma + user.LinkKarma) < 50) {
                                    continue;
                                }
                            } catch {
                                continue;
                            }

                            //Add up all points awarded to the vendors
                            for (int j = 2; j < votes[i].Length; j++) {
                                switch (votes[i][j]) {
                                    case ""://empty value will be for 95% of the values so check this first for efficiency
                                        break;

                                    case "1st":
                                        vendors[j - 1].VendorPoints += 5;
                                        break;

                                    case "2nd":
                                        vendors[j - 1].VendorPoints += 4;
                                        break;

                                    case "3rd":
                                        vendors[j - 1].VendorPoints += 3;
                                        break;

                                    case "Runner-up 1":
                                        vendors[j - 1].VendorPoints += 1;
                                        break;

                                    case "Runner-up 2":
                                        vendors[j - 1].VendorPoints += 1;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void WriteTable(List<Vendor> vendors) {
            //Add the reddit markup table headers
            string votedTable = "Rank | Vendor/Website Link | Reddit User / Vendor Comments\n---------|---------|---------\n";
            string unvotedTable = "Vendor/Website Link | Reddit User / Vendor Comments\n---------|---------\n";

            int rankPosition = 1;
            bool draw = false;

            for (int i = 0; i < vendors.Count; i++) {
                //check if the ranks are drawn and act accordingly
                if (i > 0) {
                    if (vendors[i].VendorPoints == vendors[i - 1].VendorPoints) {
                        draw = true;
                    } else {
                        draw = false;
                        rankPosition++;
                    }
                }

                //write the tables, if a vendor has been awared no votes at all they are added to a seperate table named "additional vendor list" to avoid confusion 
                if (vendors[i].VendorPoints > 0) {
                    votedTable += string.Format("{0} | {1} | {2}\n", draw ? "-" : rankPosition.ToString(), vendors[i].VendorLinks, vendors[i].VendorQuote);
                } else {
                    unvotedTable += string.Format("{0} | {1}\n", vendors[i].VendorLinks, vendors[i].VendorQuote);
                }
            }

            //output to file with markup headers for each table section
            using (var writer = new StreamWriter("tables.txt")) {
                writer.WriteLine("#Vendor Lists");
                writer.Write("##User choice\n\n");
                writer.Write(votedTable);
                writer.WriteLine();
                writer.Write("##Additional Vendors\n\n");
                writer.Write(unvotedTable);
            }
        }
    }

    internal class Vendor : IComparable {
        public string VendorLinks;
        public string VendorName;
        public int VendorPoints;
        public string VendorQuote;

        public int CompareTo(Object obj) {
            Vendor compare = obj as Vendor;

            if (this.VendorPoints < compare.VendorPoints) {
                return 1;
            } else if (this.VendorPoints == compare.VendorPoints) {
                return 0;
            } else {
                return -1;
            }
        }
    }
}