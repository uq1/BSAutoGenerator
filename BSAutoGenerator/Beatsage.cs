using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
//using Newtonsoft.Json;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
//using YoutubeExplode;
//using YoutubeExplode.Videos.Streams;
using TagLib;

namespace BSAutoGenerator.BeatSage_Downloader
{
    /// <summary>
    /// Interaction logic for BeatSage_Controller.xaml
    /// </summary>

    [Serializable]
    public class Download : INotifyPropertyChanged
    {
        private int number;
        private string title;
        private string artist;
        private string status;
        private string difficulties;
        private string gameModes;
        private string songEvents;
        private string filePath;
        private string fileName;
        private string identifier;
        private string environment;
        private string modelVersion;
        private string youtubeID;
        private bool isAlive;
        private bool isCompleted;

        public int Number
        {
            get
            {
                return number;
            }
            set
            {
                number = value;
                RaiseProperChanged();
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                RaiseProperChanged();
            }
        }

        public string Artist
        {
            get
            {
                return artist;
            }
            set
            {
                artist = value;
                RaiseProperChanged();
            }
        }

        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                RaiseProperChanged();
            }
        }

        public string Difficulties
        {
            get
            {
                return difficulties;
            }
            set
            {
                difficulties = value;
                RaiseProperChanged();
            }
        }

        public string GameModes
        {
            get
            {
                return gameModes;
            }
            set
            {
                gameModes = value;
                RaiseProperChanged();
            }
        }

        public string SongEvents
        {
            get
            {
                return songEvents;
            }
            set
            {
                songEvents = value;
                RaiseProperChanged();
            }
        }

        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                filePath = value;
                RaiseProperChanged();
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
                Identifier = fileName;
                RaiseProperChanged();
            }
        }

        public string Identifier
        {
            get
            {
                return identifier;
            }
            set
            {
                identifier = value;
                RaiseProperChanged();
            }
        }

        public string Environment
        {
            get
            {
                return environment;
            }
            set
            {
                environment = value;
                RaiseProperChanged();
            }
        }

        public string ModelVersion
        {
            get
            {
                return modelVersion;
            }
            set
            {
                modelVersion = value;
                RaiseProperChanged();
            }
        }

        public string YoutubeID
        {
            get
            {
                return youtubeID;
            }
            set
            {
                youtubeID = value;
                RaiseProperChanged();
            }
        }

        public bool IsAlive
        {
            get
            {
                return isAlive;
            }
            set
            {
                isAlive = value;
                RaiseProperChanged();
            }
        }

        public bool IsCompleted
        {
            get
            {
                return isCompleted;
            }
            set
            {
                isCompleted = value;
                RaiseProperChanged();
            }
        }

        [field: NonSerializedAttribute()]
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaiseProperChanged([CallerMemberName] string caller = "")
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
    }

    [Serializable]
    public class DownloadManager
    {
        public static bool overwriteExisting = false;//true;
        public static bool automaticExtraction = true;
        public static string outputDirectory = "";

        public static ObservableCollection<Download> downloads = new ObservableCollection<Download>();

        public static readonly HttpClient httpClient = new HttpClient();

        public DownloadManager(string outDir)
        {
            outputDirectory = outDir;

            httpClient.DefaultRequestHeaders.Add("Host", "beatsage.com");
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BeatSage-Downloader/1.2.6");

            Thread worker = new Thread(RunDownloads);
            worker.IsBackground = true;
            worker.SetApartmentState(System.Threading.ApartmentState.STA);
            worker.Start();
        }

        public async void RunDownloads()
        {
            Console.WriteLine("RunDownloads Started");

            int previousNumberOfDownloads = downloads.Count;

            while (true)
            {
                List<Download> incompleteDownloads = new List<Download>();

                foreach (Download download in downloads)
                {
                    if (download.Status == "Queued")
                    {
                        incompleteDownloads.Add(download);
                    }
                }

                //Console.WriteLine("Checking for Downloads...");

                if (incompleteDownloads.Count >= 1)
                {
                    Download currentDownload = incompleteDownloads[0];
                    currentDownload.IsAlive = true;

                    if ((currentDownload.FilePath != "") && (currentDownload.FilePath != null))
                    {
                        try
                        {
                            await CreateCustomLevelFromFile(currentDownload);
                        }
                        catch
                        {
                            currentDownload.Status = "Unable To Create Level";
                            currentDownload.IsCompleted = true;
                        }

                        currentDownload.IsAlive = false;
                    }

                }

                System.Threading.Thread.Sleep(1000);
            }


        }

        public static ObservableCollection<Download> Downloads
        {
            get
            {
                return downloads;
            }
        }

        public void Add(Download download)
        {
            downloads.Add(download);
        }

        static async Task CreateCustomLevelFromFile(Download download)
        {
            download.Status = "Uploading File";

            download.IsCompleted = false;

            //Console.WriteLine(download.FilePath);

            TagLib.File tagFile = TagLib.File.Create(download.FilePath);

            string artistName = "Unknown";
            string trackName = "Unknown";
            byte[] imageData = null;

            //Console.WriteLine("1");

            var invalids = System.IO.Path.GetInvalidFileNameChars();

            if (tagFile.Tag.FirstPerformer != null)
            {
                artistName = String.Join("_", tagFile.Tag.FirstPerformer.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            }
            else
            {
                artistName = Microsoft.VisualBasic.Interaction.InputBox("Please supply an artist name.", "Title", "Unknown");
            }

            //Console.WriteLine("2");

            if (tagFile.Tag.Title != null)
            {
                trackName = String.Join("_", tagFile.Tag.Title.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            }
            else
            {
                trackName = System.IO.Path.GetFileNameWithoutExtension(download.FilePath);

                if (trackName == null || trackName == "song")
                {// Need to fix this for any re-generations...
                    var dirName = new DirectoryInfo(System.IO.Path.GetDirectoryName(download.FilePath)).Name;
                    trackName = dirName;
                    //MessageBox.Show("new track name: " + trackName);
                }
            }

            //Console.WriteLine("3");

            if (tagFile.Tag.Pictures.Count() > 0)
            {
                if (tagFile.Tag.Pictures[0].Data.Data != null)
                {
                    imageData = tagFile.Tag.Pictures[0].Data.Data;
                }
            }

            //Console.WriteLine("4");

            download.Artist = artistName;
            download.Title = trackName;

            string fileName = trackName + " - " + artistName;

            //Console.WriteLine("Artist " + artistName + ". Title " + trackName + ". file " + fileName);

            if (!overwriteExisting)
            {
                if (((!automaticExtraction) && (System.IO.File.Exists(outputDirectory + @"\" + fileName + ".zip"))) || ((automaticExtraction) && (Directory.Exists(outputDirectory + @"\" + fileName))))
                {
                    download.Status = "Already Exists";
                    download.IsAlive = false;
                    download.IsCompleted = true;
                    return;
                }
            }

            //Console.WriteLine("1");

            byte[] bytes = System.IO.File.ReadAllBytes(download.FilePath);

            //Console.WriteLine("2");

            string boundary = "----WebKitFormBoundaryaA38RFcmCeKFPOms";
            var content = new MultipartFormDataContent(boundary);

            //Console.WriteLine("3");

            content.Add(new ByteArrayContent(bytes), "audio_file", /*download.FileName*/download.FilePath);

            //Console.WriteLine("4");

            if (imageData != null)
            {
                var imageContent = new ByteArrayContent(imageData);
                imageContent.Headers.Remove("Content-Type");
                imageContent.Headers.Add("Content-Disposition", "form-data; name=\"cover_art\"; filename=\"cover\"");
                imageContent.Headers.Add("Content-Type", "image/jpeg");
                content.Add(imageContent);
            }

            //Console.WriteLine("5");

            content.Add(new StringContent(trackName), "audio_metadata_title");
            content.Add(new StringContent(artistName), "audio_metadata_artist");
            content.Add(new StringContent(download.Difficulties), "difficulties");
            content.Add(new StringContent(download.GameModes), "modes");
            content.Add(new StringContent(download.SongEvents), "events");
            content.Add(new StringContent(download.Environment), "environment");
            content.Add(new StringContent(download.ModelVersion), "system_tag");

            //Console.WriteLine(content.ToString());

            var response = await httpClient.PostAsync("https://beatsage.com/beatsaber_custom_level_create", content);

            var responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseString);

            JObject jsonString = JObject.Parse(responseString);

            string levelID = (string)jsonString["id"];

            Console.WriteLine(levelID);

            await CheckDownload(levelID, trackName, artistName, download);
        }

        static async Task CheckDownload(string levelId, string trackName, string artistName, Download download)
        {
            download.Status = "Generating Custom Level";

            string url = "https://beatsage.com/beatsaber_custom_level_heartbeat/" + levelId;

            Console.WriteLine(url);

            string levelStatus = "PENDING";


            while (levelStatus == "PENDING")
            {
                try
                {
                    Console.WriteLine(levelStatus);

                    System.Threading.Thread.Sleep(1000);

                    //POST the object to the specified URI 
                    var response = await httpClient.GetAsync(url);

                    //Read back the answer from server
                    var responseString = await response.Content.ReadAsStringAsync();

                    JObject jsonString = JObject.Parse(responseString);

                    levelStatus = (string)jsonString["status"];

                }
                catch
                {
                }

            }

            if (levelStatus == "DONE")
            {
                //download.Status = "DONE";
                RetrieveDownload(levelId, trackName, artistName, download);
            }
        }

        static void RetrieveDownload(string levelId, string trackName, string artistName, Download download)
        {
            download.Status = "Downloading";

            string url = "https://beatsage.com/beatsaber_custom_level_download/" + levelId;

            Console.WriteLine(url);

            WebClient client = new WebClient();
            Uri uri = new Uri(url);

            /*if (outputDirectory == "")
            {
                outputDirectory = @"Downloads";
                //Properties.Settings.Default.Save();
            }*/

            int pathLength = System.IO.Path.GetFullPath(outputDirectory).Count();

            string fileName = trackName + " - " + artistName;

            string filePath;

            if (pathLength + fileName.Count() >= 245)
            {
                filePath = (System.IO.Path.GetFullPath(outputDirectory) + @"\" + fileName).Substring(0, 244 - pathLength);

            }
            else
            {
                filePath = outputDirectory + @"\" + fileName;
            }


            if (automaticExtraction)
            {
                download.Status = "Extracting";

                if (Directory.Exists("temp.zip"))
                {
                    Directory.Delete("temp.zip");
                }

                client.DownloadFile(uri, "temp.zip");

                if (Directory.Exists(filePath))
                {
                    Directory.Delete(filePath, true);
                }

                ZipFile.ExtractToDirectory("temp.zip", filePath);

                if (System.IO.File.Exists("temp.zip"))
                {
                    System.IO.File.Delete("temp.zip");
                }
            }
            else
            {

                if (System.IO.File.Exists(filePath + ".zip"))
                {
                    System.IO.File.Delete(filePath + ".zip");
                }

                client.DownloadFile(uri, filePath + ".zip");
            }


            download.Status = "Completed";
            download.IsAlive = false;
            download.IsCompleted = true;
        }
    }
}
