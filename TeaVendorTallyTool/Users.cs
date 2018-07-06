using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeaVendorTallyTool {
    class Users {
        private Dictionary<string, User> AllUsers = new Dictionary<string, User>();
        private string FileName = string.Empty;

        public Users(string FileLocation) {
            FileName = FileLocation;
            if (File.Exists(FileLocation)) {
                using (var reader = new StreamReader(FileLocation)) {
                    //file format = username, exists, karma, creationdate
                    var file = reader.ReadToEnd();
                    var lines = file.Split('\n');

                    foreach (var line in lines) {
                        if(line == "") { continue; }
                        User temp = new User();
                        var values = line.Split(',');

                        temp.Username = values[0].Trim();

                        if (values[1].Trim() == "True") {
                            temp.Exists = true;

                            int.TryParse(values[2].Trim(), out int Karma);
                            temp.Karma = Karma;

                            DateTimeOffset.TryParse(values[3].Trim(), out DateTimeOffset CreationDate);
                            temp.Creation = CreationDate;
                        } else {
                            temp.Exists = false;
                        }

                        AllUsers.Add(temp.Username, temp);
                    }
                }
            }
        }

        public User GetUser(string username) {
            if (AllUsers.ContainsKey(username)) {
                return AllUsers[username];
            } else {
                return null;
            }
        }

        public void AddUser(User UserToAdd) {
            AllUsers.Add(UserToAdd.Username, UserToAdd);

            if (!File.Exists(FileName)) {
                File.Create(FileName);
            }

            using (var Writer = new StreamWriter(FileName, true)) {
                //file format = username, exists, karma, creationdate
                Writer.Write(UserToAdd.Username + ',');
                Writer.Write(UserToAdd.Exists.ToString() + ',');
                Writer.Write(UserToAdd.Karma.ToString() + ',');
                Writer.Write(UserToAdd.Creation.ToString() + '\n');
            }
        }

    }

    public class User {
        string username;
        int karma;
        bool exists;
        DateTimeOffset creation;

        public int Karma { get => karma; set => karma = value; }
        public DateTimeOffset Creation { get => creation; set => creation = value; }
        public string Username { get => username; set => username = value; }
        public bool Exists { get => exists; set => exists = value; }
    }
}
