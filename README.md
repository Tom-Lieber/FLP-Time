# FLP-Time
This .NET console application extracts the "time spent on project" statistic from FL Studio project files in batch.
It relies on an exponential function to approximate how these values are saved in the .flp filetype.
The output of this application can only be considered an estimate. Reported values are on average 2 percent lower than actual values, although this varies from -4 to 2 percent. An adjustment is made when reporting the total to help account for this error.

# Usage
The program is operated via the command line. You can either enter the directory to scan at runtime or provide it as an argument. Subdirectories are also scanned. All .flp files found in the given directory will be analyzed, including those that are multiple versions of the same file. If you have multiple of the same project, the time will be counted multiple times. It is reccomended to create a directory that only contains the most recent version of each project or your reported values will be far higher than reality.
