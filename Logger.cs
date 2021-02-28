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
            if (LogFile == "")
            {
                path = "d:\\Projects\\general.log";
            } else
            {
                path = "d:\\Projects\\" + LogFile;
            }
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