# TeaVendorTally
A program for tallying points from a google forms vote, specificallly designed for the /r/tea subreddit.

## Setup (now outdated)
### Google Form Settings
1. Take the existing vendor list and add the vendor names as options in a multiple choice grid. The choices available should be "1st", "2nd", "3rd", "Runner-up 1" and "Runner-up 2".
2. Check shuffle row order, allow only one answer per person and check limit to one option per column. 
3. After the poll closes simply go to the response spreadsheet and download it as a .csv file.

### Processing results with Tea Vendor Tally tool
1. Compile the .exe and move it into a folder you want to use for the tea tally tool.
2. Place the Google Form result csv file in the same folder as the executable or copy the file location.
3. Take the same vendor list used to create the vendor form and copy the table (with markup) into a file named "quotes.csv". Save this with the tally tool executable or copy the file location.
4. Launch a command prompt window with the dirctory in the file location of the executable. if all files are in the same place simply run the program by typing the executable name followed by the result file name. If the files are not all in the same folder simply execute the program with the following flags: -r followed by the result files file location and -q followed by the quote files file location.
5. If the program prints out any mapping errors fix the names in the quote file or manually fix the row in the outputted table.txt file.
6. Copy and paste all the content within the table.txt into the reddit wiki page
