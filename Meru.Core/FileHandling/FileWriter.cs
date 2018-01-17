using System;
using System.IO;

namespace IA.FileHandling
{
    public class FileWriter : IDisposable
    {
        private string filePath;

        private StreamWriter file;

        public FileWriter(string fileName)
        {
            if (fileName.Split('.').Length == 1)
            {
                filePath = Directory.GetCurrentDirectory() + "\\" + fileName;
                file = new StreamWriter(new FileStream(filePath + ".config", FileMode.Create));
                file.WriteLine($"# {fileName} created with {Bot.VersionText}");
            }
            else
            {
                filePath = Directory.GetCurrentDirectory() + "\\" + fileName;
                file = new StreamWriter(new FileStream(filePath, FileMode.Create));
                file.WriteLine($"# {fileName} created with {Bot.VersionText}");
            }
        }

        public FileWriter(string fileName, string relativePath)
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + relativePath);
            if (fileName.Split('.').Length == 1)
            {
                filePath = Directory.GetCurrentDirectory() + "\\" + relativePath + "\\" + fileName;
                file = new StreamWriter(new FileStream(filePath + ".config", FileMode.Create));
                file.WriteLine($"# {fileName} created with {Bot.VersionText}");
            }
            else
            {
                filePath = Directory.GetCurrentDirectory() + "\\" + relativePath + "\\" + fileName;
                file = new StreamWriter(new FileStream(filePath, FileMode.Create));
                file.WriteLine($"# {fileName} created with {Bot.VersionText}");
            }
        }

        public static explicit operator bool(FileWriter x)
        {
            return x != null;
        }

        public void Dispose()
        {
            file.Dispose();
        }

        public void Write(string variable)
        {
            file.WriteLine(variable);
            file.Flush();
        }

        public void Write(string variable, string comment)
        {
            file.WriteLine($"# {comment}");
            file.WriteLine(variable);
            file.Flush();
        }

        public void WriteComment(string comment)
        {
            file.WriteLine($"# {comment}");
            file.Flush();
        }

        public void Finish()
        {
            file.Flush();
            Dispose();
        }
    }
}