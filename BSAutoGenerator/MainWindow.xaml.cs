//#define _DISABLED_POPUPS_
#define _INTERNAL_BPM_DETECTOR_ // experimental one never works...
//#define _EXPERIMENTAL_SETTINGS_

using System.Windows;
using System.Text.Json;
using BSAutoGenerator.Data;
using BSAutoGenerator.Algorithm;
using System.IO;
using System.Collections.Generic;
using System;
using BSAutoGenerator.Data.Structure;
using System.Linq;
using System.Windows.Data;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Win32;
using BSAutoGenerator.Data.V2;
using Microsoft.VisualBasic;
using BSAutoGenerator.Info;
using System.Runtime;
using System.Windows.Shapes;
using Path = System.IO.Path;
using static BSAutoGenerator.MainWindow;
using NVorbis;
using System.Reflection;
using System.Windows.Forms;
using DragDropEffects = System.Windows.DragDropEffects;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using Binding = System.Windows.Data.Binding;
using DragEventHandler = System.Windows.DragEventHandler;
using SelectionMode = System.Windows.Controls.SelectionMode;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using System.DirectoryServices.ActiveDirectory;
using BSAutoGenerator.BeatSage_Downloader;
using System.Runtime.InteropServices;
using OggVorbisEncoder;
using System.Xml.Linq;
using System.Threading;
using NAudio.Vorbis;
using TagLib.Mpeg;
using NAudio.Wave;
using Chihya.Tempo;
using System.Windows.Forms.PropertyGridInternal;
using System.Threading.Channels;
using BSAutoGenerator.Info.Chains;

namespace BSAutoGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Temporary save the path to song folder
        internal string filePath = "";
        // Ignore null value during serialization
        internal JsonSerializerOptions options = new();
        // Json data
        internal InfoData infoData = new();
        internal List<DifficultyData> difficultyData = new();
        // Information on the difficulty
        internal List<DataItem> dataItem = new();
        internal List<List<string>> oldData = new();

        // Config values... Command line...
        public static bool SILENCE = false;
        public static bool USE_BEATSAGE = false;
        public static bool USE_BEATSAGE_REMAP = false;
        public static bool USE_BEATSAGE_REMAP_DOUBLES = false;
        public static bool ENABLE_OBSTACLES = false;
        public static bool ENABLE_DOT_TRANSITIONS = false;
        public static string PATTERNS_FOLDER = "default";

        // Config values... Difficulty...
        /*int SPEED_EASY = 7;
        int SPEED_STANDARD = 8;
        int SPEED_HARD = 10;
        int SPEED_EXPERT = 12;
        int SPEED_EXPERT_PLUS = 16;*/
        int SPEED_EASY = 8;
        int SPEED_STANDARD = 10;
        int SPEED_HARD = 11;
        int SPEED_EXPERT = 14;
        int SPEED_EXPERT_PLUS = 16;

#if _EXPERIMENTAL_SETTINGS_
        //float BPM_DIVIDER = 2.0f;
        float BPM_DIVIDER = 3.75f;
        //float IRANGE_MULTIPLIER = 0.55f;
        float IRANGE_MULTIPLIER = 0.33333f;

        float IRANGE_EASY = 0.0010f;           // Easy
        float IRANGE_STANDARD = 0.0015f;       // Standard
        float IRANGE_HARD = 0.003f;            // Hard
        float IRANGE_EXPERT = 0.005f;          // Expert
        float IRANGE_EXPERT_PLUS = 0.010f;     // Expert+
#else //!_EXPERIMENTAL_SETTINGS_
        float BPM_DIVIDER = 1.0f;
        float IRANGE_MULTIPLIER = 1.0f;

        /*
        float IRANGE_EASY = 0.0005f;            // Easy
        float IRANGE_STANDARD = 0.00075f;       // Standard
        float IRANGE_HARD = 0.00015f;           // Hard
        float IRANGE_EXPERT = 0.00025f;         // Expert
        float IRANGE_EXPERT_PLUS = 0.005f;      // Expert+
        */
        /*
        float IRANGE_EASY = 0.00025f;            // Easy
        float IRANGE_STANDARD = 0.000375f;       // Standard
        float IRANGE_HARD = 0.000075f;           // Hard
        float IRANGE_EXPERT = 0.000125f;         // Expert
        float IRANGE_EXPERT_PLUS = 0.0025f;      // Expert+
        */
        
        /*
        float IRANGE_EASY = 0.001f;            // Easy
        float IRANGE_STANDARD = 0.001f;       // Standard
        float IRANGE_HARD = 0.001f;           // Hard
        float IRANGE_EXPERT = 0.001f;         // Expert
        float IRANGE_EXPERT_PLUS = 0.001f;      // Expert+
        */

        float IRANGE_EASY = 0.01f;            // Easy
        float IRANGE_STANDARD = 0.01f;       // Standard
        float IRANGE_HARD = 0.01f;           // Hard
        float IRANGE_EXPERT = 0.01f;         // Expert
        float IRANGE_EXPERT_PLUS = 0.01f;      // Expert+
