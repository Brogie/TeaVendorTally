using System;
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

                ReadVendors(allVendors, quotesFileLocation);

                //Load in the vendor poll CSV and shop quotes
                ReadPollingData(allVendors, resultsFileLocation);
                //MapQuoteData(allVendors, quotesFileLocation);

                //Sort the vendor lists first alphabetically, then by points so that drawn vendors are organised alphabetically
                allVendors = allVendors.OrderBy(Vendor => Vendor.Name).ToList();
                allVendors = allVendors.OrderByDescending(Vendor => Vendor.Points).ToList();

                WriteTable(allVendors);
            }
        }

        private static void ReadPollingData(List<Vendor> vendors, string fileName) {
            // make a name searchable vendor list
            Dictionary<string, Vendor> VendorDict = new Dictionary<string, Vendor>();
            foreach (var v in vendors) {
                VendorDict.Add(v.Name, v);
            }

            using (var reader = new StreamReader(fileName)) {
                //Process votes 
                using (var pb = new ProgressBar()) {
                    //Read and verify votes
                    List<string[]> votes;
                    using (var p1 = pb.Progress.Fork(1)) {
                        votes = VerifyVotes(reader, p1, vendors);
                    }

                    List<string> unmatched = new List<string>();

                    using (var p1 = pb.Progress.Fork(1, "Parsing votes")) {
                        //peform on each vote
                        for (int i = 0; i < votes.Count; i++) {
                            p1.Report((double)i / votes.Count, $"Vote: {i}/{votes.Count}");
                            Thread.Sleep(10);

                            //1st
                            if (VendorDict.ContainsKey(votes[i][2])) {
                                VendorDict[votes[i][2]].Points += 5;
                            } else {
                                unmatched.Add(votes[i][2]);
                            }
                            //2nd
                            if (VendorDict.ContainsKey(votes[i][3])) {
                                VendorDict[votes[i][3]].Points += 4;
                            } else {
                                unmatched.Add(votes[i][3]);
                            }
                            //3rd
                            if (VendorDict.ContainsKey(votes[i][4])) {
                                VendorDict[votes[i][4]].Points += 3;
                            } else {
                                unmatched.Add(votes[i][4]);
                            }
                            //run 1
                            if (VendorDict.ContainsKey(votes[i][5])) {
                                VendorDict[votes[i][5]].Points += 1;
                            } else {
                                unmatched.Add(votes[i][5]);
                            }
                            //run 2
                            if (VendorDict.ContainsKey(votes[i][6])) {
                                VendorDict[votes[i][6]].Points += 1;
                            } else {
                                unmatched.Add(votes[i][6]);
                            }
                        }

                        foreach (var u in unmatched) {
                            if (u != "") {
                                Console.WriteLine("Unmatched vote: " + u);
                            }
                        }
                    }
                }
            }
        }

        private static void ReadVendors(List<Vendor> vendors, string FileName) {
            using (var reader = new StreamReader(FileName)) {
                while (!reader.EndOfStream) {
                    //Grab vendor
                    var line = reader.ReadLine();
                    var values = line.Split('|');

                    //get name
                    string name = Regex.Replace(values[0], @" ?\(.*?\)", string.Empty);
                    name = name.Replace("[", string.Empty);
                    name = name.Replace("]", string.Empty);
                    name = name.Trim();

                    Vendor temp = new Vendor {
                        Name = name,
                        Links = values[0].Trim(),
                        Quote = values[1].Trim(),
                        Origin = values[2].Trim(),
                        Range = values[3].Trim(),
                        Points = 0
                    };
                    vendors.Add(temp);
                }
            }
        }

        private static List<string[]> VerifyVotes(StreamReader reader, Progress prog, List<Vendor> vendors) {
            List<string[]> votes = new List<string[]>();
            List<string> usernames = new List<string>();
            List<string> bannedUsers = new List<string>();
            Dictionary<string, int> voteCheck = new Dictionary<string, int>();
            List<string> multipleBan = new List<string>(), notValidBan = new List<string>(), tooYoungBan = new List<string>(), fewKarmaBan = new List<string>();

            string[] file = reader.ReadToEnd().Split('\n');

            for (int i = 1; i < file.Length; i++) {
                //validate one vote per user
                var values = file[i].Trim().Split(',');

                //report progress start
                prog.Report((double)i / file.Length, $"Verifying: {values[1]} {i}/{file.Length}");

                //VOTE CHECK - add similar votes together to see vote stuffing
                string first = "N/A", second = "N/A", third = "N/A", run1 = "N/A", run2 = "N/A";

                if (values[2].Trim() != "") {
                    first = values[2];
                }
                if (values[3].Trim() != "") {
                    second = values[3];
                }
                if (values[4].Trim() != "") {
                    third = values[4];
                }
                if (values[5].Trim() != "") {
                    run1 = values[5];
                }
                if (values[6].Trim() != "") {
                    run2 = values[6];
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

                ////REDDIT CHECK - username must be valid reddit user that is older than 7 days and has karma
                //try {
                //    //Validate user
                //    var redditInterface = new Reddit();
                //    var user = redditInterface.GetUser(values[1]);
                //    //if user doesn't have enough points or isn't old enough then don't add their results
                    
                //    if (user.Created.Date > DateTimeOffset.Now.AddDays(-7).Date || (user.CommentKarma + user.LinkKarma) < 50) {
                //        if (!bannedUsers.Contains(values[1].ToLower())) {
                //            bannedUsers.Add(values[1].ToLower());
                //        }

                //        if (user.Created.Date > DateTimeOffset.Now.AddDays(-7).Date && !tooYoungBan.Contains(values[1])) {
                //            tooYoungBan.Add(values[1]);
                //        }

                //        if ((user.CommentKarma + user.LinkKarma) < 50 && !fewKarmaBan.Contains(values[1])) {
                //            fewKarmaBan.Add(values[1]);
                //        }
                //    } 
                //} catch {//will catch when user doesn't exist (error 404)
                //    if (!bannedUsers.Contains(values[1].ToLower())) {
                //        bannedUsers.Add(values[1].ToLower());
                //    }

                //    if (!notValidBan.Contains(values[1])) {
                //        notValidBan.Add(values[1]);
                //    }
                //}
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
                writer.WriteLine("\n");

                //Karma section
                writer.WriteLine("-Under Karma Limit-");
                foreach (var user in fewKarmaBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine("\n");

                //Age section
                writer.WriteLine("-Under account age limit-");
                foreach (var user in tooYoungBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine("\n");

                //Invalid ban
                writer.WriteLine("-Username isn't a valid reddit account-");
                foreach (var user in notValidBan) {
                    writer.Write(user + ", ");
                }
                writer.WriteLine("\n");

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
            string votedTable = "Rank | Vendor/Website Link | Reddit User / Vendor Comments | Shipping Origin | Shipping Range\n---------|---------|---------|---------|---------\n";
            string unvotedTable = "Vendor/Website Link | Reddit User / Vendor Comments | Shipping Origin | Shipping Range\n---------|---------|---------|---------\n";

            int rankPosition = 1;
            bool draw = false;

            for (int i = 0; i < vendors.Count; i++) {
                //check if the ranks are drawn and act accordingly
                if (i > 0) {
                    if (vendors[i].Points == vendors[i - 1].Points) {
                        draw = true;
                    } else {
                        draw = false;
                        rankPosition++;
                    }
                }

                //write the tables, if a vendor has been awared no votes at all they are added to a seperate table named "additional vendor list" to avoid confusion 
                if (vendors[i].Points > 0) {
                    votedTable += string.Format("{0} | {1} | {2} | {3} | {4}\n", draw ? "-" : rankPosition.ToString(), vendors[i].Links, vendors[i].Quote, vendors[i].Origin, vendors[i].Range);
                } else {
                    unvotedTable += string.Format("{0} | {1} | {2} | {3}\n", vendors[i].Links, vendors[i].Quote, vendors[i].Origin, vendors[i].Range);
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
        private string links;
        private string name;
        private int points;
        private string quote;
        private string origin;
        private string range;

        public int Points { get => points; set => points = value; }
        public string Name { get => name; set => name = value; }
        public string Links { get => links; set => links = value; }
        public string Quote { get => quote; set => quote = value; }
        public string Origin { get => origin; set => origin = value; }
        public string Range { get => range; set => range = value; }

        public int CompareTo(Object obj) {
            Vendor compare = obj as Vendor;

            if (this.Points < compare.Points) {
                return 1;
            } else if (this.Points == compare.Points) {
                return 0;
            } else {
                return -1;
            }
        }
    }
}