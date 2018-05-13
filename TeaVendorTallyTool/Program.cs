using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TeaVendorTallyTool {

    internal class Program {

        private static void Main(string[] args) {
            string resultsFileLocation = string.Empty;
            string quotesFileLocation = "quotes.csv";

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

                //Sort the vendor lists
                allVendors = allVendors.OrderBy(Vendor => Vendor.VendorName).ToList();
                allVendors = allVendors.OrderByDescending(Vendor => Vendor.VendorPoints).ToList();

                writeTable(allVendors);
            }

        }

        private static void MapQuoteData(List<Vendor> MapTo, string quoteFileLocation) {
            Dictionary<string, Tuple<string,string>> quotes = new Dictionary<string, Tuple<string, string>>();
            using (var reader = new StreamReader(quoteFileLocation)) {
                //Read in results
                while (!reader.EndOfStream) {
                    var line = reader.ReadLine();
                    var values = line.Split('|');

                    string key = Regex.Replace(values[0], @" ?\(.*?\)", string.Empty);
                    key = key.Replace("[", string.Empty);
                    key = key.Replace("]", string.Empty);
                    key = key.Trim();

                    quotes.Add(key, new Tuple<string, string>(values[0].Trim(), values[1].Trim()));
                }
            }

            for (int i = 0; i < MapTo.Count; i++) {
                if (quotes.ContainsKey(MapTo[i].VendorName)) {
                    MapTo[i].VendorLinks = quotes[MapTo[i].VendorName].Item1;
                    MapTo[i].VendorQuote = quotes[MapTo[i].VendorName].Item2;
                } else if (MapTo[i].VendorName == "Tea Gschwendner") {//dirty but no easy way around with the dataset
                    MapTo[i].VendorLinks = "[Tea Gschwendner](http://shop.tgtea.com/) also [German site](http://www.teegschwendner.de/)";
                    MapTo[i].VendorQuote = "\"They are a German company with physical retail locations in Chicago, IL, and around the world. ... I find their loose leaf teas to be of higher quality than other tea shops with retail locations in Chicago... while being similar in price. They sell flavored teas, unflavored teas, rooibos and herbal.\"";
                } else {
                    Console.WriteLine(MapTo[i].VendorName + " Does not have a map, please manually add links and quote");
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

                //start at 1 to remove "Timestamp" column
                for (int i = 1; i < values.Length; i++) {
                    //Clean the name
                    string vendorName = values[i].Remove(values[i].Length - 1);
                    vendorName = vendorName.Remove(0, 41);

                    Vendor temp = new Vendor();
                    temp.VendorName = vendorName;
                    temp.VendorPoints = 0;
                    vendors.Add(temp);
                }

                //Read in results
                while (!reader.EndOfStream) {
                    line = reader.ReadLine();
                    values = line.Split(',');

                    for (int i = 1; i < values.Length; i++) {
                        switch (values[i]) {
                            case "":
                                break;

                            case "1st":
                                vendors[i - 1].VendorPoints += 5;
                                break;

                            case "2nd":
                                vendors[i - 1].VendorPoints += 4;
                                break;

                            case "3rd":
                                vendors[i - 1].VendorPoints += 3;
                                break;

                            case "Runner-up 1":
                                vendors[i - 1].VendorPoints += 1;
                                break;

                            case "Runner-up 2":
                                vendors[i - 1].VendorPoints += 1;
                                break;
                        }
                    }
                }
            }
        }

        private static void writeTable(List<Vendor> vendors) {
            string votedTable = "Rank | Vendor/Website Link | Reddit User / Vendor Comments\n---------|---------|---------\n";
            string unvotedTable = "Vendor/Website Link | Reddit User / Vendor Comments\n---------|---------\n";

            for (int i = 0; i < vendors.Count; i++) {
                if (vendors[i].VendorPoints > 0) {
                    votedTable += string.Format("{0} | {1} | {2}\n", i + 1, vendors[i].VendorLinks, vendors[i].VendorQuote);
                } else {
                    unvotedTable += string.Format("{0} | {1}\n", vendors[i].VendorLinks, vendors[i].VendorQuote);
                }
            }

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