#endif //_EXPERIMENTAL_SETTINGS_

        public MainWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();

            options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Start
            OpenButton.Visibility = Visibility.Visible;
            OpenAudio.Visibility = Visibility.Visible;

            // Prefill DataGrid
            for (int i = 1; i <= 3; ++i)
            {
                var column = new DataGridTextColumn();
                switch (i)
                {
                    case 1: column.Header = "Type";
                        break;
                    case 2: column.Header = "Before";
                        break;
                    case 3: column.Header = "Now";
                        break;
                }
                column.Binding = new Binding("Column" + i);
                DiffDataGrid.Columns.Add(column);
            }

            dataItem.Add(new DataItem { Column1 = "Note", Column2 = "", Column3 = "" });
            dataItem.Add(new DataItem { Column1 = "Bomb", Column2 = "", Column3 = "" });
            dataItem.Add(new DataItem { Column1 = "Obstacle", Column2 = "", Column3 = "" });
            dataItem.Add(new DataItem { Column1 = "Chain", Column2 = "", Column3 = "" });
            dataItem.Add(new DataItem { Column1 = "Arc", Column2 = "", Column3 = "" });
            dataItem.Add(new DataItem { Column1 = "Light", Column2 = "", Column3 = "" });
            dataItem.Add(new DataItem { Column1 = "Boost", Column2 = "", Column3 = "" });

            foreach(var item in dataItem)
            {
                DiffDataGrid.Items.Add(item);
            }

            //// UQ1: Drag/drop support...
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(LightMap_DragEnter);
            this.Drop += new DragEventHandler(LightMap_DragDrop);

            bool run_silent = false;
            string? filename = null;

            // UQ1: Command line file specification support...
            if (args.Length > 1)
            {
                bool skip = false;

                for (int i = 1; i < args.Length; i++)
                {
                    if (skip)
                    {
                        skip = false;
                        continue;
                    }

                    string arg = args[i];

                    if (arg.Contains("--silent", StringComparison.OrdinalIgnoreCase))
                    {
                        run_silent = true;
                    }
                    else if (arg.Contains("--beatsage-remap-doubles", StringComparison.OrdinalIgnoreCase))
                    {
                        USE_BEATSAGE_REMAP_DOUBLES = true;
                        USE_BEATSAGE = true; // also set this, as it is implied...
                    }
                    else if (arg.Contains("--beatsage-remap", StringComparison.OrdinalIgnoreCase))
                    {
                        USE_BEATSAGE_REMAP = true;
                        USE_BEATSAGE = true; // also set this, as it is implied...
                    }
                    else if (arg.Contains("--beatsage", StringComparison.OrdinalIgnoreCase))
                    {
                        USE_BEATSAGE = true;
                    }
                    else if (arg.Contains("--obstacles", StringComparison.OrdinalIgnoreCase))
                    {
                        ENABLE_OBSTACLES = true;
                    }
                    else if (arg.Contains("--dot-transitions", StringComparison.OrdinalIgnoreCase))
                    {
                        ENABLE_DOT_TRANSITIONS = true;
                    }
                    else if (arg.Contains("--bpm", StringComparison.OrdinalIgnoreCase))
                    {
                        if (args.Length > i + 1)
                        {
                            BPM_DIVIDER = float.Parse(args[i + 1]);
                            skip = true; // we just read the next arg, skip it...
                            continue;
                        }
                    }
                    else if (arg.Contains("--irangemultiplier", StringComparison.OrdinalIgnoreCase))
                    {
                        if (args.Length > i + 1)
                        {
                            IRANGE_MULTIPLIER = float.Parse(args[i + 1]);
                            skip = true; // we just read the next arg, skip it...
                            continue;
                        }
                    }
                    else if (arg.Contains("--patterns", StringComparison.OrdinalIgnoreCase))
                    {
                        if (args.Length > i + 1)
                        {
                            PATTERNS_FOLDER = args[i + 1];
                            skip = true; // we just read the next arg, skip it...
                            continue;
                        }
                    }
                    else
                    {// Filenames...
                        filename = arg.Replace("--", "").Replace("/", "");
                    }
                }
            }

            if (filename != null && filename != "")
            {
                ProcessFile(filename, run_silent);
            }
        }

        /// <summary>
        /// Data for the InfoDataGrid column
        /// </summary>
        public class DataItem
        {
            public string? Column1 { get; set; }
            public string? Column2 { get; set; }
            public string? Column3 { get; set; }
        }

        /// <summary>
        /// Used when files are loaded to show new layout on the window
        /// </summary>
        public void Transition()
        {
            OpenButton.Visibility = Visibility.Collapsed;
            OpenAudio.Visibility = Visibility.Collapsed;
            DiffListBox.Visibility = Visibility.Visible;
            DiffListBox.Width = 150;
            DiffListBox.SelectedIndex = 0;
            // Filling the data to show the user
            DiffListBox.SelectionMode = SelectionMode.Single;
            WindowSize.Width = 600;
            LightButton.Visibility = Visibility.Visible;
            DownLightButton.Visibility = Visibility.Visible;
            SaveButton.Visibility = Visibility.Visible;
            AutomapperButton.Visibility = Visibility.Visible;
            BombButton.Visibility = Visibility.Visible;
            ArcButton.Visibility = Visibility.Visible;
            InvertButton.Visibility = Visibility.Visible;
            LoloppeButton.Visibility = Visibility.Visible;
            ChainButton.Visibility = Visibility.Visible;
            DDButton.Visibility = Visibility.Visible;
            // Show data
            DiffDataGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Fill out the InfoDataGrid with currently selected difficulty
        /// </summary>
        /// <param name="Index">Difficulty</param>
        public void FillDataGrid(int Index)
        {
            List<string> temp = oldData[Index];

            dataItem[0].Column2 = temp[0];
            dataItem[1].Column2 = temp[1];
            dataItem[2].Column2 = temp[2];
            dataItem[3].Column2 = temp[3];
            dataItem[4].Column2 = temp[4];
            dataItem[5].Column2 = temp[5];
            dataItem[6].Column2 = temp[6];

            dataItem[0].Column3 = difficultyData[Index].colorNotes.Count.ToString();
            dataItem[1].Column3 = difficultyData[Index].bombNotes.Count.ToString();
            dataItem[2].Column3 = difficultyData[Index].obstacles.Count.ToString();
            dataItem[3].Column3 = difficultyData[Index].burstSliders.Count.ToString();
            dataItem[4].Column3 = difficultyData[Index].sliders.Count.ToString();
            dataItem[5].Column3 = difficultyData[Index].basicBeatmapEvents.Count.ToString();
            dataItem[6].Column3 = difficultyData[Index].colorBoostBeatmapEvents.Count.ToString();

            DiffDataGrid.Items[0] = dataItem[0];
            DiffDataGrid.Items[1] = dataItem[1];
            DiffDataGrid.Items[2] = dataItem[2];
            DiffDataGrid.Items[3] = dataItem[3];
            DiffDataGrid.Items[4] = dataItem[4];
            DiffDataGrid.Items[5] = dataItem[5];
            DiffDataGrid.Items[6] = dataItem[6];

            DiffDataGrid.Items.Refresh();
        }

        /// <summary>
        /// Select info.dat, verify difficulty, read them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if(filePath == "") // No file are selected yet
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filter = "Info.dat|Info.dat";
                openFileDialog.Title = "Open Info.dat";
                openFileDialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data";
                bool? result = openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (result == true)
                {
                    filePath = openFileDialog.FileName;        
                }
            }
            if(filePath != "") // A file is selected
            {
                string? path = Path.GetDirectoryName(filePath) + "\\";
                string? selectedFileName = Path.GetFileName(filePath);
                if(selectedFileName != null)
                {
                    if (selectedFileName.Equals("Info.dat", StringComparison.OrdinalIgnoreCase))
                    {
                        try // Read the Info.dat
                        {
                            using StreamReader r = new(path + selectedFileName);
                            {
                                while (r.Peek() != -1)
                                {
                                    string json = r.ReadToEnd();
                                    infoData = JsonSerializer.Deserialize<InfoData>(json);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("ERROR: Reading Info.dat");
                            infoData = new();
                            filePath = "";
                        }

                        if(infoData != null)
                        {
                            DiffListBox.Items.Clear(); // Prepare the PathListBox to add all the difficulty inside

                            try
                            {
                                foreach (var difficulty in infoData._difficultyBeatmapSets)
                                {
                                    var type = difficulty._beatmapCharacteristicName;
                                    foreach (var beatmap in difficulty._difficultyBeatmaps)
                                    {
                                        if (System.IO.File.Exists(path + beatmap._beatmapFilename))
                                        {
                                            using StreamReader r = new(path + beatmap._beatmapFilename);
                                            while (r.Peek() != -1)
                                            {
                                                string json = r.ReadToEnd();
                                                if(json.Contains("_version")) // Older version (probably 2.0.0)
                                                {
                                                    OldDifficultyData oldDiffData = JsonSerializer.Deserialize<OldDifficultyData>(json);
                                                    // Convert it to 3.0.0
                                                    difficultyData.Add(new(oldDiffData));
                                                }
                                                else // Version 3.0.0 beatmap
                                                {
                                                    var test = JsonSerializer.Deserialize<DifficultyData>(json);
                                                    difficultyData.Add(test);
                                                }
                                            }

                                            DiffListBox.Items.Add(beatmap._beatmapFilename);

                                            List<string> temp = new();

                                            temp.Add(difficultyData.Last().colorNotes.Count.ToString());
                                            temp.Add(difficultyData.Last().bombNotes.Count.ToString());
                                            temp.Add(difficultyData.Last().obstacles.Count.ToString());
                                            temp.Add(difficultyData.Last().burstSliders.Count.ToString());
                                            temp.Add(difficultyData.Last().sliders.Count.ToString());
                                            temp.Add(difficultyData.Last().basicBeatmapEvents.Count.ToString());
                                            temp.Add(difficultyData.Last().colorBoostBeatmapEvents.Count.ToString());

                                            oldData.Add(temp);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("ERROR: Reading difficulty");
                                MessageBox.Show(ex.Message);
                            }

                            if(difficultyData.Count > 0)
                            {
                                Transition();
                                FillDataGrid(0);
                            }
                            else
                            {
                                filePath = "";
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("ERROR: Info.dat not selected");
                        filePath = "";
                    }
                }
            }
        }

        private void PathListBox_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            //if(DiffDataGrid.Visibility == Visibility.Visible)
            {
                FillDataGrid(DiffListBox.SelectedIndex);
            }
        }

        private void AddLighting(int SelectedIndex)
        {
            List<ColorNote> timing = new();
            bool nerfStrobes = false;
            bool applyToAll = false;
            bool boostLight = true;

            timing.AddRange(difficultyData[SelectedIndex].colorNotes);

#if _DISABLED_POPUPS_
            MessageBoxResult messageBoxResult = MessageBox.Show("Do you want to apply this Lightshow to all the difficulty?", "Light", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                applyToAll = true;
            }
            else
            {
                applyToAll = false;
            }

            messageBoxResult = MessageBox.Show("Do you want to use bomb to light too?", "Light", MessageBoxButton.YesNo);
            if(messageBoxResult == MessageBoxResult.Yes)
            {
                foreach (var bomb in difficultyData[DiffListBox.SelectedIndex].bombNotes)
                {
                    timing.Add(new(bomb));
                }
                timing.OrderBy(o => o.beat);
            }

            messageBoxResult = MessageBox.Show("Do you want to reduce strobes?", "Light", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                nerfStrobes = true;
            }
            else
            {
                nerfStrobes = false;
            }

            messageBoxResult = MessageBox.Show("Do you want to use Boost Light?", "Light", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                boostLight = true;
            }
            else
            {
                boostLight = false;
            }
#endif

            List<ColorBoostEventData> boostEvents;
            List<BasicEventData> basicEvents;
            List<BurstSliderData> burstSliderData = new(difficultyData[SelectedIndex].burstSliders);

            (boostEvents, basicEvents) = Light.CreateLight(timing, burstSliderData, nerfStrobes, boostLight);

            difficultyData[SelectedIndex].colorBoostBeatmapEvents = boostEvents;
            difficultyData[SelectedIndex].basicBeatmapEvents = basicEvents;

            if (applyToAll)
            {
                foreach (var difficulty in difficultyData)
                {
                    difficulty.basicBeatmapEvents = difficultyData[SelectedIndex].basicBeatmapEvents;

                    if (boostLight)
                    {
                        difficulty.colorBoostBeatmapEvents = difficultyData[SelectedIndex].colorBoostBeatmapEvents;
                    }
                }
            }

            FillDataGrid(SelectedIndex);
        }

        /// <summary>
        /// Generate light for selected difficulty
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Light_Click(object? sender, RoutedEventArgs? e)
        {
            AddLighting(DiffListBox.SelectedIndex);
        }

        private void AddDownlight(int SelectedIndex)
        {
            bool applyToAll = false;

#if _DISABLED_POPUPS_
            MessageBoxResult messageBoxResult = MessageBox.Show("Do you want to apply Downlight to all the difficulty?", "Downlight", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                applyToAll = true;
            }
#else
            //applyToAll = true;
#endif

            if (applyToAll)
            {
                foreach (var difficulty in difficultyData)
                {
                    difficulty.basicBeatmapEvents = DownLight.Down(difficulty.basicBeatmapEvents.ToList(), 0.5, 0.25, 5);
                }
            }
            else
            {
                difficultyData[SelectedIndex].basicBeatmapEvents = DownLight.Down(difficultyData[SelectedIndex].basicBeatmapEvents.ToList(), 0.5, 0.25, 5);
            }

            FillDataGrid(SelectedIndex);
        }

        /// <summary>
        /// Turn all long strobes into pulse, remove fast off, set on backlight during long period, remove spam
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Downlight_Click(object? sender, RoutedEventArgs? e)
        {
            AddDownlight(DiffListBox.SelectedIndex);
        }

        private void SaveFile(string? datFilePath)
        {
            bool newVersion = true;

#if _DISABLED_POPUPS_
            MessageBoxResult messageBoxResult = MessageBox.Show("Save the map for Beat Saber version above 1.20.0 (v3)?", "Version", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.No)
            {
                newVersion = false;
            }
#else
            newVersion = false;// false;
#endif

            try
            {
                string? complete = datFilePath;// Path.GetDirectoryName(datFilePath);

                if (datFilePath == null || complete == null || complete == "")
                {
                    if (filePath != null)
                    {
                        complete = Path.GetDirectoryName(filePath);
                    }
                    else
                    {
                        var systemPath = Environment.
                                     GetFolderPath(
                                         Environment.SpecialFolder.CommonApplicationData
                                     );
                        complete = Path.Combine(systemPath, "BSAutoGenerator");

                        Directory.CreateDirectory(complete);
                    }
                }

                // Set author/editor info before saving...
                if (USE_BEATSAGE_REMAP || USE_BEATSAGE_REMAP_DOUBLES)
                {
                    infoData._levelAuthorName = "BSAutoGenerator (RealFlow v4 BeatSage Remap)";
                    infoData._customData._editors._lastEditedBy = infoData._levelAuthorName;
                }
                else if (!USE_BEATSAGE)
                {
                    infoData._levelAuthorName = "BSAutoGenerator (RealFlow v4)";
                    infoData._customData._editors._lastEditedBy = infoData._levelAuthorName;
                }
                else
                {
                    infoData._levelAuthorName = "BSAutoGenerator (BeatSage)";
                    infoData._customData._editors._lastEditedBy = infoData._levelAuthorName;
                }

                //MessageBox.Show("Saving to " + complete);

                if (newVersion)
                {
                    for (int i = 0; i < DiffListBox.Items.Count; i++)
                    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        string fileName = DiffListBox.Items[i].ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                        string fName = complete + "\\" + fileName;

                        if (System.IO.File.Exists(fName))
                        {
                            System.IO.File.Delete(fName);
                        }

                        //MessageBox.Show("Saving " + fName);

                        using StreamWriter wr = new(fName);
                        wr.WriteLine(JsonSerializer.Serialize<DifficultyData>(difficultyData[i]));
                        wr.Close();
                    }

                    string iName = complete + "\\" + "Info.dat";
                    
                    if (System.IO.File.Exists(iName))
                    {
                        System.IO.File.Delete(iName);
                    }

                    using (StreamWriter wr = new(iName))
                    {
                        wr.WriteLine(JsonSerializer.Serialize<InfoData>(infoData));
                        wr.Close();
                    }
                }
                else
                {
                    for (int i = 0; i < DiffListBox.Items.Count; i++)
                    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        string fileName = DiffListBox.Items[i].ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                        difficultyData[i].version = "2.0.0";

                        string fName = complete + "\\" + fileName;
                        
                        if (System.IO.File.Exists(fName))
                        {
                            System.IO.File.Delete(fName);
                        }

                        //MessageBox.Show("Saving " + fName);

                        using StreamWriter wr = new(fName);
                        wr.WriteLine(JsonSerializer.Serialize<OldDifficultyData>(new(difficultyData[i])));
                        wr.Close();
                    }

                    string iName = complete + "\\" + "Info.dat";

                    if (System.IO.File.Exists(iName))
                    {
                        System.IO.File.Delete(iName);
                    }

                    using (StreamWriter wr = new(complete + "\\" + "Info.dat"))
                    {
                        infoData._version = "2.0.0";
                        wr.WriteLine(JsonSerializer.Serialize<InfoData>(infoData));
                        wr.Close();
                    }
                }

                /*string oggName = complete + "\\" + "song.ogg";
                //MessageBox.Show(name);

                TagLib.File tagFile = TagLib.File.Create(oggName);

                //
                // Cover art...
                //

                if (tagFile.Tag.Pictures.Length > 0)
                {// If we have cover art in the audio file, write a new cover.jpg file from it...
                    TagLib.IPicture pic = tagFile.Tag.Pictures[0];
                    MemoryStream ms = new MemoryStream(pic.Data.Data);

                    if (ms != null)
                    {
                        var currentImage = System.Drawing.Image.FromStream(ms);
                        currentImage.Save(complete + "\\" + "cover.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        ms.Close();
                    }
                }*/

                if (!SILENCE/*datFilePath == null*/)
                {
                    MessageBox.Show("Done");

                    ProcessStartInfo dir = new()
                    {
                        Arguments = complete,
                        FileName = "explorer.exe"
                    };

                    Process.Start(dir);

                    Application.Current.Shutdown();
                }
                else
                {
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Save difficulty and the Info.dat file in ProgramData
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object? sender, RoutedEventArgs? e)
        {
            SaveFile(null);
        }

        private void InvertButton_Click(object sender, RoutedEventArgs e)
        {
            bool limiter = true;

            MessageBoxResult messageBoxResult = MessageBox.Show("Use the limiter? Default: Yes", "Invert", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.No)
            {
                limiter = false;
            }

            (difficultyData[DiffListBox.SelectedIndex].colorNotes, difficultyData[DiffListBox.SelectedIndex].obstacles) = Invert.MakeInvert(difficultyData[DiffListBox.SelectedIndex].colorNotes, difficultyData[DiffListBox.SelectedIndex].obstacles, 0.25, limiter);
            FillDataGrid(DiffListBox.SelectedIndex);
        }

        private void BombButton_Click(object sender, RoutedEventArgs e)
        {
            difficultyData[DiffListBox.SelectedIndex].bombNotes.AddRange(Bomb.CreateBomb(difficultyData[DiffListBox.SelectedIndex].colorNotes));
            FillDataGrid(DiffListBox.SelectedIndex);
        }

        private void LoloppeButton_Click(object sender, RoutedEventArgs e)
        {
            difficultyData[DiffListBox.SelectedIndex].colorNotes = Loloppe.LoloppeGen(difficultyData[DiffListBox.SelectedIndex].colorNotes);
            FillDataGrid(DiffListBox.SelectedIndex);
        }

        private void AutomapperButton_Click(object sender, RoutedEventArgs e)
        {
            float bpm;
            bool limiter = true;

            bpm = infoData._beatsPerMinute;
            try
            {
                bpm = float.Parse(Interaction.InputBox("Enter song BPM (if it's wrong)", "BPM", bpm.ToString()));
            }
            catch (Exception)
            {
                bpm = infoData._beatsPerMinute;
            }

#if _DISABLED_POPUPS_
            MessageBoxResult messageBoxResult = MessageBox.Show("Allow inversed backhands flow? Default: No", "Limiter", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                limiter = false;
            }
#endif

            List<float> timings = new();
            difficultyData[DiffListBox.SelectedIndex].colorNotes.ForEach(o => timings.Add(o.beat));

            (difficultyData[DiffListBox.SelectedIndex].colorNotes, difficultyData[DiffListBox.SelectedIndex].burstSliders, difficultyData[DiffListBox.SelectedIndex].obstacles) = NoteGenerator.AutoMapper(timings, bpm, limiter);
            difficultyData[DiffListBox.SelectedIndex].bombNotes = new();
            FillDataGrid(DiffListBox.SelectedIndex);
        }

        private void DDButton_Click(object sender, RoutedEventArgs e)
        {
            bool limiter = true;
            bool removeSide = false;

            MessageBoxResult messageBoxResult = MessageBox.Show("Reduce the map by half? Default: Yes", "Limiter", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.No)
            {
                limiter = false;
            }

            if(limiter)
            {
                messageBoxResult = MessageBox.Show("Consider side note as up (to remove)? Default: No", "Limiter", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    removeSide = true;
                }
            }

            List<ColorNote> colorNotes;
            List<BurstSliderData> burstSliders;

            (colorNotes, burstSliders) = DoubleDirectional.Emilia(difficultyData[DiffListBox.SelectedIndex].colorNotes, limiter, removeSide);
            difficultyData[DiffListBox.SelectedIndex].colorNotes = colorNotes;
            difficultyData[DiffListBox.SelectedIndex].burstSliders = burstSliders;
            FillDataGrid(DiffListBox.SelectedIndex);
        }

        private void AddChains(int SelectedIndex)
        {
            List<ColorNote> colorNotes;
            List<BurstSliderData> burstSliders;
            (burstSliders, colorNotes) = Chain.Chains(difficultyData[SelectedIndex].colorNotes);
            difficultyData[SelectedIndex].colorNotes = colorNotes;
            difficultyData[SelectedIndex].burstSliders = burstSliders;
            FillDataGrid(SelectedIndex);
        }

        private void ChainButton_Click(object? sender, RoutedEventArgs? e)
        {
            AddChains(DiffListBox.SelectedIndex);
        }

        private void AddArcs(int SelectedIndex)
        {
            List<SliderData> arc;
            arc = Arc.CreateArc(difficultyData[SelectedIndex].colorNotes);
            difficultyData[SelectedIndex].sliders = arc;
            FillDataGrid(SelectedIndex);
        }

        private void ArcButton_Click(object? sender, RoutedEventArgs? e)
        {
            AddArcs(DiffListBox.SelectedIndex);
        }

        private void OpenAudio_Click(object sender, RoutedEventArgs e)
        {
            if (filePath == "") // No file are selected yet
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filter = "*.*gg|*.*gg|*.mp3|*.mp3";
                openFileDialog.Title = "Open audio";
                openFileDialog.InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Beat Saber\\Beat Saber_Data";
                bool? result = openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (result == true)
                {
                    filePath = openFileDialog.FileName;
                }
            }
            if (filePath != "") // A file is selected
            {
                var systemPath = Environment.
                            GetFolderPath(
                                Environment.SpecialFolder.CommonApplicationData
                            );
                var complete = Path.Combine(systemPath, "BSAutoGenerator");

                if(Directory.Exists(complete))
                {
                    Directory.Delete(complete, true);
                }
                Directory.CreateDirectory(complete);

                if(Path.GetExtension(filePath) == ".mp3")
                {
                    System.IO.File.Move(filePath, Path.GetDirectoryName(filePath) + "\\song.mp3");

                    filePath = Path.GetDirectoryName(filePath) + "\\song.mp3";

                    MP3toOGG.ConvertToOgg(filePath, complete);
                }
                else
                {
                    if (System.IO.File.Exists(complete + "\\song.ogg"))
                    {
                        System.IO.File.Delete(complete + "\\song.ogg");
                    }
                    System.IO.File.Copy(filePath, complete + "\\song.ogg");
                }

                List<ColorNote> colorNotes = new();
                List<BurstSliderData> burstSliders;
                List<Obstacle> obstacles = new();

                float bpm = 0;
                bool limiter = true;

                List<float> indistinguishableRange = new();
                /*
                indistinguishableRange.Add(0.01f);      // Expert+
                indistinguishableRange.Add(0.005f);     // Expert
                indistinguishableRange.Add(0.003f);     // Hard
                indistinguishableRange.Add(0.0015f);    // Standard
                indistinguishableRange.Add(0.0010f);    // Easy
                */

                indistinguishableRange.Add(IRANGE_EXPERT_PLUS * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_EXPERT * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_HARD * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_STANDARD * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_EASY * IRANGE_MULTIPLIER);

                BPMDetector detector = new(filePath);
                BPMGroup group = detector.Groups.Where(o => o.Count == detector.Groups.Max(o => o.Count)).First();
                bpm = group.Tempo;
                try
                {
                    bpm = float.Parse(Interaction.InputBox("Enter song BPM", "Automatic BPM detection", bpm.ToString()));
                }
                catch (Exception)
                {
                    bpm = 200;
                }

#if _DISABLED_POPUPS_
                MessageBoxResult messageBoxResult = MessageBox.Show("Allow inversed backhands flow? Default: No", "Limiter", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    limiter = false;
                }
#endif

                for (int i = 0; i < indistinguishableRange.Count; i++)
                {
                    colorNotes = new();
                    burstSliders = new();
                    obstacles = new();

                    (colorNotes, burstSliders, obstacles) = Onset.GetMap(filePath, bpm, indistinguishableRange[i], false/*limiter*/, i);

                    if(colorNotes.Count > 0)
                    {
                        // Create a new file
                        difficultyData.Add(new(colorNotes));
                        difficultyData[i].burstSliders = burstSliders;
                        difficultyData[i].obstacles = obstacles;
                    }
                }

                List<DifficultyBeatmaps> btList = new();
                DifficultyBeatmaps difficultyBeatmaps = new("Easy", 1, "EasyStandard.dat", SPEED_EASY, 0, new(0, 0, "Easy", null, null, null, null, null, null, null));
                btList.Add(difficultyBeatmaps);
                infoData._difficultyBeatmapSets.Add(new("Standard", btList));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("Normal", 3, "NormalStandard.dat", SPEED_STANDARD, 0, new(0, 0, "Normal", null, null, null, null, null, null, null)));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("Hard", 5, "HardStandard.dat", SPEED_HARD, 0, new(0, 0, "Hard", null, null, null, null, null, null, null)));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("Expert", 7, "ExpertStandard.dat", SPEED_EXPERT, 0, new(0, 0, "Expert", null, null, null, null, null, null, null)));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("ExpertPlus", 9, "ExpertPlusStandard.dat", SPEED_EXPERT_PLUS, 0, new(0, 0, "Expert+", null, null, null, null, null, null, null)));
                infoData._beatsPerMinute = bpm;

                DiffListBox.Items.Clear();

                for(int i = 0; i < difficultyData.Count(); i++)
                {
                    switch (i)
                    {
                        case 0: DiffListBox.Items.Add("ExpertPlusStandard.dat"); break;
                        case 1: DiffListBox.Items.Add("ExpertStandard.dat"); break;
                        case 2: DiffListBox.Items.Add("HardStandard.dat"); break;
                        case 3: DiffListBox.Items.Add("NormalStandard.dat"); break;
                        case 4: DiffListBox.Items.Add("EasyStandard.dat"); break;
                    }

                    List<string> temp = new();

                    temp.Add(difficultyData[i].colorNotes.Count.ToString());
                    temp.Add(difficultyData[i].bombNotes.Count.ToString());
                    temp.Add(difficultyData[i].obstacles.Count.ToString());
                    temp.Add(difficultyData[i].burstSliders.Count.ToString());
                    temp.Add(difficultyData[i].sliders.Count.ToString());
                    temp.Add(difficultyData[i].basicBeatmapEvents.Count.ToString());
                    temp.Add(difficultyData[i].colorBoostBeatmapEvents.Count.ToString());

                    oldData.Add(temp);
                }
                
                DiffListBox.SelectedIndex = 0;

                if (difficultyData.Count > 0)
                {
                    Transition();
                    FillDataGrid(0);
                }
            }
        }

        // UQ1: Fast drag/drop optimizing...
        private void LightMap_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                e.Effects = DragDropEffects.All;

            //if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
        }

        /*[DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        */

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        private const int STD_OUTPUT_HANDLE = -11;
        private const int MY_CODE_PAGE = 0;//437;
        private static bool showConsole = false;//true; //Or false if you don't want to see the console


        private string ProcessAudioFile(string draggedFilePath)
        {
            float bpm = 0;
            bool limiter = true;

            //
            // Load up the audio file...
            //

            string? path = Path.GetDirectoryName(draggedFilePath) + "\\";
            string? selectedFileName = Path.GetFileName(draggedFilePath);

            //MessageBox.Show("Loading: " + selectedFileName + " (path " + path + ")");

            if (selectedFileName == null)
            {
                return "";
            }

            filePath = draggedFilePath;

            if (filePath != "") // A file is selected
            {
                string folderName = path.Replace("\\song\\", "");
                var complete = folderName;

                if (Path.GetExtension(filePath) == ".mp3")
                {
                    System.IO.File.Move(filePath, Path.GetDirectoryName(filePath) + "\\song.mp3");

                    filePath = Path.GetDirectoryName(filePath) + "\\song.mp3";

                    MP3toOGG.ConvertToOgg(filePath, complete);

                    // Also copy over any cover art...
                    TagLib.File tagFileIn = TagLib.File.Create(filePath);

                    var pics = tagFileIn.Tag.Pictures;

                    if (pics != null)
                    {
                        string oggName = path + "song.ogg";
                        TagLib.File tagFileOut = TagLib.File.Create(oggName);
                        tagFileOut.Tag.Pictures = pics;
                        tagFileOut.Save();
                    }
                }
                else if (selectedFileName != "song.ogg")
                {
                    if (System.IO.File.Exists(complete + "\\song.ogg"))
                    {
                        System.IO.File.Delete(complete + "\\song.ogg");
                    }

                    System.IO.File.Copy(filePath, complete + "\\song.ogg");
                }

                //
                // Song title and artist...
                //

                string name = path + "song.ogg";
                //MessageBox.Show(name);

                TagLib.File tagFile = TagLib.File.Create(name);
                var SONG_NAME = tagFile.Tag.Title;
                var ARTIST_NAME = tagFile.Tag.AlbumArtists.FirstOrDefault();
                //var albumTitle = tagFile.Tag.Album;

                if (SONG_NAME == null || SONG_NAME == "")
                {
                    //SONG_NAME = "Unknown";
                    SONG_NAME = Microsoft.VisualBasic.Interaction.InputBox("Please supply a song name.", "Title", "Unknown");
                }

                if (ARTIST_NAME == null || ARTIST_NAME == "")
                {
                    //ARTIST_NAME = "Unknown";
                    ARTIST_NAME = Microsoft.VisualBasic.Interaction.InputBox("Please supply an artist name.", "Title", "Unknown");
                }

                //MessageBox.Show("Artist: " + ARTIST_NAME + ". Song: " + SONG_NAME);

                infoData._songName = SONG_NAME;
                infoData._songAuthorName = ARTIST_NAME;

                //MessageBox.Show("SONG_NAME: " + SONG_NAME + ". ARTIST_NAME: " + ARTIST_NAME);

                //
                // Cover art...
                //

                if (tagFile.Tag.Pictures.Length > 0)
                {// If we have cover art in the audio file, write a new cover.jpg file from it...
                    TagLib.IPicture pic = tagFile.Tag.Pictures[0];
                    MemoryStream ms = new MemoryStream(pic.Data.Data);

                    if (ms != null /*&& ms.Length > 4096*/)
                    {
                        var currentImage = System.Drawing.Image.FromStream(ms);

                        // Load thumbnail into PictureBox
                        //AlbumArt.Image = currentImage.GetThumbnailImage(100, 100, null, System.IntPtr.Zero);

                        currentImage.Save(path + "cover.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                        ms.Close();
                    }
                }

                //MessageBox.Show("tagFile done.");

                List <ColorNote> colorNotes = new();
                List<BurstSliderData> burstSliders;
                List<Obstacle> obstacles = new();

                List<float> indistinguishableRange = new();
                /*
                indistinguishableRange.Add(0.01f);      // Expert+
                indistinguishableRange.Add(0.005f);     // Expert
                indistinguishableRange.Add(0.003f);     // Hard
                indistinguishableRange.Add(0.0015f);    // Standard
                indistinguishableRange.Add(0.0010f);    // Easy
                */

                indistinguishableRange.Add(IRANGE_EXPERT_PLUS * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_EXPERT * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_HARD * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_STANDARD * IRANGE_MULTIPLIER);
                indistinguishableRange.Add(IRANGE_EASY * IRANGE_MULTIPLIER);

#if _INTERNAL_BPM_DETECTOR_
                BPMDetector detector = new(filePath);
                BPMGroup group = detector.Groups.Where(o => o.Count == detector.Groups.Max(o => o.Count)).First();
                bpm = group.Tempo;

#if _DISABLED_POPUPS_
                try
                {
                    bpm = float.Parse(Interaction.InputBox("Confirm song BPM", "Automatic BPM detection", bpm.ToString()));
                }
                catch (Exception)
                {
                    bpm = 200;
                }
#endif //_DISABLED_POPUPS_

                //bpm /= BPM_DIVIDER;
#else //!_INTERNAL_BPM_DETECTOR_

                try
                {
                    //var wav = Chihya.Tempo.EnergyTempoDetector.WaveReader.ReadWaveFile(fileName);

                    //MessageBox.Show("detect BPM begins.");

                    if (Path.GetExtension(filePath) == ".ogg" || Path.GetExtension(filePath) == ".egg")
                    {
                        int start = 0;
                        int length = 0;

                        using (VorbisWaveReader reader = new VorbisWaveReader(filePath))
                        {
                            // Originally the sample rate was constant (44100), and the number of channels was 2. 
                            // Let's just in case take them from file's properties
                            var sampleRate = reader.WaveFormat.SampleRate;
                            var channels = reader.WaveFormat.Channels;

                            int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                            if (bytesPerSample == 0)
                            {
                                bytesPerSample = 2; // assume 16 bit
                            }

                            int sampleCount = (int)reader.Length / bytesPerSample;

                            // Read the wave data

                            start *= channels * sampleRate;
                            length *= channels * sampleRate;
                            /*if (start >= sampleCount)
                            {
                                groups = new BPMGroup[0];
                                return;
                            }*/

                            if (length == 0 || start + length >= sampleCount)
                            {
                                length = sampleCount - start;
                            }

                            length = (int)(length / channels) * channels;

                            ISampleProvider sampleReader = reader.ToSampleProvider();
                            float[] samples = new float[length];
                            sampleReader.Read(samples, start, length);

                            byte[] samplesByte = new byte[length];
                            for (int s = 0; s < samples.Length; s++)
                                samplesByte[s] = (byte)samples[s];


                            // Use a config preset.
                            var config = Chihya.Tempo.EnergyTempoDetectorConfig.For44KHz;

                            var filter = new ButterworthFilter(5000, 44100);

                            var properties = new SignalProperties(channels, samples.Length / 2, sampleRate, SignalSampleFormat.Unsigned8Bit);

                            // Create a tempo detector using known information.
                            var bpmdetector = new Chihya.Tempo.EnergyTempoDetector(samplesByte/*wav.data*/, properties/*wav.properties*/, config, filter);

                            // Detect tempo.
                            var tempo = bpmdetector.Detect();

                            // Print out the result. BPM = 0 means detection failed.
                            //Console.WriteLine($"Starts from {tempo.BeatStart}, BPM is {tempo.BeatsPerMinute:0.00}");

                            bpm = tempo.BeatsPerMinute;
                        }
                    }
                    else
                    {
                        int start = 0;
                        int length = 0;

                        using (MediaFoundationReader reader = new MediaFoundationReader(filePath))
                        {
                            // Originally the sample rate was constant (44100), and the number of channels was 2. 
                            // Let's just in case take them from file's properties
                            var sampleRate = reader.WaveFormat.SampleRate;
                            var channels = reader.WaveFormat.Channels;

                            int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                            if (bytesPerSample == 0)
                            {
                                bytesPerSample = 2; // assume 16 bit
                            }

                            int sampleCount = (int)reader.Length / bytesPerSample;

                            // Read the wave data

                            start *= channels * sampleRate;
                            length *= channels * sampleRate;
                            /*if (start >= sampleCount)
                            {
                                groups = new BPMGroup[0];
                                return;
                            }*/

                            if (length == 0 || start + length >= sampleCount)
                            {
                                length = sampleCount - start;
                            }

                            length = (int)(length / channels) * channels;

                            ISampleProvider sampleReader = reader.ToSampleProvider();
                            float[] samples = new float[length];
                            sampleReader.Read(samples, start, length);

                            byte[] samplesByte = new byte[length];
                            for (int s = 0; s < samples.Length; s++)
                                samplesByte[s] = (byte)samples[s];


                            // Use a config preset.
                            var config = Chihya.Tempo.EnergyTempoDetectorConfig.For44KHz;

                            var filter = new ButterworthFilter(5000, 44100);

                            //var properties = new SignalProperties(channels, samples.Length / 2, sampleRate, SignalSampleFormat.Unsigned8Bit);

                            int bitDepth = bytesPerSample * 8;
                            SignalSampleFormat format;

                            if (bitDepth == 8)
                            {
                                format = SignalSampleFormat.Unsigned8Bit;
                            }
                            else if (bitDepth == 16)
                            {
                                format = SignalSampleFormat.Signed16Bit;
                            }
                            else if (bitDepth == 32)
                            {
                                // Don't seem to have this flag anywhere in converter...
                                /*if (fmtCode == 1)
                                {
                                    format = SignalSampleFormat.Signed32Bit;
                                }
                                else if (fmtCode == 3)
                                {
                                    format = SignalSampleFormat.Float32Bit;
                                }
                                else*/
                                {
                                    var fmtCode = "fmtCode bit";
                                    throw new NotSupportedException($"Format {fmtCode} is not supported while bit depth = {bitDepth}.");
                                }
                            }
                            else
                            {
                                throw new NotSupportedException($"Bit depth is not supported: {bitDepth}.");
                            }

                            var dataSize = samples.Length; // ?!?!?!?!

                            var samplesPerChannel = dataSize / channels / (bitDepth / 8);
                            var properties = new SignalProperties(channels, samplesPerChannel, sampleRate, format);


                            // Create a tempo detector using known information.
                            var bpmdetector = new Chihya.Tempo.EnergyTempoDetector(samplesByte/*wav.data*/, properties/*wav.properties*/, config, filter);

                            // Detect tempo.
                            var tempo = bpmdetector.Detect();

                            // Print out the result. BPM = 0 means detection failed.
                            //Console.WriteLine($"Starts from {tempo.BeatStart}, BPM is {tempo.BeatsPerMinute:0.00}");

                            bpm = tempo.BeatsPerMinute;
                        }
                    }

                    MessageBox.Show("bpm detection done: " + bpm.ToString());

                    /*try
                    {
                        bpm = float.Parse(Interaction.InputBox("Primary method of BPM detection success.\n\nConfirm song BPM", "Automatic BPM detection", bpm.ToString()));
                    }
                    catch (Exception)
                    {
                        bpm = 0;
                    }*/
                }
                catch (Exception e)
                {
                    MessageBox.Show("bpm detection failed:\n" + e);
                    bpm = 0;
                }

                if (bpm == 0)
                {// Failed, use original detector...
                    BPMDetector detector = new(filePath);
                    BPMGroup group = detector.Groups.Where(o => o.Count == detector.Groups.Max(o => o.Count)).First();
                    bpm = group.Tempo;

                    try
                    {
                        bpm = float.Parse(Interaction.InputBox("Primary method of BPM detection failed. Secondary method was used as a fallback.\n\nConfirm song BPM", "Automatic BPM detection", bpm.ToString()));
                    }
                    catch (Exception)
                    {
                        bpm = 200;
                    }
                }
#endif //_INTERNAL_BPM_DETECTOR_

#if _DISABLED_POPUPS_
                try
                {
                    bpm = float.Parse(Interaction.InputBox("Enter song BPM", "Automatic BPM detection", bpm.ToString()));
                }
                catch (Exception)
                {
                    bpm = 200;
                }

                MessageBoxResult messageBoxResult = MessageBox.Show("Allow inversed backhands flow? Default: No", "Limiter", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    limiter = false;
                }
#endif

                for (int i = 0; i < indistinguishableRange.Count; i++)
                {
                    colorNotes = new();
                    burstSliders = new();
                    obstacles = new();

                    //(colorNotes, burstSliders, obstacles) = Onset.GetMap(filePath, bpm, indistinguishableRange[i], limiter);
                    (colorNotes, burstSliders, obstacles) = Onset.GetMap(filePath, bpm / BPM_DIVIDER, indistinguishableRange[i], false/*limiter*/, i);

                    if (BPM_DIVIDER != 1.0f)
                    {// If using a custom divider, convert the note times back to the real BPM, as beatsaber quest seems to hate low BPM songs.
                        foreach (ColorNote n in colorNotes)
                        {
                            n.beat *= BPM_DIVIDER;
                        }

                        foreach (BurstSliderData n in burstSliders)
                        {
                            n.beat *= BPM_DIVIDER;
                        }

                        foreach (Obstacle n in obstacles)
                        {
                            n.beat *= BPM_DIVIDER;
                        }
                    }

                    if (colorNotes.Count > 0)
                    {
                        // Create a new file
                        difficultyData.Add(new(colorNotes));
                        difficultyData[i].burstSliders = burstSliders;
                        difficultyData[i].obstacles = obstacles;
                    }
                }

                List<DifficultyBeatmaps> btList = new();
                DifficultyBeatmaps difficultyBeatmaps = new("Easy", 1, "EasyStandard.dat", SPEED_EASY, 0, new(0, 0, "Easy", null, null, null, null, null, null, null));
                btList.Add(difficultyBeatmaps);
                infoData._difficultyBeatmapSets.Add(new("Standard", btList));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("Normal", 3, "NormalStandard.dat", SPEED_STANDARD, 0, new(0, 0, "Normal", null, null, null, null, null, null, null)));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("Hard", 5, "HardStandard.dat", SPEED_HARD, 0, new(0, 0, "Hard", null, null, null, null, null, null, null)));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("Expert", 7, "ExpertStandard.dat", SPEED_EXPERT, 0, new(0, 0, "Expert", null, null, null, null, null, null, null)));
                infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Add(new("ExpertPlus", 9, "ExpertPlusStandard.dat", SPEED_EXPERT_PLUS, 0, new(0, 0, "Expert+", null, null, null, null, null, null, null)));
                infoData._beatsPerMinute = bpm;

                DiffListBox.Items.Clear();

                for (int i = 0; i < difficultyData.Count(); i++)
                {
                    switch (i)
                    {
                        case 0: DiffListBox.Items.Add("ExpertPlusStandard.dat"); break;
                        case 1: DiffListBox.Items.Add("ExpertStandard.dat"); break;
                        case 2: DiffListBox.Items.Add("HardStandard.dat"); break;
                        case 3: DiffListBox.Items.Add("NormalStandard.dat"); break;
                        case 4: DiffListBox.Items.Add("EasyStandard.dat"); break;
                    }

                    List<string> temp = new();

                    temp.Add(difficultyData[i].colorNotes.Count.ToString());
                    temp.Add(difficultyData[i].bombNotes.Count.ToString());
                    temp.Add(difficultyData[i].obstacles.Count.ToString());
                    temp.Add(difficultyData[i].burstSliders.Count.ToString());
                    temp.Add(difficultyData[i].sliders.Count.ToString());
                    temp.Add(difficultyData[i].basicBeatmapEvents.Count.ToString());
                    temp.Add(difficultyData[i].colorBoostBeatmapEvents.Count.ToString());

                    oldData.Add(temp);
                }

                DiffListBox.SelectedIndex = 0;

                if (difficultyData.Count > 0)
                {
                    Transition();
                    FillDataGrid(0);
                }

                filePath = complete + "Info.dat";
                //SaveFile(filePath);

                //MessageBox.Show("file: " + filePath);
            }

            //
            // Auto-map this new music file...
            //


            bpm = infoData._beatsPerMinute;
