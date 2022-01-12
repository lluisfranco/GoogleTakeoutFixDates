using System;
using System.IO;

namespace GoogleTakeoutFixDates
{
    class Program
    {
        public static GoogleTakeoutParserService GoogleTakeoutParserService { get; set; }
        static void Main(string[] args)
        {
            try
            {
                PrintHeader();
                var facebook_base_path = Console.ReadLine();

                facebook_base_path = @"C:\Takeout\";// "/home/lluisfranco/Pictures/Fb";//"C:\Takeout\";

                GoogleTakeoutParserService = new GoogleTakeoutParserService(facebook_base_path);
                GoogleTakeoutParserService.Log += (s, e) => { Console.WriteLine(e.LogMessage); };
                GoogleTakeoutParserService.Initialize();
                GoogleTakeoutParserService.ReadPhotosInformationFromFileSystem();
                GoogleTakeoutParserService.ExportInformationToFileSystem();
                PrintSummary();
            }
            catch (DirectoryNotFoundException dex)
            {
                Console.WriteLine($"** ERROR : Folder '{dex.Message}' not found **");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"** ERROR : {ex.Message} **");
            }
        }

        private static void PrintHeader()
        {
            Console.WriteLine($"*****************************************************************************************");
            Console.WriteLine($"** GOOGLE TAKEOUT BACKUP UTILITY - PHOTOS/VIDEOS DATES FIXER                           **");
            Console.WriteLine($"** When you backup your Takeout profile, a ZIP file is generated with all your info.   **");
            Console.WriteLine($"** Once unzipped, there is a folder named 'photos_and_videos which contains all your   **");
            Console.WriteLine($"** albums, profile and timeline photos, organized in folders.                          **");
            Console.WriteLine($"** These folders contains your photos but some metadata has been removed (like date)   **");
            Console.WriteLine($"** This script reads the content of your profile, reading from the HTML pages,         **");
            Console.WriteLine($"** and fixing the photo metadata. After finishing, all then photos are copied          **");
            Console.WriteLine($"** to a new folder called '_Export'                                                    **");
            Console.WriteLine($"*****************************************************************************************");
            Console.WriteLine($"");
            Console.WriteLine($"Enter your Google Takeout backup base path ('C:\\Takeout' (Win) or '/home/<user>/Takeout' (Linux or Mac)");
        }

        private static void PrintSummary()
        {
            Console.WriteLine($"*****************************************************************************************");
            Console.WriteLine($"** EXPORT SUMMARY");
            Console.WriteLine($"** TOTAL ALBUMS: {GoogleTakeoutParserService.TotalAlbumsExported}");
            Console.WriteLine($"** TOTAL PHOTOS: {GoogleTakeoutParserService.TotalPhotosExported}");
            Console.WriteLine($"** TOTAL ERRORS: {GoogleTakeoutParserService.TotalErrors}");
            Console.WriteLine($"** Elapsed Time: {GoogleTakeoutParserService.Clock.ElapsedMilliseconds / 1000:n0}s.");
            Console.WriteLine($"*****************************************************************************************");
        }
    }

}
