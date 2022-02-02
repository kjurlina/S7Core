using System;
using System.Threading;
using System.Threading.Tasks;

namespace S7Core
{
    // S7 Core Framework
    // Working with configuration, logs and SQLite database
    // Coding by kjurlina
    // Have a lot of fun

    class S7Core
    {
        // Main routime
        static void Main(string[] args)
        {
            // Supervisor - global variable (? for nullable types...)
            S7CoreSupervisor? Supervisor = null;

            // Application threads
            S7CoreThread1 Thread1 = new S7CoreThread1();
            S7CoreThread2 Thread2 = new S7CoreThread2();

            // Create local supervisor instance and event handler for application closing event
            try
            {
                Supervisor = new S7CoreSupervisor();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler((sender, e) => CurrentDomain_ProcessExit(sender, e, Supervisor));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            // Handle log file
            try
            {
                if (!Supervisor.CheckLogFileExistence())
                {
                    // Create log file
                    Supervisor.CreateLogFile();
                    Supervisor.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                    Supervisor.ToLogFile("Application started");
                }
                else if (Supervisor.ChecklLogFileSize() > 1048576)
                {
                    // If log file is too big archive it and create new one
                    Supervisor.ArchiveLogFile();
                    Supervisor.CreateLogFile();
                    Supervisor.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                    Supervisor.ToLogFile("Application started");
                }
                else
                {
                    Supervisor.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                    Supervisor.ToLogFile("Application started");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Something went wrong with log file. Exiting application");
                return;
            }

            // Handle config file
            try
            {
                if (!Supervisor.CheckConfigFileExistence())
                {
                    Supervisor.ToLogFile("Configuration file does not exist. Exiting application");
                    return;
                }
                else
                {
                    Supervisor.ReadConfigFile();
                    Supervisor.ToLogFile("Configuration file loaded successfully");
                }
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Something went wrong with configuration file. Exiting application");
                return;
            }

            // Handle database file
            try
            {
                if (!Supervisor.CheckDatabaseExits())
                {
                    Supervisor.CreateDatabase();
                }
                else
                {
                    Supervisor.ToLogFile("Database checked and OK");
                }

            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Something went wrong with database file. Exiting application");
                return;
            }

            // handle database tables
            try
            {
                // Defaultna tablica
                if (!Supervisor.CheckDatabaseTableExists("S7CoreConfig"))
                {
                    Supervisor.CreateDatabaseTable("S7CoreConfig");
                }
                // Primjer proziva za kreiranje tablice po volji
                // if (!Supervisor.CheckDatabaseTableExists("S7NekaTablica"))
                // {
                //     Supervisor.CreateDatabaseTable("S7NekaTablica", "(SupacPrvi varchar(32), StupacDrugi varchar(64))");
                // }
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile(ex.Message);
                Supervisor.ToLogFile("Something went wrong with database runtime table. Exiting application");
                return;
            }

            // Multithreading all application threads
            try
            {
                var Thread1Task = Task.Factory.StartNew(() => Thread1.InfiniteTask(Supervisor));
                var Thread2Task = Task.Factory.StartNew(() => Thread2.InfiniteTask(Supervisor));

                Task.WaitAny(Thread1Task, Thread2Task);

                if (Thread1Task.IsCompleted & !Thread2Task.IsCompleted)
                {
                    Supervisor.ToLogFile("Thread1 shut down unexpectedly");
                }
                else if (!Thread1Task.IsCompleted & Thread2Task.IsCompleted)
                {
                    Supervisor.ToLogFile("Thread2 shut down unexpectedly");
                }
                else
                {
                    Supervisor.ToLogFile("Both Thread1 and Thread2 shut down unexpectedly");
                }
            }
            catch (Exception ex)
            {
                Supervisor.ToLogFile("Error in Thread1/Thread2 multithreading " + ex.Message);
            }
        }

        // Exit routine
        static void CurrentDomain_ProcessExit(object? sender, EventArgs e, S7CoreSupervisor supervisor)
        {
            supervisor.ToLogFile("Application closed");
            supervisor.ToLogFile("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }
    }

}
