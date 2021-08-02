using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FLPTime
{
    class MainProgram
    {
        static string VERSION_NUMBER = "1.0";

        static void Main(string[] args)
        {

            Console.WriteLine($"FLP-Time: version {VERSION_NUMBER}");
            Console.WriteLine("Scanning this directory...");

            int minutesTotal = 0;

            //Get FLP filepaths within exe directory.
            string path;
            string[] flpFiles = {""};
            if (args.Length == 0)
            {
                Console.WriteLine("Enter the directory where your project files are located (e.g. C:\\Image-Line\\Projects. Subdirectories will also be included.");
                path = Console.ReadLine();
            }
            else
            {
                path = args[0];
            }
            try
            {
                flpFiles = Directory.GetFiles(path, "*.flp", SearchOption.AllDirectories);
            }
            catch (IOException e)
            {
                Close($"Could not find the specified directory. {e.Message}");
            }
            
            if(flpFiles.Length == 0)
                Close("Didn't find any .flp files in the current or specified directory. Make sure the program is executed from the folder containing your projects or provide an argument with the directory you want to scan.");

            //Scan each FLP file for the 3-byte time segment and convert it to minutes using the forumla in MinutesFromBytes().
            foreach (string flpFile in flpFiles)
            {
                List<byte> timeBytes = new List<byte>();
                int minutes = 0;
                try
                {
                    timeBytes = GrabBytes(flpFile);
                }
                catch (ByteReadException e)
                {
                    Console.WriteLine($"Error reading data in {Path.GetFileName(flpFile)}. {e.Message}");
                }
                timeBytes.Reverse();
                try
                {
                    minutes = MinutesFromBytes(timeBytes);
                }
                catch (OverflowException e)
                {
                    Console.WriteLine($"Error converting bytes in {Path.GetFileName(flpFile)}. {e.Message}");
                }
                
                minutesTotal = minutesTotal + minutes;

                Console.WriteLine($"Logged {MinutesToString(minutes)} on project {Path.GetFileName(flpFile)}.");

            }

            Close($"{MinutesToString(minutesTotal)} spent on all projects.");
        }

        /// <summary>
        /// Scans the given FLP file for the byte segment representing the time spent on project statistic.
        /// </summary>
        /// <param name="flpFile"></param>
        /// <returns>A list of bytes.</returns>
        static List<byte> GrabBytes(string flpFile)
        {
            List<byte> timeBytes = new List<byte>();
            using (Stream source = File.OpenRead(flpFile))
            {
                byte[] buffer = new byte[2048];
                int bytesRead;
                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    List<byte> bytes = new List<byte>(buffer);
                    if (!bytes.Contains(237))
                        throw new ByteReadException("Failed to find time data entry point in first block.");
                    int startingIndex = bytes.IndexOf(237);
                    bool foundStart = false;
                    int currentIndex = 0;
                    foreach (byte checkByte in bytes)
                    {
                        if (checkByte == 237)
                        {
                            if (bytes[currentIndex + 1] == 16)
                            {
                                foundStart = true;
                                startingIndex = currentIndex;
                                break;
                            }
                        }
                        currentIndex++;
                    }
                    if (!foundStart)
                        throw new ByteReadException("Failed to find time data entry point.");
                    for (int i = 15; i < 18; i++) //reads in the last three bytes of the sequence
                    {
                        timeBytes.Add(bytes[startingIndex + i]);
                    }
                    break;
                }
            }
            return timeBytes;
        }

        /// <summary>
        /// Converts a value in minutes to a string representing hours and minutes.
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns>A string representing hours and minutes.</returns>
        static string MinutesToString(int minutes)
        {
            if (minutes < 60)
                return minutes.ToString() + "minutes";
            int hours = (int)Math.Floor((double)(minutes / 60));
            int leftoverMinutes = minutes - hours * 60;
            return $"{hours} hours and {leftoverMinutes} minutes";
        }

        /// <summary>
        /// Takes in the time byte array taken from an FLP file and converts it to minutes by the formula 0.698... * e^(0.00016... * decimalValue). 
        /// R2 value = 0.9992 for projects greater than 10 minutes.
        /// </summary>
        /// <param name="timeBytes"></param>
        /// <returns>An integer number representing minutes spent in the project file.</returns>
        static int MinutesFromBytes(List<byte> timeBytes)
        {
            StringBuilder hex = new StringBuilder(timeBytes.Count * 2);
            foreach (byte b in timeBytes)
                hex.AppendFormat("{0:x2}", b);
            int intVal = Int32.Parse(hex.ToString(), System.Globalization.NumberStyles.HexNumber) - 4145152;
            if (intVal <= 0)
                return 0;
            int minutesValue = (int)Math.Floor(0.698032696183935 * Math.Exp(0.00016979447312 * intVal));
            if (minutesValue < 0)
                throw new OverflowException($"Time byte value was too large. Value = {hex}");
            return minutesValue;

        }

        static void Close(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press any key to close.");
            Console.ReadKey();
            Environment.Exit(1);
        }

    }

    public class ByteReadException : Exception
    {
        public ByteReadException()
        {
        }

        public ByteReadException(string message)
            :base(message)
        {
        }
    }
}
