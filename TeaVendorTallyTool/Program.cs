using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using RedditSharp;
using ShellProgressBar;

namespace TeaVendorTallyTool {

    internal class Program {
        public enum State {
            MAINMENU,
            LOADSETTINGS,
            SETTINGS,
            RUN,
            EXIT
        };

        private struct Settings {
            public string VendorFileLocation { get; set; }
            public string VoteFileLocation { get; set; }
            public string OldOrderFileLocation { get; set; }
            public bool RankChangeColumn { get; set; }
            public bool Verify { get; set; }
            public bool RedditVerify { get; set; }
            public bool RegionalTable { get; set; }
            public bool Weighted { get; set; }
        }

        private static void Main(string[] args) {
            if (!Directory.Exists("files")) {
                Directory.CreateDirectory("files");
            }

            State progState = State.LOADSETTINGS;
            Settings progSettings = new Settings();

            //State machine
            while (progState != State.EXIT) {
                Console.Clear();
                switch (progState) {
                    case State.MAINMENU:
                        UserInput.Header("Main Menu");
                        progState = MainMenuSelector();
                        break;
                    case State.LOADSETTINGS:
                        LoadSettings(ref progSettings);
                        progState = State.MAINMENU;
                        break;
                    case State.SETTINGS:
                        UserInput.Header("Settings");
                        EditSettings(ref progSettings, ref progState);
                        break;
                    case State.RUN:
                        UserInput.Header("Running");
                        RunProgram(progSettings);
                        Console.ReadKey();
                        progState = State.MAINMENU;
                        break;
                    case State.EXIT:
                        break;
                    default:
                        break;
                }
            }
        }

        #region MenuOptions
        static private State MainMenuSelector() {
            State output = State.MAINMENU;
            string[] options = new string[] { "Run", "Settings", "Exit" };

            //Options menu
            switch (options[UserInput.SelectionMenu(options)]) {
                case "Run":
                    output = State.RUN;
                    break;
                case "Settings":
                    output = State.SETTINGS;
                    break;
                case "Exit":
                    output = State.EXIT;
                    break;

                default:
                    break;
            }

            return output;
        }

