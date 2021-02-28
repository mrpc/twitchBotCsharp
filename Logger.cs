using System;
using System.IO;

namespace twitchBot
{
    public class Logger
    {

        private StreamWriter logFile;
        private bool isOpen = false;
        private string path;

        public Logger(string LogFile = "")
        {
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;

            
            //once you have the path you get the directory with:
            string DirectoryPath = new Uri(System.IO.Path.GetDirectoryName(appPath)).LocalPath;

            if (LogFile == "")
            {
                path = DirectoryPath + "\\general.log";
            } else
            {
                path = DirectoryPath + "\\" + LogFile;
            }
            Console.WriteLine(path);
            if (!File.Exists(path))
            {
                logFile = File.CreateText(path);
                logFile.Close();
            }
        }

        public void Open()
        {
            if (isOpen)
            {
                return;
            }
            logFile = new StreamWriter(path, true);    
            isOpen = true;
        }

        public void Log(string log)
        {
            if (!isOpen)
            {
                Open();
            }
            logFile.WriteLine(DateTime.Now.ToString("MM/dd/yyyy H:mm:ss") + " - " + log);
            Close();
            Open();

        }

        public void Close()
        {
            logFile.Close();
            isOpen = false;
        }
    }
}