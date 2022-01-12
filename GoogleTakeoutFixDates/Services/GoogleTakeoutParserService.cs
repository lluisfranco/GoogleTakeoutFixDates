using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GoogleTakeoutFixDates
{
    public class GoogleTakeoutParserService
    {
        public Stopwatch Clock { get; private set; } = new();
        public GoogleTakeoutParser GoogleTakeoutParser { get; private set; } = new();
        public LogDetailEnum LogDetailMode { get; set; } = LogDetailEnum.Normal;
        public event EventHandler<LogEventArgs> Log;
        public int TotalAlbumsExported { get; private set; }
        public int TotalPhotosExported { get; private set; }
        public int TotalErrors { get; private set; }

        const string TAKEOUT_PHOTOS_FOLDER_NAME = "Google Photos";
        const string EXPORT_FOLDER_NAME = "_Export";
        const string EXPORT_PHOTOS_FOLDER_NAME = "Photos";
        const bool USE_ALBUM_NAME_IN_PHOTOS = true;
        const string PHOTOS_EXTENSION = ".jpg";
        const string EXPORT_NO_DATE_PHOTOS_FOLDER_NAME = "_NoDate";

        public void RaiseEventLog(string message, LogDetailEnum detailMode = LogDetailEnum.Normal)
        {
            if (LogDetailMode == LogDetailEnum.Disabled) return;
            if (LogDetailMode >= detailMode)
                Log?.Invoke(this, new LogEventArgs() { LogMessage = message });
        }

        public GoogleTakeoutParserService(string basePath)
        {
            GoogleTakeoutParser.BaseFolderPath = basePath;
        }

        public void Initialize()
        {
            Clock.Start();
            if (string.IsNullOrWhiteSpace(GoogleTakeoutParser.BaseFolderPath))
                throw new Exception("Google Takeout base path cannot be null.");
            var google_takeout_folder = new DirectoryInfo(GoogleTakeoutParser.BaseFolderPath);
            if (!google_takeout_folder.Exists)
                throw new DirectoryNotFoundException(GoogleTakeoutParser.BaseFolderPath);
            var google_takeout_photos_path = Path.GetFullPath(
                Path.Combine(GoogleTakeoutParser.BaseFolderPath, TAKEOUT_PHOTOS_FOLDER_NAME));
            var google_takeout_photos_folder = new DirectoryInfo(google_takeout_photos_path);
            GoogleTakeoutParser.PhotosFolderPath = google_takeout_photos_path;
            if (!google_takeout_photos_folder.Exists)
                throw new DirectoryNotFoundException(GoogleTakeoutParser.PhotosFolderPath);            
            RaiseEventLog($"Initialize - OK ({Clock.ElapsedMilliseconds:n2}ms.)");
        }

        public void ReadPhotosInformationFromFileSystem()
        {
            var google_takeout_photos_folder = new DirectoryInfo(GoogleTakeoutParser.PhotosFolderPath);
            var folders = google_takeout_photos_folder.GetDirectories();
            foreach (var folder in folders)
            {
                var album = GetPhotoAlbumFromFolder(folder);
                if (album != null) GoogleTakeoutParser.PhotoAlbums.Add(album);
            }
            RaiseEventLog($"Reading Info - OK ({Clock.ElapsedMilliseconds:n2}ms.)");
        }

        private PhotosAlbumNode GetPhotoAlbumFromFolder(DirectoryInfo albumFolder)
        {
            try
            {
                RaiseEventLog($"Start - Reading Album Info '{albumFolder.Name}'", LogDetailEnum.Normal);
                var album = new PhotosAlbumNode
                {
                    Name = albumFolder.Name,
                    Date = GetAlbumDateFromName(albumFolder.Name),
                    URL = Path.GetFullPath(albumFolder.FullName)
                };
                var photosFiles = albumFolder.GetFiles();
                foreach (var photoFile in photosFiles)
                {
                    if (photoFile.Extension == PHOTOS_EXTENSION)
                    {
                        var photo = GetPhotoFromFile(photoFile);
                        if (!photo.Date.HasValue) photo.Date = album.Date;
                        photo.AlbumNode = album;
                        album.Photos.Add(photo);
                    }
                }
                RaiseEventLog($"{album.Photos.Count} photos in '{album.Name}'", LogDetailEnum.Verbose);
                RaiseEventLog($"End - Reading Album Info '{album.Name}'", LogDetailEnum.Verbose);
                return album;
            }
            catch (Exception ex)
            {
                TotalErrors++;
                RaiseEventLog($"ERROR - {ex.Message}'");
                throw;
            }
        }

        private DateTime? GetAlbumDateFromName(string name)
        {
            //try
            //{
                if (int.TryParse(name.AsSpan(0, 4), out var year) &&
                    int.TryParse(name.AsSpan(5, 2), out var month) &&
                    int.TryParse(name.AsSpan(8, 2), out var day))
                    return new DateTime(year, month, day);
                else
                    return null;
            //}
            //catch (Exception)
            //{
            //    return DateTime.Today;
            //}
        }

        private PhotoNode GetPhotoFromFile(FileInfo photoFile)
        {
            var photoDate = GetDateFromFile(photoFile);
            var photo = new PhotoNode
            {
                Name = photoFile.Name,
                URL = photoFile.FullName,
                Date = photoDate
            };
            return photo;
        }

        private DateTime? GetDateFromFile(FileInfo photoFile)
        {
            var fileDateMetadata = ImageExtensions.GetDateMetadata(photoFile.FullName);
            if (!fileDateMetadata.HasValue)
            {
                RaiseEventLog($"ERROR - '{photoFile}' does not contain metadata.", LogDetailEnum.Verbose);
                var dateFromJson = GetDateFromJsonInfo(photoFile);
                if (!dateFromJson.HasValue)
                {
                    return null;
                }
                else
                {
                    return dateFromJson.Value;
                }
            }
            else
            {
                return fileDateMetadata.Value;
            }
        }

        private DateTime? GetDateFromJsonInfo(FileInfo photoFile)
        {
            var jsonPhotoFile = new FileInfo($"{photoFile.FullName}.json");
            if (jsonPhotoFile.Exists)
            {
                var jsonContent = File.ReadAllText(jsonPhotoFile.FullName); 
                var jsonInfo = System.Text.Json.JsonSerializer.Deserialize<PhotoJsonInfo>(jsonContent);
                var dateNoUtc = jsonInfo.photoTakenTime.formatted.Replace("UTC", "").Trim();
                return Convert.ToDateTime(dateNoUtc);
            }
            else
            {
                RaiseEventLog($"ERROR - '{jsonPhotoFile}' not found.", LogDetailEnum.Verbose);
                return null;
            }
        }

        public void ExportInformationToFileSystem()
        {
            var exportFolderPath = Path.GetFullPath(
                Path.Combine(GoogleTakeoutParser.BaseFolderPath, EXPORT_FOLDER_NAME));
            var exportFolder = new DirectoryInfo(exportFolderPath);
            if (exportFolder.Exists) exportFolder.Delete(true);
            exportFolder.Create();
            ExportPhotosInformationToFileSystem();
            Clock.Stop();
        }

        public void ExportPhotosInformationToFileSystem()
        {
            var exportPhotosFolderPath = Path.GetFullPath(
                Path.Combine(GoogleTakeoutParser.BaseFolderPath, EXPORT_FOLDER_NAME, EXPORT_PHOTOS_FOLDER_NAME));
            var exportPhotosMainFolder = new DirectoryInfo(exportPhotosFolderPath);
            if (exportPhotosMainFolder.Exists) exportPhotosMainFolder.Delete(true);
            exportPhotosMainFolder.Create();
            foreach (var photoAlbum in GoogleTakeoutParser.PhotoAlbums)
            {
                ExportAlbum(exportPhotosMainFolder, photoAlbum);
            }
            var noDateAlbum = new PhotosAlbumNode()
            {
                Name = EXPORT_NO_DATE_PHOTOS_FOLDER_NAME,
                Date = DateTime.Today,
                URL = Path.GetFullPath(Path.Combine(exportPhotosMainFolder.FullName, EXPORT_NO_DATE_PHOTOS_FOLDER_NAME))
            };
            noDateAlbum.Photos.AddRange(GoogleTakeoutParser.PhotoAlbums.SelectMany(
                a => a.Photos).Where(p => !p.Date.HasValue));
            ExportAlbum(exportPhotosMainFolder, noDateAlbum);
        }

        private void ExportAlbum(DirectoryInfo exportPhotosMainFolder, PhotosAlbumNode photoAlbum)
        {
            int i = 0;
            try
            {
                var albumName = photoAlbum.Name.ReplaceInvalidCharsInFileName();
                RaiseEventLog($"Start - Exporting Album '{albumName}'", LogDetailEnum.Verbose);
                var albumFolder = exportPhotosMainFolder.CreateSubdirectory(albumName);
                var photosWithDate = photoAlbum.Photos.Where(p => p.Date.HasValue);
                foreach (var photo in photosWithDate)
                {
                    i++;
                    ExportPhoto(albumFolder, photo, i);
                }
                RaiseEventLog($"Exported: {photoAlbum.Photos.Count} photos in album: '{albumName}'");
                RaiseEventLog($"End - Exporting Album '{albumName}'", LogDetailEnum.Verbose);
                TotalAlbumsExported++;
            }
            catch (Exception ex)
            {
                TotalErrors++;
                RaiseEventLog($"ERROR - {ex.Message}'");
                throw;
            }
        }

        private void ExportPhoto(DirectoryInfo albumFolder, PhotoNode photo, int i)
        {
            var photoFile = new FileInfo(photo.URL);
            if (photoFile.Exists)
            {
                try
                {
                    var photoFileName = USE_ALBUM_NAME_IN_PHOTOS ?
                        $"{albumFolder.Name}_{i}.{photoFile.Extension}" :
                        photoFile.Name;
                    var newPhotoFile = Path.GetFullPath(
                        Path.Combine(albumFolder.FullName, photoFileName));
                    if (File.Exists(newPhotoFile))
                    {
                        newPhotoFile = GetFileNewName(newPhotoFile);
                    }
                    photoFile.CopyTo(newPhotoFile);
                    ImageExtensions.SaveDateMetadata(newPhotoFile, photo.Date.Value);
                    RaiseEventLog($"Exported: '{photoFile.Name}' to '{albumFolder.Name}'", LogDetailEnum.Verbose);
                    TotalPhotosExported++;
                }
                catch (Exception ex)
                {
                    TotalErrors++;
                    RaiseEventLog($"ERROR - {ex.Message}'");
                    throw;
                }
            }
        }

        private string GetFileNewName(string filename)
        {
            var fi = new FileInfo(filename);
            if (fi.Exists)
            {
                var newname = $"{fi.Name.Replace("." + fi.Extension, null)}_1.{fi.Extension}";
                var newpath = Path.GetFullPath(Path.Combine(fi.DirectoryName, newname));
                return GetFileNewName(newpath);
            }
            else
            {
                return filename;
            }
        }
    }

}
