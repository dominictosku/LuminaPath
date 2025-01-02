using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminaPath.Infrastructure.Helper
{
    public class DatabaseTools
    {
        public static void BackupDatabase(string host, string databaseName, string username, string password, string backupFilePath)
        {
            try
            {
                // Build the mysqldump command
                var processInfo = new ProcessStartInfo
                {
                    FileName = "mysqldump", // Ensure this is in the system's PATH
                    Arguments = $"-h {host} -u {username} -p{password} {databaseName} > {backupFilePath}",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the process
                var process = Process.Start(processInfo);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Database backup completed successfully.");
                }
                else
                {
                    Console.WriteLine($"Error occurred: {process.StandardError.ReadToEnd()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while backing up the database: {ex.Message}");
            }
        }
    }
}
