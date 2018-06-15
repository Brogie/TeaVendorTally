//Author           - Jack Brogan
//Last Edit Date   - 25/Nov/2014
//Version          - 1.6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TeaVendorTallyTool {

    /// <summary>
    /// This is the menu system that I have been developing during the fenner
    /// labs to help me with my cinema entry program, I have tried my best to
    /// make this so that I can include it with any console application I want
    /// without having to alter the code.
    /// 
    /// This class mostly deals with validation of input and making user input
    /// easier through selection menus.
    /// </summary>
    internal static class UserInput {

        /// <summary>
        /// Takes a string that the user wrote on the screen and clears it,
        /// </summary>
        /// <param name="inStartOffsetL">
        /// Where the cursor started before the user typed anything (from the
        /// left of the console)
        /// </param>
        /// <param name="inStartOffsetT">
        /// Where the cursor started before the user typed anything (from the
        /// top of the console)
        /// </param>
        /// <param name="inString">The string to be cleared from the screen</param>
        public static void ClearInputString(int inStartOffsetL, int inStartOffsetT, string inString) {
            int whitespace = 0;

            //move the cursor to the start of where the user began inputting
            Console.SetCursorPosition(inStartOffsetL, inStartOffsetT);

            //Now we clear what the user wrote by printing spaces over it

            //needs to loop once per character in the string (subtract tab characters as we handle tabs later)
            for (int i = 0; i < (inString.Length - inString.Count(tabs => tabs == '\t')); i++) {
                whitespace += 1;
            }

            //this handles and tab characters that have been inputted
            for (int i = 0; i < inString.Count(tabs => tabs == '\t'); i++) {
                whitespace += 8;
            }

            //generate a string that is all spaces and is the length of the whitespace needed.
            StringBuilder ClearingString = new StringBuilder(string.Empty, whitespace);

            for (int i = 0; i < whitespace; i++) {
                ClearingString.Insert(i, " ");
            }

            //print the clearing string to clear the text
            Console.Write(ClearingString);

            //finally reset the curser to allow the user to reinput the number
            Console.SetCursorPosition(inStartOffsetL, inStartOffsetT);
        }

        /// <summary>
        /// Draws a header at the top of the screen and returns the cursor back
        /// to the start location.
        /// </summary>
        /// <param name="inHeader">Title of the header to be passed in</param>
        /// <param name="colour">Colour of the header (default is red)</param>
        public static void Header(string inHeader, int colour = 12) {
            //Declare Variables
            int whitespace = 0;
            StringBuilder outHeader = new StringBuilder(string.Empty);
            int startTOffset, startLOffset;

            //Grab the curser so we can move back to its location after we finish
            startTOffset = Console.CursorTop;
            startLOffset = Console.CursorLeft;

            //if the start cursor is inside the header (>3) then move it down to the bottom
            if (startTOffset < 3)
                startTOffset = 3;

            //Move the cursor to the top so it can draw the header
            Console.SetCursorPosition(0, 0);

            //Throw an exception if the header will not fit on one line
            if (inHeader.Length > (Console.WindowWidth - 2))
                inHeader = "Error: Header too long";

            //draw the top of the box
            outHeader.Append("╔");

            for (int i = 0; i < (Console.WindowWidth - 2); i++)
                outHeader.Append("═");

            outHeader.Append("╗");

            //calculate the whitespace required for each side of the header
            whitespace = (((Console.WindowWidth - inHeader.Length) / 2) - 1);

            //Construct middle part of box with the header in the middle
            outHeader.Append("║");

            for (int i = 0; i < whitespace; i++)
                outHeader.Append(" ");

            outHeader.Append(inHeader);

            for (int i = 0; i < whitespace; i++)
                outHeader.Append(" ");

            //Add an extra = in the whitespace if the header has an odd amount
            //of characters
            if (inHeader.Length % 2 != 0)
                outHeader.Append(" ");

            outHeader.Append("║");

            //draw bottom of the box
            outHeader.Append("╚");

            for (int i = 0; i < (Console.WindowWidth - 2); i++)
                outHeader.Append("═");

            outHeader.Append("╝");

            //Draw the header
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = (ConsoleColor)colour;
            Console.Write(outHeader);
            Console.ResetColor();

            //finally reset the curser to allow program to carry on from where it left off
            Console.SetCursorPosition(startLOffset, startTOffset);
        }

        /// <summary>
        /// Reads in from a text file and adds each line to a list of strings
        /// (you will need to pass the list to this method)
        /// </summary>
        /// <param name="stringList">List to add the films to</param>
        /// <param name="fileName">File name to read from (include .txt)</param>
        public static void ReadFile(string fileName, List<string> stringList) {
            StreamReader reader = new StreamReader(fileName);
            string line;

            while (reader.EndOfStream == false) {
                line = reader.ReadLine();
                stringList.Add(line);
            }
            reader.Close();
        }

        /// <summary>
        /// This method will get the user to write a string and will force the
        /// user to write something in (so you dont get any blank strings)
        /// </summary>
        /// <returns>The string the user entered</returns>
        public static string ValidString(int minLength, int maxLength) {
            if (minLength >= maxLength) {
                throw new Exception("Min should not be bigger than max");
            }
            int startOffsetT, startOffsetL;
            bool stringValid = false;
            string outputString = "";

            //Grab the curser before any text input (incase we need to handle an exeption)
            startOffsetT = Console.CursorTop;
            startOffsetL = Console.CursorLeft;

            while (stringValid == false) {
                outputString = Console.ReadLine();

                if (outputString.Length < minLength || outputString.Length > maxLength) {
                    ClearInputString(inStartOffsetL: startOffsetL, inStartOffsetT: startOffsetT, inString: outputString);
                } else {
                    stringValid = true;
                }
            }

            return outputString;
        }

        /// <summary>
        /// Writes a menu to the screen that gives the user the ablility to
        /// select either yes or no.
        /// </summary>
        /// <returns>Boolen yes=true no=false</returns>
        public static bool YesNo() {
            // Make a selection menu with the option of yes or no, then return a
            // bool result depending on what the user selected
            if (UserInput.SelectionMenu(new string[2] { "Yes", "No" }) == 0) {
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Writes a menu to the screen that gives the user the ablility to
        /// select either true or false.
        /// </summary>
        /// <returns>Boolen yes=true no=false</returns>
        public static bool TrueFalse() {
            // Make a selection menu with the option of yes or no, then return a
            // bool result depending on what the user selected
            if (UserInput.SelectionMenu(new string[2] { "True", "False" }) == 0) {
                return true;
            } else {
                return false;
            }
        }

        #region Integer Input / Validation

        /// <summary>
        /// A method that takes a minimum number and a maximum number and allows
        /// the user to input a number within those bounds. The method will not
        /// allow the user to crash the program by entering in a string variable
        /// / integer too large or small, and it will output a relevent error message.
        /// </summary>
        /// <param name="min">The minimum value for the number</param>
        /// <param name="max">The maximum value for the number</param>
        /// <returns>the integer that the user inputted</returns>
        public static int ReadRange(int min, int max) {
            int output;
            //Throw an exception if the programmer puts a bigger minimum than max (to stop infinate loop)
            if (min >= max) {
                throw new Exception("Min should not be bigger than max");
            }
            while (true) {
                int startOffsetT, startOffsetL;
                //get the start point for the cursor
                startOffsetT = Console.CursorTop;
                startOffsetL = Console.CursorLeft;
                //get user input
                output = ValidInteger();

                if (output >= min && output <= max) {
                    break;
                } else {
                    //if number is out of range then restart the input
                    Console.SetCursorPosition(startOffsetL, startOffsetT);
                    for (int i = 0; i < output.ToString().Length; i++)
                        Console.Write(" ");
                    Console.SetCursorPosition(startOffsetL, startOffsetT);
                }
            }
            return output;
        }

        /// <summary>
        /// Gets user input for an integer, doesn't print any errors, insted
        /// simply deletes the invalid number. If this breaks keep in mind i did
        /// this at 1am
        /// </summary>
        /// <returns>the integer the user inputs</returns>
        public static int ValidInteger() {
            string outputString;
            bool validOutput = false;
            int output = 0;
            int startOffsetT, startOffsetL;

            //Grab the curser before any text input (incase we need to handle an exeption)
            startOffsetT = Console.CursorTop;
            startOffsetL = Console.CursorLeft;

            while (!validOutput) {
                //Get user input and place it in a string
                outputString = Console.ReadLine();
                try {
                    //-0 isn't handled well when converting to an integer so we need to throw an exception
                    if (outputString == "-0") {
                        throw new Exception("-0 wont convert to integer correctly");
                    }
                    //This will parse the string into the output int, if it works then the output will be valid
                    output = int.Parse(outputString);
                    //this set will only be reached if int.parse dosn't throw an exception
                    validOutput = true;
                } catch {
                }

                //Clear what the user wrote
                ClearInputString(inStartOffsetL: startOffsetL, inStartOffsetT: startOffsetT, inString: outputString);
            }

            //write the actual value of what you are returning (so that if the user wrote 005 it will reformat it to 5)
            Console.Write(output);

            return output;
        }

        #endregion Integer Input / Validation

        #region Selection Menu Overloads

        /// <summary>
        /// This method creates a user friendly menu that allows users to select
        /// items from an array of strings.
        /// Also has a optional integer parameter which allows you to set the
        /// padding around the menu.
        /// Will Throw exception if there are too many items + padding to display
        /// on the screen.
        /// </summary>
        /// <param name="selectionArray">String Array containing each line of the
        /// menu</param>
        ///<param name="padding">How much space to pad arounf the menu</param>
        /// <returns>integer that is the index of the item the user selected.
        /// </returns>
        public static int SelectionMenu(string[] selectionArray, int padding = 0) {
            bool loopComplete = false;
            int topOffset = Console.CursorTop;
            int bottomOffset = 0;
            int selectedItem = 0;
            ConsoleKeyInfo kb;

            Console.CursorVisible = false;

            //this will resise the console if the amount of elements in the list are too big
            if ((selectionArray.Length + padding) > Console.WindowHeight) {
                try {
                    Console.SetWindowSize(80, (selectionArray.Length + padding));
                } catch {
                    throw new Exception("Too many items in the array to display");
                }
            }

            while (!loopComplete) {
                //This for loop prints the array out
                for (int i = 0; i < selectionArray.Length; i++) {
                    if (i == selectedItem) {
                        //This section is what highlights the selected item
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine("-" + selectionArray[i]);
                        Console.ResetColor();
                    } else {
                        //this section is what prints unselected items
                        Console.WriteLine("-" + selectionArray[i]);
                    }
                }

                bottomOffset = Console.CursorTop;

                //This reads the input of the user and puts it into the variable KB
                kb = Console.ReadKey(true);
                //This switch statement parses the user input and acts appropriatly
                //to alter what film is selected / what film will be outputed
                switch (kb.Key) {
                    case ConsoleKey.UpArrow:
                        if (selectedItem > 0) {
                            selectedItem--;
                        } else {
                            selectedItem = (selectionArray.Length - 1);
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (selectedItem < (selectionArray.Length - 1)) {
                            selectedItem++;
                        } else {
                            selectedItem = 0;
                        }
                        break;

                    case ConsoleKey.Enter:
                        loopComplete = true;
                        break;
                }
                //this resets the cursor back to the top of the page so that the menu
                //can be wrote over with an updated selection.
                Console.SetCursorPosition(0, topOffset);
            }
            //this sets the cursor just after the menu so that the program can continue
            Console.SetCursorPosition(0, bottomOffset);

            Console.CursorVisible = true;
            return selectedItem;
        }

        /// <summary>
        /// This method creates a user friendly menu that allows users to select
        /// items from an array of strings.
        /// Also has a optional integer parameter which allows you to set the
        /// padding around the menu.
        /// Will Throw exception if there are too many items + padding to display
        /// on the screen.
        /// </summary>
        /// <param name="selectionArray">String Array containing each line of the
        /// menu</param>
        ///<param name="padding">How much space to pad arounf the menu</param>
        /// <returns>integer that is the index of the item the user selected.
        /// </returns>
        public static int SelectionMenu(char[] selectionArray, int padding = 0) {
            string[] convertedArray = new string[selectionArray.Length];

            for (int i = 0; i < selectionArray.Length; i++) {
                convertedArray[i] = selectionArray[i].ToString();
            }

            return SelectionMenu(selectionArray: convertedArray, padding: padding);

        }

        /// <summary>
        /// This method creates a user friendly menu that allows users to select
        /// items from an array of strings. Also has a optional integer
        /// parameter which allows you to set the padding around the menu. Will
        /// Throw exception if there are too many items + padding to display on
        /// the screen.
        /// </summary>
        /// <param name="selectionList">
        /// String Array containing each line of the menu
        /// </param>
        /// <param name="padding">How much space to pad arounf the menu</param>
        /// <returns>integer that is the index of the item the user selected.</returns>
        public static int SelectionMenu(List<string> selectionList, int padding = 0) {
            string[] convertedArray = new string[selectionList.Count];

            for (int i = 0; i < selectionList.Count; i++) {
                convertedArray[i] = selectionList[i];
            }

            return SelectionMenu(selectionArray: convertedArray, padding: padding);
        }

        #endregion Selection Menu Overloads
    }
}