using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;

namespace S7Core
{
    // S7Core supervisor and side kick
    // Coding by kjurlina. Have a lot of fun
    internal class S7CoreSupervisor
    {
        // Global class variables
        private string ConfigFilePath;
        private string LogFilePath;
        private string DbFilePath;
        private string SeparationString;

        private string[]? ConfigFileContent;
        private string[]? ConfigLineContent;
        private int ConfigFileNumberOfLines;
        private bool Verbose = true;

        int i;

        // Make sure only one thread accesses file handling functions
        static readonly object FileAccessGuard = new object();

        public S7CoreSupervisor()
        {
            // Create configuration & log file names & paths
            if (Environment.OSVersion.ToString().Contains("Windows"))
            {
                ConfigFilePath = @".\config\S7CoreConfig.txt";
                LogFilePath = @".\logs\S7CoreLog.txt";
                DbFilePath = @".\db\S7CoreDB.sqlite";
                SeparationString = " :: ";
            }
            else if (Environment.OSVersion.Platform.ToString().Contains("Linux"))
            {
                ConfigFilePath = @".\config\S7CoreConfig.txt";
                LogFilePath = @".\logs\S7CoreLog.txt";
                DbFilePath = @".\db\S7CoreDB.sqlite";
                SeparationString = " :: ";
            }
            else
            {
                ConfigFilePath = "";
                LogFilePath = "";
                DbFilePath = "";
                SeparationString = "";
            }
        }

        // Log file functions
        public bool CheckLogFileExistence()
        {
            // Check if config file exists
            return File.Exists(LogFilePath);
        }
        public long ChecklLogFileSize()
        {
            // Check log file size
            long LogFileSize = new FileInfo(LogFilePath).Length;
            return LogFileSize;
        }
        public void CreateLogFile()
        {
            Directory.CreateDirectory("logs");
            var fileStream = File.Create(LogFilePath);
            fileStream.Close();
            ToLogFile("Application has created a new log file", false);
        }
        public void ArchiveLogFile()
        {
            // First log that existing log file will be archived
            ToLogFile("Application will archive this log file because it's too big", false);

            // Save file and extend name with current timestamp
            string ArchiveLogFilePath;
            string LogFileTS = DateTime.Now.Year.ToString() + "_" +
                               DateTime.Now.Month.ToString() + "_" +
                               DateTime.Now.Day.ToString() + "_" +
                               DateTime.Now.Hour.ToString() + "_" +
                               DateTime.Now.Minute.ToString() + "_" +
                               DateTime.Now.Second.ToString();

            ArchiveLogFilePath = @".\logs\S7CoreLog_" + LogFileTS + ".txt";

            // Create current log file archive copy
            File.Copy(LogFilePath, ArchiveLogFilePath);
            File.Delete(LogFilePath);
        }

