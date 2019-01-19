using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageParser
{
    class Program
    {
        static string _directory = "";
        static string _newDirectory = "";

        static void Main(string[] args)
        {
            Console.WriteLine(@"Please enter a directory to parse. Example: C:\User\Photos");

            _directory = Console.ReadLine();

            if (!Directory.Exists(_directory)) {
                Console.WriteLine("Directory does not exist!");
                return;
            }

            _newDirectory = Path.Join(_directory, "Processed");

            if(!Directory.Exists(_newDirectory))
                Directory.CreateDirectory(_newDirectory);

            Console.WriteLine($"Processing file into directory: {_newDirectory}");

            string[] dirs = Directory.GetDirectories(_directory);

            foreach(var dir in dirs) {
                ParseDirectory(dir);
            }

            Console.WriteLine("Processing Complete! :)");
            Console.WriteLine($"Processed photos are located here: {_newDirectory}");

            Console.Read();
        }

        private static void ParseDirectory(string folder)
        {
            Console.WriteLine($"Parsing Folder: {folder}");
            string[] dirs = Directory.GetDirectories(folder);
            foreach(var dir in dirs){
                ParseDirectory(dir);
            }
            string[] files = Directory.GetFiles(folder);
            Console.WriteLine($"Parsing {files.Count()} Files.");
            foreach(var file in files){
                ParseFile(file);
            }
        }

        private static void ParseFile(string file)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var fileName = Path.GetFileName(file);
            var validExtensions = new List<string>(){
                ".jpg",
                ".png"
            };
            if (!validExtensions.Contains(ext))
                return;

            Console.WriteLine($"Processing Image: {Path.GetFileName(file)}");

            try 
            {       
                DateTime dateTaken;     
                using(System.Drawing.Image image = System.Drawing.Image.FromFile(file))
                {
                    PropertyItem propertyItem = image.GetPropertyItem(0x9003); // DateTime taken
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    string dateString = encoding.GetString(propertyItem.Value, 0, propertyItem.Len - 1);
                    
                    DateTime.TryParseExact(dateString, "yyyy:MM:dd HH:mm:ss", CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTaken);
                    Console.WriteLine($"Date: {dateTaken}");
                    image.Dispose();
                }
                         
                if (dateTaken == DateTime.MinValue) {
                    string path = Path.Join(_newDirectory, "No_Date");
                    if (!Directory.Exists(path)){
                        Directory.CreateDirectory(path);
                    }
                    if (!File.Exists(Path.Join(path, fileName)))
                        File.Move(file, Path.Join(path, fileName));
                    else
                        ProcessDuplicate(file);
                } else {
                    string path2 = Path.Join(_newDirectory, dateTaken.Year.ToString(), dateTaken.Month.ToString());
                    if (!Directory.Exists(path2)){
                        Directory.CreateDirectory(path2);
                    }
                    if (!File.Exists(Path.Join(path2, fileName)))
                        File.Move(file, Path.Join(path2, fileName));
                    else
                        ProcessDuplicate(file);
                }
            } 
            catch(Exception ex) {
                Console.WriteLine("Error Processing Image", ex);
                string path = Path.Join(_newDirectory, "Errors");
                if (!Directory.Exists(path)){
                    Directory.CreateDirectory(path);
                }

                string newPath = Path.Join(path, fileName);
                if (!File.Exists(newPath))
                    File.Move(file, newPath);
                else
                    ProcessDuplicate(file);
            }
        }

        private static void ProcessDuplicate(string filePath) {
            var dupPath = Path.Join(_newDirectory, "Possible_Duplicates");
            if(!Directory.Exists(dupPath))
                Directory.CreateDirectory(dupPath);

            var fileInfo = new FileInfo(filePath);
            var newFilePath = Path.Join(dupPath, fileInfo.Name);
            if (!File.Exists(newFilePath)) {
                fileInfo.MoveTo(newFilePath);
            } else {
                // We need to copy with an extented name.
                var justFileName = fileInfo.Name.Replace(fileInfo.Extension, "");
                var newFileName = justFileName + Guid.NewGuid().ToString().Substring(0, 8) + fileInfo.Extension;
                fileInfo.MoveTo(Path.Join(dupPath, newFileName));
            }
        }
    }
}