#if _DISABLED_POPUPS_
            try
            {
                bpm = float.Parse(Interaction.InputBox("Enter song BPM (if it's wrong)", "BPM", bpm.ToString()));
            }
            catch (Exception)
            {
                bpm = infoData._beatsPerMinute;
            }

            MessageBoxResult messageBoxResult = MessageBox.Show("Allow inversed backhands flow? Default: No", "Limiter", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                limiter = false;
            }
#endif

            List<float> timings = new();
            difficultyData[DiffListBox.SelectedIndex].colorNotes.ForEach(o => timings.Add(o.beat));

            (difficultyData[DiffListBox.SelectedIndex].colorNotes, difficultyData[DiffListBox.SelectedIndex].burstSliders, difficultyData[DiffListBox.SelectedIndex].obstacles) = NoteGenerator.AutoMapper(timings, bpm, limiter);
            difficultyData[DiffListBox.SelectedIndex].bombNotes = new();
            FillDataGrid(DiffListBox.SelectedIndex);

            /*
            //
            // Free shit...
            //
            //filePath = "";
            // Ignore null value during serialization
            options = new();
            // Json data
            infoData = new();
            //difficultyData = new();
            difficultyData.Clear();
            // Information on the difficulty
            dataItem.Clear();
            oldData.Clear();
            */

            return filePath;
        }

        private string ProcessBeatSageFile(string draggedFilePath)
        {
            //this.Show();
            //this.OpenAudio.Visibility = Visibility.Hidden;
            //this.OpenButton.Visibility = Visibility.Hidden;

            if (showConsole)
            {
                AllocConsole();
                IntPtr stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                Microsoft.Win32.SafeHandles.SafeFileHandle safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true);
                FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(MY_CODE_PAGE);
                StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }


            string? path = Path.GetDirectoryName(draggedFilePath) + "\\";
            string? selectedFileName = Path.GetFileName(draggedFilePath);

            string selectedDifficulties = "Hard,Expert,Normal,ExpertPlus";
            //string selectedGameModes = "Standard,90Degree,NoArrows,OneSaber";
            //string selectedSongEvents = "DotBlocks,Obstacles,Bombs";
            string selectedGameModes = "Standard,NoArrows,OneSaber";
            
            if (USE_BEATSAGE_REMAP || USE_BEATSAGE_REMAP_DOUBLES)
            {
                selectedGameModes = "Standard";
            }
            
            string selectedSongEvents = "DotBlocks,Obstacles";
            string selectedEnvironment = "DefaultEnvironment";
            string selectedModelVersion = "v2-flow"; // V2

            string filePath = path.TrimEnd('\r', '\n');

            //Console.WriteLine("File Path: " + filePath);

            DownloadManager dlm = new DownloadManager(path);

            dlm.Add(new Download()
            {
                Number = DownloadManager.downloads.Count + 1,
                YoutubeID = "",
                Title = "???",
                Artist = "???",
                Status = "Queued",
                Difficulties = selectedDifficulties,
                GameModes = selectedGameModes,
                SongEvents = selectedSongEvents,
                FilePath = draggedFilePath,
                FileName = System.IO.Path.GetFileName(filePath),
                Environment = selectedEnvironment,
                ModelVersion = selectedModelVersion,
                IsAlive = false,
                IsCompleted = false
            });


            Download dl = DownloadManager.downloads[0];
            string last_status = "";

            string windowTitle = this.Title;
            this.Title = windowTitle + "(Downloading)";

            while (!dl.IsCompleted)
            {
                if (dl.Status != last_status)
                {
                    Console.WriteLine(dl.Status);
                    last_status = dl.Status;
                    this.Title = windowTitle + "(Downloading - " + dl.Status + ")";
                }

                if (dl.Status == "Unable To Create Level")
                {
                    MessageBox.Show("ProcessBeatSageFile: Beat sage failed to generate a map.");
                    Application.Current.Shutdown();
                    return "";
                }

                System.Threading.Thread.Sleep(1000);
            }

            this.Title = windowTitle;

            string downloadedFileDir = dl.Title + " - " + dl.Artist;


            // Move extracted temp folder to the original location...
            string downloadedPath = path + downloadedFileDir + @"\";
            string finalPath = path;
            var d = new DirectoryInfo(downloadedPath); //Assuming Test is your Folder

            try
            {
                //MessageBox.Show("downlaodedPath: " + downloadedPath + " >>> finalPath " + finalPath);

                FileInfo[] Files = d.GetFiles("*.*");

                if (Files.Length <= 0)
                {
                    MessageBox.Show("ProcessBeatSageFile: Beat sage failed to generate any files.");
                    Application.Current.Shutdown();
                    return "";
                }

                foreach (FileInfo file in Files)
                {
                    //MessageBox.Show("file: " + file.Name);
                    file.MoveTo(finalPath + file.Name, true);
                }

                // Delete the now empty temp folder...
                //MessageBox.Show("delete: " + downloadedPath);
                Directory.Delete(downloadedPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Application.Current.Shutdown();
            }

            // Return the new dat file location, ready for processing...
            string returnPath = finalPath + "Info.dat";
            filePath = returnPath;
            return returnPath;
        }

        private void LoadDatFile(string draggedFilePath)
        {
            //
            // Load...
            //

            string windowTitle = this.Title;
            this.Title = windowTitle + " (Loading)";

            string? path = Path.GetDirectoryName(draggedFilePath) + "\\";
            string? selectedFileName = Path.GetFileName(draggedFilePath);

            //MessageBox.Show("Loading: " + selectedFileName + " (path " + path + ")");

            if (selectedFileName == null)
            {
                MessageBox.Show("LoadDatFile: No dat file selected.");
                Application.Current.Shutdown();
                return;
            }

            if (selectedFileName.Equals("Info.dat", StringComparison.OrdinalIgnoreCase))
            {
                //MessageBox.Show("Processing: " + selectedFileName + " (path " + path + ")");

                filePath = draggedFilePath;

                try // Read the Info.dat
                {
                    using StreamReader r = new(path + selectedFileName);
                    {
                        while (r.Peek() != -1)
                        {
                            string json = r.ReadToEnd();
                            infoData = JsonSerializer.Deserialize<InfoData>(json);
                        }
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("ERROR: Reading Info.dat");
                    infoData = new();
                    filePath = "";
                    Application.Current.Shutdown();
                    return;
                }

                if (infoData != null)
                {
                    DiffListBox.Items.Clear(); // Prepare the PathListBox to add all the difficulty inside

                    try
                    {
                        foreach (var difficulty in infoData._difficultyBeatmapSets)
                        {
                            var type = difficulty._beatmapCharacteristicName;
                            foreach (var beatmap in difficulty._difficultyBeatmaps)
                            {
                                //MessageBox.Show("Loading: " + beatmap._beatmapFilename + " (path " + path + ")");

                                if (System.IO.File.Exists(path + beatmap._beatmapFilename))
                                {
                                    using StreamReader r = new(path + beatmap._beatmapFilename);
                                    while (r.Peek() != -1)
                                    {
                                        string json = r.ReadToEnd();
                                        if (json.Contains("_version")) // Older version (probably 2.0.0)
                                        {
                                            OldDifficultyData oldDiffData = JsonSerializer.Deserialize<OldDifficultyData>(json);
                                            // Convert it to 3.0.0
                                            difficultyData.Add(new(oldDiffData));
                                        }
                                        else // Version 3.0.0 beatmap
                                        {
                                            var test = JsonSerializer.Deserialize<DifficultyData>(json);
                                            difficultyData.Add(test);
                                        }
                                    }

                                    DiffListBox.Items.Add(beatmap._beatmapFilename);
                                    //MessageBox.Show("file: " + beatmap._beatmapFilename + "\n_difficulty: " + beatmap._difficulty);

                                    List<string> temp = new();

                                    temp.Add(difficultyData.Last().colorNotes.Count.ToString());
                                    temp.Add(difficultyData.Last().bombNotes.Count.ToString());
                                    temp.Add(difficultyData.Last().obstacles.Count.ToString());
                                    temp.Add(difficultyData.Last().burstSliders.Count.ToString());
                                    temp.Add(difficultyData.Last().sliders.Count.ToString());
                                    temp.Add(difficultyData.Last().basicBeatmapEvents.Count.ToString());
                                    temp.Add(difficultyData.Last().colorBoostBeatmapEvents.Count.ToString());

                                    beatmap._customData._difficultyLabel = beatmap._difficulty; // UQ1: Added, this was using all default names, expertPlus...

                                    oldData.Add(temp);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("ERROR: Reading difficulty");
                        MessageBox.Show(ex.Message);
                    }

                    if (difficultyData.Count > 0)
                    {
                        Transition();
                        FillDataGrid(0);
                    }
                    else
                    {
                        filePath = "";
                        MessageBox.Show("ERROR: Info.dat contains no difficulty info.");
                        Application.Current.Shutdown();
                        return;
                    }

                    //MessageBox.Show("DAT BPM: " + infoData._beatsPerMinute);

                    if (infoData._beatsPerMinute == 0.0f)
                    {// Missing, try to calculate...
                        this.Title = windowTitle + " (Calculating missing BPM)";

#if _DISABLED_POPUPS_
                        if (!SILENCE)
                        {
                            MessageBox.Show("Info.dat contains no bpm info, will try to detect, and then ask for confirmation.");
                        }
#endif //_DISABLED_POPUPS_

                        BPMDetector detector = new(path + "song.ogg");
                        BPMGroup group = detector.Groups.Where(o => o.Count == detector.Groups.Max(o => o.Count)).First();

                        float bpm = group.Tempo;

//#if _DISABLED_POPUPS_
                        if (!SILENCE)
                        {
                            bpm = float.Parse(Interaction.InputBox("Confirm song BPM", "Automatic BPM detection", bpm.ToString()));
                        }
//#endif //_DISABLED_POPUPS_

                        infoData._beatsPerMinute = bpm;

                        this.Title = windowTitle;
                    }

                    //MessageBox.Show("BPM: " + infoData._beatsPerMinute);
                }
            }
            else
            {
                MessageBox.Show("ERROR: Info.dat not selected");
                filePath = "";
                Application.Current.Shutdown();
                return;
            }


            //MessageBox.Show("Loaded: " + filePath);
        }

        private void ProcessDatFile(string draggedFilePath)
        {
            filePath = draggedFilePath;

            if (filePath == null || filePath == "")
            {
                MessageBox.Show("ProcessDatFile: No dat file loaded.");
                Application.Current.Shutdown();
                return;
            }

            //
            // Process...
            //
            //MessageBox.Show("Processing.");

            if (USE_BEATSAGE_REMAP || USE_BEATSAGE_REMAP_DOUBLES)
            {// Remap the beatsage output with the user's patterns. This way we keep timings from BS, but use the user's favorate patterns on notes...
                string windowTitle = this.Title;
                this.Title = windowTitle + "(Re-mapping)";

                //MessageBox.Show("Re-mapping.");

                for (int i = 0; i < difficultyData.Count(); i++)
                {
                    // Merge any close notes into doubles...
                    (difficultyData[i].colorNotes, difficultyData[i].burstSliders, difficultyData[i].obstacles) = NoteGenerator.CheckDoubles(difficultyData[i].colorNotes, difficultyData[i].burstSliders, difficultyData[i].obstacles);
                    // Remap...
                    (difficultyData[i].colorNotes, difficultyData[i].burstSliders, difficultyData[i].obstacles) = NoteGenerator.Remapper(difficultyData[i].colorNotes, difficultyData[i].burstSliders, difficultyData[i].obstacles, USE_BEATSAGE_REMAP_DOUBLES);

                    if (!ENABLE_OBSTACLES)
                    {// Obstacles are disabled, so remove the ones that beatsage added...
                        difficultyData[i].obstacles = new();
                    }
                }

                this.Title = windowTitle;
            }

            DiffListBox.SelectedIndex = 0;

            for (int s = 0; s < DiffListBox.Items.Count; s++)
            {
                FillDataGrid(s);
            }

            DiffListBox.Visibility = Visibility.Visible;
            
            for (int s = 0; s < DiffListBox.Items.Count; s++)
            {
                //DiffListBox.SelectedIndex = s;
                FillDataGrid(s);
                AddLighting(s);
                AddDownlight(s);
                //AddChains(s);
                AddArcs(s);
            }

            
            //
            // Save...
            //
            //MessageBox.Show("Saving to " + filePath);

            //Save_Click(null, null);
            string savePath = filePath.Contains(".dat") ? Path.GetDirectoryName(filePath) : filePath;
            //MessageBox.Show("savePath " + filePath);
            SaveFile(savePath);
            //SILENCE = true;
            //Save_Click(null, null);

            //DiffListBox.Visibility = Visibility.Hidden;


            //
            // Free shit...
            //
            //filePath = "";
            // Ignore null value during serialization
            //options = new();
            // Json data
            //infoData = new();
            //difficultyData = new();
            //difficultyData.Clear();
            // Information on the difficulty
            //dataItem.Clear();
            //oldData.Clear();


            // Hmm, since the above doesn't seem to make dropping next file work (works on second though, but I don't trust it...)
            Application.Current.Shutdown();
        }

        private void ProcessFile(string fileName, bool silent)
        {
            bool wasAudioFile = false;
            string useFileName = fileName;

            this.Show();
            this.OpenAudio.Visibility = Visibility.Hidden;
            this.OpenButton.Visibility = Visibility.Hidden;

            SILENCE = true;

            if (useFileName.Contains(".ogg") || useFileName.Contains(".mp3"))
            {// Need to generate a completely new map...
                if (!USE_BEATSAGE)
                {
                    useFileName = ProcessAudioFile(useFileName);
                }
                else
                {
                    useFileName = ProcessBeatSageFile(useFileName);
                    //MessageBox.Show("LoadDatFile: " + useFileName);
                    LoadDatFile(useFileName);
                }

                if (useFileName == "")
                {
                    MessageBox.Show("ProcessAudioFile returned no file.");
                    Application.Current.Shutdown();
                    return;
                }

                wasAudioFile = true;
            }
            else
            {
                LoadDatFile(useFileName);
            }

            string windowTitle = this.Title;
            this.Title = windowTitle + "(Processing)";

            // Add lighting...
            //MessageBox.Show("ProcessDatFile: " + useFileName);
            ProcessDatFile(useFileName);
            this.Title = windowTitle;

            if (!silent)
            {
                if (wasAudioFile)
                {
                    MessageBox.Show("Your song has been mapped.");
                }
                else
                {
                    MessageBox.Show("Lighting data created.");
                }
            }
        }

        private void LightMap_DragDrop(object sender, DragEventArgs e)
        {
            string[]? fileList = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (fileList != null)
            {
                bool silent = false;

                if (fileList.Length > 1)
                {
                    silent = true;
                }

                foreach (string fileName in fileList)
                {
                    ProcessFile(fileName, silent);
                }
            }
        }
    }
}
