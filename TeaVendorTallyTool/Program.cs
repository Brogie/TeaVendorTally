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
                //Process votes 
                using (var pb = new ProgressBar()) {
                    //Read vendor names from the CSV header
                    ReadVendors(vendors, reader);

                    //Read and verify votes
                    List<string[]> votes;
                    using (var p1 = pb.Progress.Fork(1)) {
                        votes = VerifyVotes(reader, p1, vendors);
                    }

                    using (var p1 = pb.Progress.Fork(1, "Parsing votes")) {
                        //peform on each vote
                        for (int i = 0; i < votes.Count; i++) {
                            p1.Report((double)i / votes.Count, $"Vote: {i}/{votes.Count}");
                            Thread.Sleep(10);

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

        private static void ReadVendors(List<Vendor> vendors, StreamReader reader) {
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
        }

        private static List<string[]> VerifyVotes(StreamReader reader, Progress prog, List<Vendor> vendors) {
            List<string[]> votes = new List<string[]>();
            List<string> usernames = new List<string>();
            List<string> bannedUsers = new List<string>();
            Dictionary<string, int> voteCheck = new Dictionary<string, int>();
            List<string> multipleBan = new List<string>(), notValidBan = new List<string>(), tooYoungBan = new List<string>(), fewKarmaBan = new List<string>();

            string[] file = reader.ReadToEnd().Split('\n');

            for (int i = 0; i < file.Length; i++) {
                //validate one vote per user
                var values = file[i].Trim().Split(',');

                //report progress start
                prog.Report((double)i / file.Length, $"Verifying: {values[1]} {i}/{file.Length}");

                //VOTE CHECK - add similar votes together to see vote stuffing
                string first = "N/A", second = "N/A", third = "N/A", run1 = "N/A", run2 = "N/A";

                //get veNdor names to build key
                for (int j = 2; j < values.Length; j++) {
                    switch (values[j]) {
                        case ""://empty value will be for 95% of the values so check this first for efficiency
                            break;

                        case "1st":
                            first = vendors[j].VendorName;
                            break;

                        case "2nd":
                            second = vendors[j].VendorName;
                            break;

                        case "3rd":
                            third = vendors[j].VendorName;
                            break;

                        case "Runner-up 1":
                            run1 = vendors[j].VendorName;
                            break;

                        case "Runner-up 2":
                            run2 = vendors[j].VendorName;
                            break;
                    }
                }

                var key = $"1st: {first}, 2nd: {second}, 3rd: {third}, Runner-up 1: {run1}, Runner-up 2: {run2}";

                if (!voteCheck.ContainsKey(key)) {
                    voteCheck.Add(key, 1);
                } else {
                    voteCheck[key]++;
                }

                //USERNAME CHECK - one vote per user, band users having multiple votes
                if (!usernames.Contains(values[1].ToLower())) {
                    usernames.Add(values[1].ToLower());
                } else {
                    if (!bannedUsers.Contains(values[1].ToLower())) {
                        bannedUsers.Add(values[1].ToLower());
                    }

                    if (!multipleBan.Contains(values[1])) {
                        multipleBan.Add(values[1]);
                    }
                }

                //REDDIT CHECK - username must be valid reddit user that is older than 7 days and has karma
                try {
                    //Validate user
                    var redditInterface = new Reddit();
                    var user = redditInterface.GetUser(values[1]);
                    //if user doesn't have enough points or isn't old enough then don't add their results
                    if (user.Created < DateTimeOffset.Now.AddDays(7) && (user.CommentKarma + user.LinkKarma) < 50) {
                        if (!bannedUsers.Contains(values[1].ToLower())) {
                            bannedUsers.Add(values[1].ToLower());
                        }

                        if (user.Created < DateTimeOffset.Now.AddDays(7) && !tooYoungBan.Contains(values[1])) {
                            tooYoungBan.Add(values[1]);
                        }

                        if ((user.CommentKarma + user.LinkKarma) < 50 && !fewKarmaBan.Contains(values[1])) {
                            fewKarmaBan.Add(values[1]);
                        }
                    } 
                } catch {//will catch when user doesn't exist (error 404)
                    if (!bannedUsers.Contains(values[1].ToLower())) {
                        bannedUsers.Add(values[1].ToLower());
                    }

                    if (!notValidBan.Contains(values[1])) {
                        notValidBan.Add(values[1]);
                    }
                }
                //add all votes at this point
                votes.Add(values);
            }

            //remove votes from banned users
            votes.RemoveAll(x => bannedUsers.Contains(x[1].ToLower()));

            //output vote report
            using (var writer = new StreamWriter("VoteReport.txt")) {
                //Banned Users heading
                writer.WriteLine("[BANNED USERS]");
                //multiple vote section
                writer.WriteLine("-Multiple Votes-");
                foreach (var user in multipleBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine();

                //Karma section
                writer.WriteLine("-Under Karma Limit-");
                foreach (var user in fewKarmaBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine();

                //Age section
                writer.WriteLine("-Under account age limit-");
                foreach (var user in tooYoungBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine();

                //Invalid ban
                writer.WriteLine("-Username isn't a valid reddit account-");
                foreach (var user in notValidBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine();

                //Vote heading
                writer.WriteLine("[COMMON VOTES]");
                //sort vote counts
                var sortableVoteCheck = voteCheck.ToList();
                sortableVoteCheck.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

                foreach (var vote in sortableVoteCheck) {
                    writer.WriteLine("Voted " + vote.Value + " times: " + vote.Key);
                }
            }

            return votes;
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