        static private void LoadSettings(ref Settings settings) {
            if (!File.Exists("Settings.txt")) {
                using (var writer = new StreamWriter("Settings.txt")) {
                    writer.WriteLine("Vendor File Location = ");
                    writer.WriteLine("Vote File Location = ");
                    writer.WriteLine("Old Order File Location = ");
                    writer.WriteLine("Rank Change Column = True");
                    writer.WriteLine("Verify = True");
                    writer.WriteLine("Reddit Verify = False");
                    writer.WriteLine("Regional Tables = False");
                    writer.WriteLine("Weighted = True");
                }
            }

            using (var reader = new StreamReader("Settings.txt")) {
                while (!reader.EndOfStream) {
                    var values = reader.ReadLine().Split('=');

                    for (int i = 0; i < values.Length; i++) {
                        values[i] = values[i].Trim();
                    }

                    switch (values[0]) {
                        case "Vendor File Location":
                            settings.VendorFileLocation = values[1];
                            break;
                        case "Vote File Location":
                            settings.VoteFileLocation = values[1];
                            break;
                        case "Old Order File Location":
                            settings.OldOrderFileLocation = values[1];
                            break;
                        case "Rank Change Column":
                            settings.RankChangeColumn = values[1].ToLower() == "true";
                            break;
                        case "Verify":
                            settings.Verify = values[1].ToLower() == "true";
                            break;
                        case "Reddit Verify":
                            settings.RedditVerify = values[1].ToLower() == "true";
                            break;
                        case "Regional Tables":
                            settings.RegionalTable = values[1].ToLower() == "true";
                            break;
                        case "Weighted":
                            settings.Weighted = values[1].ToLower() == "true";
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        static private void EditSettings(ref Settings settings, ref State progState) {
            string[] options = new string[] {"Vendor File Location",
                                            "Vote File Location",
                                            "Old Order File Location",
                                            "Rank Change Column",
                                            "Verify",
                                            "Reddit Verify",
                                            "Regional Tables",
                                            "Weighted",
                                            "EXIT TO MENU"};

            switch (options[UserInput.SelectionMenu(options)]) {
                case "Vendor File Location":
                    Console.Write("\n[Enter new Vendor File Location]: ");
                    settings.VendorFileLocation = UserInput.ValidString(1, 100);
                    break;
                case "Vote File Location":
                    Console.Write("\n[Enter new Vote File Location]: ");
                    settings.VoteFileLocation = UserInput.ValidString(1, 100);
                    break;
                case "Old Order File Location":
                    Console.Write("\n[Enter new Old Order File Location]: ");
                    settings.OldOrderFileLocation = UserInput.ValidString(1, 100);
                    break;
                case "Rank Change Column":
                    Console.WriteLine("\n[Set Rank Change Column value]");
                    settings.RankChangeColumn = UserInput.TrueFalse();
                    break;
                case "Verify":
                    Console.WriteLine("\n[Set Verify value]");
                    settings.Verify = UserInput.TrueFalse();
                    break;
                case "Reddit Verify":
                    Console.WriteLine("\n[Set Reddit Verify value]");
                    settings.RedditVerify = UserInput.TrueFalse();
                    break;
                case "Regional Tables":
                    Console.WriteLine("\n[Set Regional Tables value]");
                    settings.RegionalTable = UserInput.TrueFalse();
                    break;
                case "Weighted":
                    Console.WriteLine("\n[Set Weighted Tally value]");
                    settings.Weighted = UserInput.TrueFalse();
                    break;
                case "EXIT TO MENU":
                    progState = State.MAINMENU;
                    break;
            }

            SaveSettings(settings);
        }

        private static void SaveSettings(Settings settings) {
            using (var writer = new StreamWriter("Settings.txt")) {
                writer.WriteLine("Vendor File Location = " + settings.VendorFileLocation);
                writer.WriteLine("Vote File Location = " + settings.VoteFileLocation);
                writer.WriteLine("Old Order File Location = " + settings.OldOrderFileLocation);
                writer.WriteLine("Rank Change Column = " + settings.RankChangeColumn.ToString());
                writer.WriteLine("Verify = " + settings.Verify.ToString());
                writer.WriteLine("Reddit Verify = " + settings.RedditVerify.ToString());
                writer.WriteLine("Regional Tables = " + settings.RegionalTable.ToString());
                writer.WriteLine("Weighted = " + settings.Weighted.ToString());
            }
        }

        private static void RunProgram(Settings settings) {
            //verify files
            if (VerifySettings(settings)) {
                var options = new ProgressBarOptions {
                    ForegroundColor = ConsoleColor.Yellow,
                    ForegroundColorDone = ConsoleColor.DarkGreen,
                    BackgroundColor = ConsoleColor.DarkGray,
                    BackgroundCharacter = '\u2593'
                };

                //Load Vendors
                List<Vendor> allVendors = new List<Vendor>();
                Console.Write("[Loading vendors] ");
                ReadVendors(allVendors, settings.VendorFileLocation);
                Console.WriteLine("Done: (" + allVendors.Count + " Loaded)");

                //Verify votes
                List<string[]> votes = new List<string[]>();
                int ammountOfVotes = VoteCount(settings);

                if (settings.Verify) {
                    Console.WriteLine("Verifying " + ammountOfVotes + " Votes");

                    using (var pbar = new ProgressBar(ammountOfVotes, "Verifying Votes", options)) {
                        votes = VerifyVotes(settings.VoteFileLocation, settings, pbar);
                    }

                    Console.WriteLine("Done: (" + (ammountOfVotes - votes.Count) + " votes rejected)\n");
                }

                //Tally Votes
                Console.WriteLine("Counting " + votes.Count + " Votes");

                using (var pbar = new ProgressBar(votes.Count, "Counting Votes", options)) {
                    ReadPollingData(allVendors, votes, settings.VoteFileLocation, settings.Weighted, pbar);
                }

                //Sort the vendor lists first alphabetically, then by points so that drawn vendors are organised alphabetically
                allVendors = allVendors.OrderBy(Vendor => Vendor.Name).ToList();
                allVendors = allVendors.OrderByDescending(Vendor => Vendor.Points).ToList();

                Console.WriteLine("Done");

                if (settings.RankChangeColumn) {
                    List<string> oldOrder = new List<string>();
                    using(var reader = new StreamReader(settings.OldOrderFileLocation)) {
                        var Lines = reader.ReadToEnd().Split('\n');

                        foreach (var l in Lines) {
                            oldOrder.Add(l.Trim());
                        }
                    }

                    //compare the positions
                    for (int i = 0; i < allVendors.Count-1; i++) {
                        if(oldOrder.Contains(allVendors[i].Name)) {
                            allVendors[i].Change = oldOrder.IndexOf(allVendors[i].Name) - i;
                        } else {
                            allVendors[i].Change = 1000; //must be new
                        }
                    }
                }

                //Write tables
                Console.Write("Writing to disk: ");
                WriteTable(allVendors, settings);
                Console.WriteLine("Done");

                Console.WriteLine("\nPress any key to continue...");
            }
        }
        #endregion

        private static int VoteCount(Settings settings) {
            int output = -1;

            using(var reader = new StreamReader(settings.VoteFileLocation)) {
                var file = reader.ReadToEnd();
                var lines = file.Split('\n');

                output = lines.Length - 1;
            }

            return output;
        }

        private static bool VerifySettings(Settings settings) {
            bool settingsValid = true;
            if (!File.Exists(settings.VendorFileLocation)) {
                UserInput.ColourText("Error:");
                Console.WriteLine(" Vendor file doesn't exist");
                settingsValid = false;
            }

            if (!File.Exists(settings.VoteFileLocation)) {
                UserInput.ColourText("Error:");
                Console.WriteLine(" Vote file doesn't exist");
                settingsValid = false;
            }

            if (settings.RankChangeColumn && !File.Exists(settings.OldOrderFileLocation)) {
                UserInput.ColourText("Error:");
                Console.WriteLine(" Old Order file doesn't exist");
                settingsValid = false;
            }

            if (!settings.Verify && !File.Exists("output/ValidVotes")) {
                UserInput.ColourText("Error:");
                Console.WriteLine(" To run without validation run the program with validation on the votes at least once");
                settingsValid = false;
            }

            return settingsValid;
        }

        private static void ReadPollingData(List<Vendor> vendors,List<string[]> votes, string fileName, bool weighted, ProgressBar pBar) {
            // make a name searchable vendor list
            Dictionary<string, Vendor> VendorDict = new Dictionary<string, Vendor>();
            foreach (var v in vendors) {
                VendorDict.Add(v.Name, v);
            }

            using (var reader = new StreamReader(fileName)) {
                //Process votes 
                List<string> unmatched = new List<string>();
                //peform on each vote
                for (int i = 0; i < votes.Count; i++) {
                    pBar.Tick("Counting " + (i + 1) + "/" + votes.Count + " : " + votes[i][1]);
                    Thread.Sleep(5);
                    //add vendors that have been voted for to stop multiple votes per person
                    List<string> votedVendors = new List<string>();

                    //1st
                    if (VendorDict.ContainsKey(votes[i][2])) {
                        VendorDict[votes[i][2]].Points += weighted? 5:1;
                        votedVendors.Add(votes[i][2]);
                    } else {
                        unmatched.Add(votes[i][2]);
                    }
                    //2nd
                    if (VendorDict.ContainsKey(votes[i][3])) {
                        if (!votedVendors.Contains(votes[i][3])) {
                            VendorDict[votes[i][3]].Points += weighted ? 4 : 1;
                        }
                        votedVendors.Add(votes[i][3]);
                    } else {
                        unmatched.Add(votes[i][3]);
                    }
                    //3rd
                    if (VendorDict.ContainsKey(votes[i][4])) {
                        if (!votedVendors.Contains(votes[i][4])) {
                            VendorDict[votes[i][4]].Points += weighted ? 3 : 1;
                        }
                        votedVendors.Add(votes[i][4]);
                    } else {
                        unmatched.Add(votes[i][4]);
                    }
                    //run 1
                    if (VendorDict.ContainsKey(votes[i][5])) {
                        if (!votedVendors.Contains(votes[i][5])) {
                            VendorDict[votes[i][5]].Points += weighted ? 1 : 1;
                        }
                        votedVendors.Add(votes[i][5]);
                    } else {
                        unmatched.Add(votes[i][5]);
                    }
                    //run 2
                    if (VendorDict.ContainsKey(votes[i][6])) {
                        if (!votedVendors.Contains(votes[i][6])) {
                            VendorDict[votes[i][6]].Points += weighted ? 1 : 1;
                        }
                        votedVendors.Add(votes[i][6]);
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

        private static List<string[]> VerifyVotes(string FileLocation, Settings settings, ProgressBar pBar) {
            List<string[]> votes = new List<string[]>();
            List<string> usernames = new List<string>();
            List<string> bannedUsers = new List<string>();
            Dictionary<string, int> voteCheck = new Dictionary<string, int>();
            List<string> multipleBan = new List<string>(), notValidBan = new List<string>(), tooYoungBan = new List<string>(), fewKarmaBan = new List<string>();

            Users VerifyCache = new Users("Files/VerifyCache.csv");

            string[] file;

            using (var reader = new StreamReader(FileLocation)) {
                file = reader.ReadToEnd().Split('\n');
            }

            for (int i = 1; i < file.Length; i++) {
                //validate one vote per user
                var values = file[i].Trim().Split(',');

                pBar.Tick("Verifying " + (i) + "/" + (file.Length - 1) + " : " + values[1]);

                Thread.Sleep(5);

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

                //USERNAME CHECK - one vote per user, ban users having multiple votes
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
                if (settings.RedditVerify) {

                    string Username = values[1].Trim();
                    if (Username.ToLower().StartsWith("/u/") || Username.ToLower().StartsWith("\\u\\")) {
                        Username = Username.Remove(0, 3);
                    } else if (Username.ToLower().StartsWith("u/") || Username.ToLower().StartsWith("u\\")) {
                        Username = Username.Remove(0, 2);
                    }

                    try {
                        //Validate user
                        User user = VerifyCache.GetUser(Username);

                        if(user == null) {
                            var redditInterface = new Reddit();
                            var userRequest = redditInterface.GetUser(Username);

                            User temp = new User {
                                Username = Username,
                                Exists = true,
                                Creation = userRequest.Created,
                                Karma = userRequest.CommentKarma + userRequest.LinkKarma
                            };

                            VerifyCache.AddUser(temp);
                        } else {
                            if (!user.Exists) {
                                throw new Exception();
                            }
                        }

                        //if user doesn't have enough points or isn't old enough then don't add their results

                        if (user.Creation > new DateTimeOffset(2018,5,28,0,0,0, new TimeSpan(0)) || user.Karma < 0) {
                            if (!bannedUsers.Contains(values[1].ToLower())) {
                                bannedUsers.Add(values[1].ToLower());
                            }

                            if (user.Creation > new DateTimeOffset(2018, 5, 28, 0, 0, 0, new TimeSpan(0)) && !tooYoungBan.Contains(values[1])) {
                                tooYoungBan.Add(values[1]);
                            }

                            if (user.Karma < 0 && !fewKarmaBan.Contains(values[1])) {
                                fewKarmaBan.Add(values[1]);
                            }
                        }
                    } catch {//will catch when user doesn't exist (error 404)
                        if(VerifyCache.GetUser(Username) == null) {
                            User temp = new User {
                                Username = Username,
                                Exists = false
                            };

                            VerifyCache.AddUser(temp);
                        }

                        if (!bannedUsers.Contains(Username)) {
                            bannedUsers.Add(Username.ToLower());
                        }

                        if (!notValidBan.Contains(Username)) {
                            notValidBan.Add(Username.ToLower());
                        }
                    }
                }

                //add all votes at this point
                votes.Add(values);
            }

            //remove votes from banned users
            votes.RemoveAll(x => bannedUsers.Contains(x[1].ToLower()));

            //output vote report
            using (var writer = new StreamWriter("files/VoteReport.txt")) {
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
                sortableVoteCheck.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                foreach (var vote in sortableVoteCheck) {
                    if (vote.Value > 1) {
                        writer.WriteLine("Voted " + vote.Value + " times: " + vote.Key);
                    }
                }
            }

            return votes;
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

        private static void WriteTable(List<Vendor> vendors, Settings settings) {
            string votedTable;
            string unvotedTable = "Vendor/Website Link | Reddit User / Vendor Comments | Shipping Origin | Shipping Range\n---------|---------|---------|---------\n";
            //Add the reddit markup table headers
            if (settings.RankChangeColumn) {
                votedTable = "Change | Rank | Vendor/Website Link | Reddit User / Vendor Comments | Shipping Origin | Shipping Range\n---------|---------|---------|---------|---------|---------\n";
            } else {
                votedTable = "Rank | Vendor/Website Link | Reddit User / Vendor Comments | Shipping Origin | Shipping Range\n---------|---------|---------|---------|---------\n";   
            }

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
                    if (settings.RankChangeColumn) {
                        string change = "";

                        if (vendors[i].Change == 1000) {
                            change = "New";
                        } else if (rankPosition <= 1000) {
                            if (vendors[i].Change > 0) {
                                change = "⇑ " + vendors[i].Change;
                            } else if (vendors[i].Change < 0) {
                                change = "⇓ " + Math.Abs(vendors[i].Change);
                            } else if (vendors[i].Change == 0) {
                                change = "⇔";
                            }
                        }

                        votedTable += string.Format("{0} | {1} | {2} | {3} | {4} | {5}\n", change, draw ? "-" : rankPosition.ToString(), vendors[i].Links, vendors[i].Quote, vendors[i].Origin, vendors[i].Range);
                    } else {
                        votedTable += string.Format("{0} | {1} | {2} | {3} | {4}\n", draw ? "-" : rankPosition.ToString(), vendors[i].Links, vendors[i].Quote, vendors[i].Origin, vendors[i].Range);   
                    }
                } else {
                    unvotedTable += string.Format("{0} | {1} | {2} | {3}\n", vendors[i].Links, vendors[i].Quote, vendors[i].Origin, vendors[i].Range);
                }
            }

            //output to file with markup headers for each table section
            using (var writer = new StreamWriter("files/tables.txt")) {
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
        private int change;

        public int Points { get => points; set => points = value; }
        public string Name { get => name; set => name = value; }
        public string Links { get => links; set => links = value; }
        public string Quote { get => quote; set => quote = value; }
        public string Origin { get => origin; set => origin = value; }
        public string Range { get => range; set => range = value; }
        public int Change { get => change; set => change = value; }

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