        // Configuration file functions
        public bool CheckConfigFileExistence()
        {
            // Check if config file exists
            return File.Exists(ConfigFilePath);
        }
        public bool ReadConfigFile()
        {
            // This method to read configuration file content
            ConfigFileContent = File.ReadAllLines(ConfigFilePath);

            if (ConfigFileContent.Length > 0)
            {
                // Get number of configuration file lines
                ConfigFileNumberOfLines = File.ReadAllLines(ConfigFilePath).Length;
                // Read verbose mode
                Verbose = GetVerboseMode();
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool GetVerboseMode()
        {
            // This method to get verbose mode from config file content
            try
            {
                i = 0;
                while (ConfigFileContent != null && i <= ConfigFileNumberOfLines)
                {
                    if (i >= ConfigFileNumberOfLines)
                    {
                        ToLogFile("There is nothing about verbose mode in configuration file. We will assume it's off");
                        return false;
                    }

                    ConfigLineContent = ConfigFileContent[i].Split(SeparationString);
                    if (ConfigLineContent[0] == "Verbose")
                    {
                        ToLogFile("Verbose mode is on. All future messages will be mirrored to console");
                        return true;
                    }
                    i++;
                }

                return false;
            }
            catch (Exception ex)
            {
                ToLogFile("Something went wrong with determination of verbose mode. We will assume it's off");
                ToLogFile(ex.Message);
                return false;
            }
        }

        // SQLite functions
        public bool CheckDatabaseExits()
        {
            // Check if SQLite database file exists         
            return File.Exists(DbFilePath);
        }
        public void CreateDatabase()
        {
            // Create database in given path
            SQLiteConnection.CreateFile(DbFilePath);

            // Output message to log file
            ToLogFile("New database has been created");
        }
        public bool CheckDatabaseTableExists(string table)
        {
            // Check if database table exists
            List<String> Result = ExecuteSQLiteReader("SELECT name FROM sqlite_master WHERE type = 'table'");

            foreach (string SingleRow in Result)
            {
                if (SingleRow == table)
                {
                    return true;
                }
            }

            return false;
        }
        public void CreateDatabaseTable(string table, string variables = "")
        {
            // Create database table
            // Construct query string depending on case (Config, etc...)
            string SQLiteQueryString = "";
            switch (table)
            {
                case "S7CoreConfig":
                    SQLiteQueryString = "CREATE TABLE " + table + " (Parameter varchar(32), Value varchar(64))";
                    break;
                default:
                    SQLiteQueryString = "CREATE TABLE " + table + " " + variables;
                    break;
            }

            ExecuteSQLiteWriter(SQLiteQueryString);
            ToLogFile("Database table " + table + " has been created");
        }
        private List<string> ExecuteSQLiteReader(string query)
        {
            // Execute SQLite database reading command
            string SQLiteConnectionString = "Data Source = " + DbFilePath + "; Version = 3; ";
            string SQLiteQuery = query;
            string SingleRow = "";
            List<String> Result = new List<String>();

            using (SQLiteConnection SQLiteConnection = new SQLiteConnection(SQLiteConnectionString))
            {
                SQLiteConnection.Open();
                using (SQLiteCommand SQLiteCommand = new SQLiteCommand(SQLiteQuery, SQLiteConnection))
                {
                    using (SQLiteDataReader SQLiteDataReader = SQLiteCommand.ExecuteReader())
                    {
                        while (SQLiteDataReader.Read())
                        {
                            for (i = 0; i <= SQLiteDataReader.FieldCount - 1; i++)
                            {
                                SingleRow += SQLiteDataReader.GetValue(i).ToString();

                                // If this is not last column add data separator
                                if (i < SQLiteDataReader.FieldCount - 1)
                                {
                                    SingleRow += SeparationString;
                                }
                            }
                            Result.Add(SingleRow);
                            SingleRow = "";
                        }
                    }
                }
            }

            return Result;
        }
        private void ExecuteSQLiteWriter(string query)
        {
            // Execute SQLite database writing command
            string SQLiteConnectionString = "Data Source = " + DbFilePath + "; Version = 3; ";
            string SQLiteQuery = query;
            using (SQLiteConnection SQLiteConnection = new SQLiteConnection(SQLiteConnectionString))
            {
                SQLiteConnection.Open();
                using (SQLiteCommand SQLiteCommand = new SQLiteCommand(SQLiteQuery, SQLiteConnection))
                {
                    SQLiteCommand.ExecuteNonQuery();
                }
            }
        }

        // Logging functions
        public void ToConsole(string message)
        {
            // Output message to console
            string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
            Console.WriteLine(MessageTS + " :: " + message);
        }
        public void ToLogFile(string message)
        {
            // Output message to log file - with log file check
            // Prior to writing lock the file (multithreading)
            lock (FileAccessGuard)
            {
                // Prior to writing to log file, check it's existance
                // Also check if file size is exceeded - if yes, archive and create new one
                if (!CheckLogFileExistence())
                {
                    // Create log file
                    CreateLogFile();
                }
                else if (ChecklLogFileSize() > 1048576)
                {
                    // If log file is too big archive it and create new one
                    ArchiveLogFile();
                    CreateLogFile();
                }

                string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
                using (StreamWriter sw = File.AppendText(LogFilePath))
                {
                    sw.WriteLine(MessageTS + " :: " + message);
                    if (Verbose)
                    {
                        Console.WriteLine(MessageTS + " :: " + message);
                    }
                }
            }
        }
        public void ToLogFile(string message, bool check)
        {
            // Output message to log file - without file check
            // Prior to writing lock the file (multithreading)
            lock (FileAccessGuard)
            {
                string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
                using (StreamWriter sw = File.AppendText(LogFilePath))
                {
                    sw.WriteLine(MessageTS + " :: " + message);
                    if (Verbose)
                    {
                        Console.WriteLine(MessageTS + " :: " + message);
                    }
                }
            }
        }
    }
}
