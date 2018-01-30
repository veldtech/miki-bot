using System;
using System.IO;

namespace IA.FileHandling
{
    public class FileReader : IDisposable
    {
        private StreamReader file;

        private string filePath;

        public FileReader(string fileName)
        {
            if (!fileName.Contains("."))
            {
                filePath = Directory.GetCurrentDirectory() + "/" + fileName + ".config";
                file = new StreamReader(new FileStream(filePath + ".config", FileMode.Create));
            }
            else
            {
                filePath = Directory.GetCurrentDirectory() + "/" + fileName;
                file = new StreamReader(new FileStream(filePath, FileMode.Create));
            }
        }

        public FileReader(string fileName, string relativePath)
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + relativePath);
            if (!fileName.Contains("."))
            {
                filePath = Directory.GetCurrentDirectory() + "/" + relativePath + "/" + fileName;
                file = new StreamReader(new FileStream(filePath + ".config", FileMode.Open));
            }
            else
            {
                filePath = Directory.GetCurrentDirectory() + "/" + relativePath + "/" + fileName;
                file = new StreamReader(new FileStream(filePath, FileMode.Open));
            }
        }

        public void Dispose()
        {
            file.Dispose();
        }

        public static bool FileExist(string fileName)
        {
            return File.Exists(Directory.GetCurrentDirectory() + "/" + fileName);
        }

        public static bool FileExist(string fileName, string relativePath)
        {
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/" + relativePath);
            if (!fileName.Contains("."))
            {
                return File.Exists(Directory.GetCurrentDirectory() + "/" + relativePath + "/" + fileName + ".config");
            }
            else
            {
                return File.Exists(Directory.GetCurrentDirectory() + "/" + relativePath + "/" + fileName);
            }
        }

		public string ReadAll()
		{
			string o = "";
			string temp = "";

			while (temp != null)
			{
				temp = file.ReadLine();
				if (temp == null)
				{
					break;
				}
				else
				{
					if (!temp.StartsWith("#"))
					{
						o += temp;
					}
				}
			}
			return o;
		}
        public string ReadLine()
        {
            while (true)
            {
                string currentLine = file.ReadLine();
                if (currentLine == null)
                {
                    Log.WarningAt("filereader", "no data found.");
                    break;
                }

                if (!currentLine.StartsWith("#")) return currentLine;
            }
            return "";
        }

        public void Finish()
        {
            Dispose();
        }
    }
}