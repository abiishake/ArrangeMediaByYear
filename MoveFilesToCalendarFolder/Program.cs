using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Runtime.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Text;
using System.Runtime.CompilerServices;
using MetadataExtractor;
using Directory = System.IO.Directory;
using System.IO;

namespace MoveFilesToCalendarFolder
{
    internal class Program
    {
        private static Regex r = new Regex(":");
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile($"appsettings.json");
            var config = configuration.Build();
            var calendarMonths = config.GetSection("CalendarNames").Get<List<string>>();
            Console.WriteLine("Starting the program");
            Console.WriteLine($"Inside the path - {config.GetSection("BaseFolderPath").Value}");

            var baseFolderPath = config.GetSection("BaseFolderPath").Value;
            var directories = Directory.GetDirectories(baseFolderPath);
            var baselogFileName = $"LogMovingFiles_{DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")}.txt";
            var baselogFolder = baseFolderPath + "\\Logs";
            if (!Directory.Exists(baselogFolder))
            {
                Directory.CreateDirectory(baselogFolder);
            }
            StreamWriter baseFolderwriter = new StreamWriter(baselogFolder + "\\" + baselogFileName);
            foreach (var directory in directories)
            {
                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                    var folderName = Path.GetFileName(directory);
                    if (int.TryParse(folderName, out _))
                    {
                        int errorInFilesCount = 0;
                        baseFolderwriter.WriteLine($"Processing Year - {folderName}");
                        int totalFiles = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly).Count();
                        baseFolderwriter.WriteLine($"Total Files - {totalFiles}");
                        var logFileName = $"LogMovingFiles_{DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")}.txt";
                        var logFolder = directory + "\\Logs";
                        if (!Directory.Exists(logFolder))
                        {
                            Directory.CreateDirectory(logFolder);
                        }
                        StreamWriter writer = new StreamWriter(logFolder + "\\" + logFileName);
                        Console.WriteLine($"Processing Directory - {folderName}");
                        writer.WriteLine($"Processing Directory - {folderName}");
                        foreach (var month in calendarMonths)
                        {
                            var monthPath = directory + "\\" + month;
                            Console.WriteLine($"Processing Month - {month} || Path - {monthPath}");
                            writer.WriteLine($"Processing Month - {month} || Path - {monthPath}");
                            if (!Directory.Exists(monthPath))
                            {
                                Directory.CreateDirectory(monthPath);
                            }
                            Console.WriteLine("\tTotal Files - " + totalFiles);
                            writer.WriteLine($"\tProcessing Directory - {folderName}");
                            int counter = 0;
                            int filesMoved = 0;
                            foreach (var file in directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly).OrderBy(x => x.LastWriteTime))
                            {
                                try
                                {
                                    counter++;
                                    DateTime dateOfImage = GetDateTakenFromImage(file.FullName);
                                    if (dateOfImage == DateTime.MinValue)
                                    {
                                        Console.WriteLine($"Invalid Date for files - {file.FullName}");
                                        writer.WriteLine($"Invalid Date for files - {file.FullName}");
                                    }
                                    else if (month.Contains(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateOfImage.Month)))
                                    {
                                        if (!File.Exists(monthPath + "\\" + file.Name))
                                        {
                                            Console.WriteLine($"{counter}/{totalFiles} Moving File - {file.Name}");
                                            writer.WriteLine($"{counter}/{totalFiles} Moving File - {file.Name}");
                                            File.Move(file.FullName, monthPath + "\\" + file.Name, true);
                                            filesMoved++;
                                            Console.WriteLine($"\t Success");
                                        }   
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errorInFilesCount++;
                                    Console.WriteLine($"Error Processing file - {file.FullName} || Error - {ex.Message}");
                                    writer.WriteLine($"Error Processing file - {file.FullName} || Error - {ex.Message}");
                                    continue;
                                }
                            }
                            Console.WriteLine($"Files Moved to {folderName} - {month} = {filesMoved}/{totalFiles}");
                            writer.WriteLine($"Files Moved to {folderName} - {month} = {filesMoved}/{totalFiles}");
                        }
                        writer.Close();
                        baseFolderwriter.WriteLine($"Total Errors = {errorInFilesCount}/{totalFiles}");
                        baseFolderwriter.WriteLine($"------------------------------------------------------------------------------------------------------------------------------");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Processing directory - {directory} || Error - {ex.Message}");
                    continue;
                }
            }
            baseFolderwriter.Close();
        }

        static DateTime GetDateTakenFromImage(string imagePath)
        {

            var directories = ImageMetadataReader.ReadMetadata(imagePath);

            foreach (var directory in directories)
            {
                var tags = directory.Tags;
                var dateTakenTag = tags.FirstOrDefault(tag => tag.Name == "Date/Time Original" || tag.Name == "File Modified Date" || tag.Name == "Created");

                if (dateTakenTag != null)
                {
                    if (dateTakenTag.DirectoryName == "QuickTime Movie Header")
                    {
                        if (dateTakenTag.Name == "Created")
                        {
                            string format = "ddd MMM dd HH:mm:ss yyyy";
                            if (dateTakenTag != null)
                            {
                                return DateTime.ParseExact(dateTakenTag.Description, format, CultureInfo.InvariantCulture);
                            }
                        }
                    }
                    if (dateTakenTag.Name == "File Modified Date")
                    {
                        string format = "ddd MMM dd HH:mm:ss zzz yyyy";
                        if (dateTakenTag != null)
                        {
                            return DateTime.ParseExact(dateTakenTag.Description, format, CultureInfo.InvariantCulture);
                        }
                    }
                    else if (dateTakenTag.Name == "Date/Time Original")
                    {
                        if (dateTakenTag != null && DateTime.TryParse(dateTakenTag.Description, out DateTime dateTaken))
                        {
                            return dateTaken;
                        }
                        else if (dateTakenTag.DirectoryName == "Exif SubIFD")
                        {
                            string format = "yyyy:MM:dd HH:mm:ss";
                            return DateTime.ParseExact(dateTakenTag.Description, format, CultureInfo.InvariantCulture);
                        }
                    }
                }
            }

            // Return DateTime.MinValue if the date taken information is not found
            return DateTime.MinValue;
        }
    }
}