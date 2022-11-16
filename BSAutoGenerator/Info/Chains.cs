using BSAutoGenerator.Algorithm;
using BSAutoGenerator.Data;
using BSAutoGenerator.Data.Structure;
using BSAutoGenerator.Data.V2;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Media3D;
using static BSAutoGenerator.Info.Chains.Chains;
using static BSAutoGenerator.Info.Enumerator;
using static BSAutoGenerator.Info.Utils;
using static BSAutoGenerator.MainWindow;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Windows.Forms.Design.AxImporter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using BSAutoGenerator.Info.Patterns;
using System.Windows.Documents;
using static BSAutoGenerator.Info.Patterns.Patterns;
using System.Security.Claims;

namespace BSAutoGenerator.Info.Chains
{
    internal class Chains
    {
        float MAX_CHAIN_LENGTH = 16;

        static Random rnd = new Random();

        public int maxAvailableChainLength = 0;

        public int[] numWeightUsages = new int[5];

        public class ChainData
        {
            public int line1;
            public int layer1;
            public int color1;
            public int direction1;
            public int angle1;

            public int line2;
            public int layer2;
            public int color2;
            public int direction2;
            public int angle2;
        }

        public class InputNote
        {
            public int line;
            public int layer;
            public int color;
            public int direction;
            public int angle;
        }

        public class ChainOption
        {
            public List<ChainData> data = new List<ChainData>();
            public List<Obstacle> obstacles = new List<Obstacle>();
            public List<InputNote> previousRedNotes = new List<InputNote>();
            public List<InputNote> previousBlueNotes = new List<InputNote>();
        }

        public List<ChainOption> chainOptions = new List<ChainOption>();

        InputNote CreateInputNoteFromNote(ColorNote note)
        {
            InputNote input = new InputNote();
            input.line = note.line;
            input.layer = note.layer;
            input.color = note.color;
            input.direction = note.direction;
            input.angle = note.angle;
            return input;
        }

        bool CheckIfInputNoteExists(List<InputNote> inputNotes, InputNote newNote)
        {
            foreach (InputNote note in inputNotes)
            {
                //if (note.Equals(newNote))
                if (note.line == newNote.line
                    && note.layer == newNote.layer
                    && note.color == newNote.color
                    && note.direction == newNote.direction
                    && note.angle == newNote.angle)
                {
                    return true;
                }
            }

            return false;
        }


        ChainOption? CheckIfChainOptionExists(List<ChainData> newChain, int checkLength = -1)
        {
            if (checkLength == -1)
            {
                checkLength = newChain.Count;
            }

            foreach (ChainOption oldChain in chainOptions)
            {
                if (oldChain.data.Count == checkLength)
                {
                    bool same = true;

                    for (int j = 0; j < oldChain.data.Count && j < checkLength; j++)
                    {
                        //if (!oldChain.data[j].Equals(newChain[j]))
                        if (!(oldChain.data[j].line1 == newChain[j].line1
                            && oldChain.data[j].layer1 == newChain[j].layer1
                            && oldChain.data[j].color1 == newChain[j].color1
                            && oldChain.data[j].direction1 == newChain[j].direction1
                            && oldChain.data[j].angle1 == newChain[j].angle1
                            && oldChain.data[j].line2 == newChain[j].line2
                            && oldChain.data[j].layer2 == newChain[j].layer2
                            && oldChain.data[j].color2 == newChain[j].color2
                            && oldChain.data[j].direction2 == newChain[j].direction2
                            && oldChain.data[j].angle2 == newChain[j].angle2))
                        {
                            same = false;
                            break;
                        }
                    }

                    if (same)
                    {
                        return oldChain;
                    }
                }
            }

            return null;
        }

        int GetInvertedDirection(int direction)
        {
            switch (direction)
            {
                case CutDirection.UP:
                    return CutDirection.DOWN;
                case CutDirection.DOWN:
                    return CutDirection.UP;
                case CutDirection.LEFT:
                    return CutDirection.RIGHT;
                case CutDirection.RIGHT:
                    return CutDirection.LEFT;
                case CutDirection.UP_LEFT:
                    return CutDirection.DOWN_RIGHT;
                case CutDirection.UP_RIGHT:
                    return CutDirection.DOWN_LEFT;
                case CutDirection.DOWN_LEFT:
                    return CutDirection.UP_RIGHT;
                case CutDirection.DOWN_RIGHT:
                    return CutDirection.UP_LEFT;
            }

            return CutDirection.ANY;
        }

        public void LoadExampleData(Patterns.Patterns scripted_patterns)
        {
            maxAvailableChainLength = 0;

            for (int i = 0; i < 5; i++)
                numWeightUsages[i] = 0;

            string _path = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();

            var d = new DirectoryInfo(_path + "\\patternData\\" + MainWindow.PATTERNS_FOLDER + "\\");

            //MessageBox.Show("path= " + d);

            FileInfo[] Files = d.GetFiles("*.dat");

            if (Files.Length <= 0)
            {
                return;
            }

            try
            { 
                foreach (FileInfo file in Files)
                {
                    string fileName = d.FullName + file.Name;

                    //MessageBox.Show("fileName= " + fileName);

                    if (System.IO.File.Exists(fileName))
                    {
                        List<DifficultyData> difficultyData = new();
                        // Information on the difficulty
                        //List<List<string>> oldData = new();

                        using StreamReader r = new(fileName);
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

                        List<string> temp = new();

                        temp.Add(difficultyData.Last().colorNotes.Count.ToString());
                        temp.Add(difficultyData.Last().bombNotes.Count.ToString());
                        temp.Add(difficultyData.Last().obstacles.Count.ToString());
                        temp.Add(difficultyData.Last().burstSliders.Count.ToString());
                        temp.Add(difficultyData.Last().sliders.Count.ToString());
                        temp.Add(difficultyData.Last().basicBeatmapEvents.Count.ToString());
                        temp.Add(difficultyData.Last().colorBoostBeatmapEvents.Count.ToString());

                        //oldData.Add(temp);

                        // Fix incorrect usages in input data...
                        foreach (DifficultyData dd in difficultyData)
                        {
                            List<ColorNote> notes = dd.colorNotes;
                            List<Obstacle> obstacles = dd.obstacles;

                            ColorNote? previousRed = null;
                            ColorNote? previousBlue = null;

                            // First, merge all close (doubles) notes to the same time...
                            //float doublesMaxTimeVariance = 0.02f;
                            float doublesMaxTimeVariance = 0.04f;

                            for (int i = 0; i < notes.Count - 1; i++)
                            {
                                if (notes[i].beat - notes[i + 1].beat >= -doublesMaxTimeVariance && notes[i].beat - notes[i + 1].beat <= doublesMaxTimeVariance)
                                {
                                    notes[i + 1].beat = notes[i].beat;
                                }
                            }



                            /*
                            //
                            // I was trying doing some fixes for "technically against the grand beat saber wiki rules" stuff here (most of which code is deleted now because it makes maps worse),
                            // but who am I, or the all mighty wiki-writers to decide for you??? I am but a programmer, and they mere opinions, nothing more.
                            // 
                            // If the user likes a map, who am I to override their life choices?
                            // User beware, and don't walk across the road without looking both ways!
                            //
                            // I am not going to mommy you. I choose, to let YOU CHOOSE!!!
                            //
                            // If you punch your dog, or your kid, maybe that is just a life lesson for you, your dog, and your kid?
                            //
                            // Remember you are wearing a HMD. Keep your dog and your kids out of your play area, or don't play.
                            //
                            // I made you a tool, how you use it is your choice.
                            // That is all.
                            //

                            for (int i = 0; i < notes.Count; i++)
                            {
                                if (notes[i].direction == CutDirection.ANY)
                                {// Skip, these are always a valid option...
                                    continue;
                                }

                                if (i + 1 < notes.Count && notes[i].beat == notes[i + 1].beat)
                                {// Skip doubles, tripples, etc...
                                    continue;
                                }

                                // Find the next note in this color... Check if it is technically valid, as mapper's don't always follow the wiki opinions.
                                for (int j = i + 1; j < notes.Count && j - i <= 8; j++)
                                {
                                    if (notes[i].color == notes[j].color)
                                    {
                                        if (notes[j].direction == CutDirection.ANY)
                                        {// Skip, these are always a valid option...
                                            break;
                                        }

                                        if (notes[i].beat == notes[j].beat)
                                        {// Skip doubles, tripples, etc...
                                            break;
                                        }
                                        
                                        if (IntDiff(notes[i].line, notes[j].line) <= 1 
                                            && IntDiff(notes[i].layer, notes[j].layer) <= 1
                                            && notes[i].direction == notes[j].direction)
                                        {// Drumming. This is not technically correct, convert the second to an ANY.
                                            notes[j].direction = CutDirection.ANY;
                                            // Turn into swinging pattern instead...
                                            //notes[j].direction = GetInvertedDirection(notes[i].direction);
                                            break;
                                        }

                                        break;
                                    }
                                }
                            }
                            */


                        
                            if (notes[0].color == ColorType.RED)
                            {
                                previousRed = notes[0];
                            }
                            else if (notes[0].color == ColorType.BLUE)
                            {
                                previousBlue = notes[0];
                            }

                            // Now extract all chains into our data structures...
                            int skipTo = 0;

                            for (int i = 1; i < notes.Count; i++)
                            {
                                if (i <= skipTo && skipTo != 0)
                                {// Already skip to the next double in chain...
                                    if (notes[i].color == ColorType.RED)
                                    {
                                        previousRed = notes[i];
                                    }
                                    else if (notes[i].color == ColorType.BLUE)
                                    {
                                        previousBlue = notes[i];
                                    }

                                    continue;
                                }

                                skipTo = 0;
                                int doubleChainCounter = 0;

                                // Look for new chains...
                                for (int j = i - 1; j < i + (MAX_CHAIN_LENGTH-1) && j < notes.Count - 1; j += 2)
                                {
                                    if (notes[j].beat == notes[j + 1].beat && doubleChainCounter < MAX_CHAIN_LENGTH)
                                    {// Continue chain count...
                                        skipTo = j - 1;
                                        doubleChainCounter = (j - (i - 1)) / 2;
                                    }
                                    else
                                    {// Chain ends here...
                                        break;
                                    }
                                }

                                if (doubleChainCounter > 0)
                                {// Have a chain, record it...
                                    ChainOption newChain = new ChainOption();
                                    List<ChainData> data = new List<ChainData>();
                                    List<Obstacle> walls = new List<Obstacle>();

                                    for (int j = i - 1; j <= skipTo && j < notes.Count; j += 2)
                                    {
                                        /*if (j >= notes.Count)
                                        {
                                            MessageBox.Show("i= " + i + "\nj=" + j + "\nskipTo=" + skipTo + "\ncount=" + notes.Count + "\ndoubleChainCounter=" + doubleChainCounter);
                                        }*/

                                        ColorNote note1 = notes[j];
                                        ColorNote note2 = notes[j + 1];

                                        ChainData item = new ChainData();
                                        item.line1 = note1.line;
                                        item.layer1 = note1.layer;
                                        item.color1 = note1.color;
                                        item.direction1 = note1.direction;
                                        item.angle1 = note1.angle;

                                        item.line2 = note2.line;
                                        item.layer2 = note2.layer;
                                        item.color2 = note2.color;
                                        item.direction2 = note2.direction;
                                        item.angle2 = note2.angle;

                                        data.Add(item);
                                    }

                                    if (MainWindow.ENABLE_OBSTACLES)
                                    {
                                        float startBeat = notes[i - 1].beat;
                                        float endBeat = notes[skipTo - 1].beat;
                                        float patternDuration = endBeat - startBeat;

                                        for (int j = 0; j < obstacles.Count; j++)
                                        {
                                            Obstacle obstacle = obstacles[j];

                                            if (obstacle.beat >= startBeat && obstacle.beat <= endBeat)
                                            {
                                                Obstacle item = new Obstacle(obstacle.beat - startBeat, obstacle.index, obstacle.layer, MathF.Min(obstacle.duration, patternDuration), obstacle.width, obstacle.height);
                                                walls.Add(item);
                                            }
                                        }
                                    }

                                    newChain.data = data;
                                    newChain.obstacles = walls;

                                    ChainOption? existingChain = CheckIfChainOptionExists(data);

                                    if (existingChain == null)
                                    {
                                        newChain.previousRedNotes = new List<InputNote>();
                                        newChain.previousBlueNotes = new List<InputNote>();

                                        /*string redDebug = "Wanted Red:\nli: " + previousRed.line + " la: " + previousRed.layer + " d: " + previousRed.direction + "\n";
                                        string blueDebug = "\n\nWanted Blue:\nli: " + previousBlue.line + " la: " + previousBlue.layer + " d: " + previousBlue.direction + "\n";
                                        MessageBox.Show(redDebug + blueDebug);*/

                                        if (previousRed != null)
                                        {
                                            InputNote newInput = CreateInputNoteFromNote(previousRed);

                                            //MessageBox.Show("newInput Red:\nli: " + newInput.line + " la: " + newInput.layer + " d: " + newInput.direction + "\n");


                                            if (!CheckIfInputNoteExists(newChain.previousRedNotes, newInput))
                                            {
                                                newChain.previousRedNotes.Add(newInput);
                                            }
                                        }
                                        
                                        if (previousBlue != null)
                                        {
                                            InputNote newInput = CreateInputNoteFromNote(previousBlue);

                                            //MessageBox.Show("newInput Blue:\nli: " + newInput.line + " la: " + newInput.layer + " d: " + newInput.direction + "\n");

                                            if (!CheckIfInputNoteExists(newChain.previousBlueNotes, newInput))
                                            {
                                                newChain.previousBlueNotes.Add(newInput);
                                            }
                                        }

                                        chainOptions.Add(newChain);
                                    }
                                    else
                                    {// Still add the previous red and blue notes to the previous lists...
                                        if (previousRed != null)
                                        {
                                            InputNote newInput = CreateInputNoteFromNote(previousRed);

                                            if (!CheckIfInputNoteExists(existingChain.previousRedNotes, newInput))
                                            {
                                                existingChain.previousRedNotes.Add(newInput);
                                            }
                                        }
                                        
                                        if (previousBlue != null)
                                        {
                                            InputNote newInput = CreateInputNoteFromNote(previousBlue);

                                            if (!CheckIfInputNoteExists(existingChain.previousBlueNotes, newInput))
                                            {
                                                existingChain.previousBlueNotes.Add(newInput);
                                            }
                                        }
                                    }
                                }
                                else
                                {// A single note, or a loner double... Maybe do these into structures later as well, to also script those...
                                    if (notes[i - 1].beat == notes[i].beat)
                                    {// Add a lone double possibility to the list...
                                        ChainOption newChain = new ChainOption();
                                        List<ChainData> data = new List<ChainData>();

                                        ColorNote note1 = notes[i - 1];
                                        ColorNote note2 = notes[i];

                                        ChainData item = new ChainData();
                                        item.line1 = note1.line;
                                        item.layer1 = note1.layer;
                                        item.color1 = note1.color;
                                        item.direction1 = note1.direction;
                                        item.angle1 = note1.angle;

                                        item.line2 = note2.line;
                                        item.layer2 = note2.layer;
                                        item.color2 = note2.color;
                                        item.direction2 = note2.direction;
                                        item.angle2 = note2.angle;

                                        data.Add(item);

                                        newChain.data = data;

                                        ChainOption? existingChain = CheckIfChainOptionExists(data);

                                        if (existingChain == null)
                                        {
                                            newChain.previousRedNotes = new List<InputNote>();
                                            newChain.previousBlueNotes = new List<InputNote>();

                                            if (previousRed != null)
                                            {
                                                InputNote newInput = CreateInputNoteFromNote(previousRed);

                                                if (!CheckIfInputNoteExists(newChain.previousRedNotes, newInput))
                                                {
                                                    newChain.previousRedNotes.Add(newInput);
                                                }
                                            }
                                            
                                            if (previousBlue != null)
                                            {
                                                InputNote newInput = CreateInputNoteFromNote(previousBlue);

                                                if (!CheckIfInputNoteExists(newChain.previousBlueNotes, newInput))
                                                {
                                                    newChain.previousBlueNotes.Add(newInput);
                                                }
                                            }

                                            chainOptions.Add(newChain);
                                        }
                                        else
                                        {// Still add the previous red and blue notes to the previous lists...
                                            if (previousRed != null)
                                            {
                                                InputNote newInput = CreateInputNoteFromNote(previousRed);

                                                if (!CheckIfInputNoteExists(existingChain.previousRedNotes, newInput))
                                                {
                                                    existingChain.previousRedNotes.Add(newInput);
                                                }
                                            }
                                            
                                            if (previousBlue != null)
                                            {
                                                InputNote newInput = CreateInputNoteFromNote(previousBlue);

                                                if (!CheckIfInputNoteExists(existingChain.previousBlueNotes, newInput))
                                                {
                                                    existingChain.previousBlueNotes.Add(newInput);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (notes[i].color == ColorType.RED)
                                {
                                    previousRed = notes[i];
                                }
                                else if (notes[i].color == ColorType.BLUE)
                                {
                                    previousBlue = notes[i];
                                }
                            }

                            // Also set up patterns data...
                            scripted_patterns.LoadExampleData(dd);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }

            int[] numOptions = new int[16];

            for (int i = 0; i < 16; i++)
            {
                numOptions[i] = 0;
            }

            foreach (var cd in chainOptions)
            {
                numOptions[cd.data.Count]++;
            }

            for (int i = 1; i < 16; i++)
            {
                if (numOptions[i] == 0)
                {// Copy from a greater one if possibile, to fill out the options...
                    bool copied = false;

                    for (int j = i + 1; j < 16; j++)
                    {
                        if (numOptions[j] > 0)
                        {// This one will do...
                            int origChainLength = j;

                            for (int co = 0; co < chainOptions.Count; co++)
                            {
                                var cd = chainOptions[co];

                                if (cd.data.Count == origChainLength)
                                {
                                    if (CheckIfChainOptionExists(cd.data, i) == null)
                                    {
                                        continue;
                                    }

                                    //MessageBox.Show("Copying option for length " + i + " from " + co + " (length " + cd.data.Count + ")");

                                    ChainOption newChain = new ChainOption();
                                    List<ChainData> data = new List<ChainData>();

                                    for (int c = 0; c < i; c++)
                                    {
                                        ChainData old = cd.data[c];

                                        ChainData item = new ChainData();
                                        item.line1 = old.line1;
                                        item.layer1 = old.layer1;
                                        item.color1 = old.color1;
                                        item.direction1 = old.direction1;
                                        item.angle1 = old.angle1;

                                        item.line2 = old.line2;
                                        item.layer2 = old.layer2;
                                        item.color2 = old.color2;
                                        item.direction2 = old.direction2;
                                        item.angle2 = old.angle2;

                                        data.Add(item);
                                    }

                                    newChain.data = data;

                                    // Copy the previous note options lists here as well...
                                    newChain.previousRedNotes = cd.previousRedNotes;
                                    newChain.previousBlueNotes = cd.previousBlueNotes;

                                    if (CheckIfChainOptionExists(data) == null)
                                    {
                                        chainOptions.Add(newChain);
                                        copied = true;
                                    }
                                }
                            }
                        }

                        if (copied)
                        {
                            break;
                        }
                    }
                }
            }

            // Add known previous red and blue saber start positions to anything starting in the same place...
            foreach (ChainOption option1 in chainOptions)
            {
                foreach (ChainOption option2 in chainOptions)
                {
                    if (option1 == option2)
                    {
                        continue;
                    }

                    //if (option1.data[0].Equals(option2.data[0]))
                    if (option1.data[0].line1 == option2.data[0].line1
                        && option1.data[0].layer1 == option2.data[0].layer1
                        //&& option1.data[0].color1 == option2.data[0].color1
                        && option1.data[0].direction1 == option2.data[0].direction1
                        && option1.data[0].angle1 == option2.data[0].angle1
                        && option1.data[0].line2 == option2.data[0].line2
                        && option1.data[0].layer2 == option2.data[0].layer2
                        //&& option1.data[0].color2 == option2.data[0].color2
                        && option1.data[0].direction2 == option2.data[0].direction2
                        && option1.data[0].angle2 == option2.data[0].angle2)
                    {// These start in the same place... Copy over known start positions to option1...
                        foreach (InputNote previous in option2.previousRedNotes)
                        {
                            if (!CheckIfInputNoteExists(option1.previousRedNotes, previous))
                            {
                                option1.previousRedNotes.Add(previous);
                            }

                            // Testing... Adding reds & blues to both options, should be fine?!?!?!?
                            if (!CheckIfInputNoteExists(option1.previousBlueNotes, previous))
                            {
                                option1.previousBlueNotes.Add(previous);
                            }
                        }

                        foreach (InputNote previous in option2.previousBlueNotes)
                        {
                            if (!CheckIfInputNoteExists(option1.previousBlueNotes, previous))
                            {
                                option1.previousBlueNotes.Add(previous);
                            }

                            // Testing... Adding reds & blues to both options, should be fine?!?!?!?
                            if (!CheckIfInputNoteExists(option1.previousRedNotes, previous))
                            {
                                option1.previousRedNotes.Add(previous);
                            }
                        }
                    }
                }

                //MessageBox.Show("option1 has " + option1.previousRedNotes.Count + " red and " + option1.previousBlueNotes.Count + " blue previous notes.\n");
            }

            for (int i = 0; i < 16; i++)
            {
                numOptions[i] = 0;
            }

            foreach (var cd in chainOptions)
            {
                numOptions[cd.data.Count]++;
            }

            int highest = 0;

            for (int i = 0; i < 16; i++)
            {
                if (numOptions[i] > 0)
                {
                    highest = i;
                }
            }

            maxAvailableChainLength = highest;

            // Finish up the Patterns data as well...
            scripted_patterns.CompleteParternsInfo(Files.Length);


            //
            // For debugging...
            //

            /*
            // Final counts, for debug display...
            string chainsCountInfo = "";

            for (int i = 1; i < 16; i++)
            {
                chainsCountInfo += i.ToString() + " - has " + numOptions[i].ToString() + " possibilities.\n";
            }

            MessageBox.Show("Calculated " + chainOptions.Count + " chain options from " + Files.Length + " example difficulty dat files.\n\n" + chainsCountInfo);
            */
        }

        bool IsUpDirection(int a)
        {
            if (a == CutDirection.UP || a == CutDirection.UP_LEFT || a == CutDirection.UP_RIGHT)
            {
                return true;
            }

            return false;
        }

        bool IsDownDirection(int a)
        {
            if (a == CutDirection.DOWN || a == CutDirection.DOWN_LEFT || a == CutDirection.DOWN_RIGHT)
            {
                return true;
            }

            return false;
        }

        bool IsRightDirection(int a)
        {
            if (a == CutDirection.RIGHT || a == CutDirection.UP_RIGHT || a == CutDirection.DOWN_RIGHT)
            {
                return true;
            }

            return false;
        }

        bool IsLeftDirection(int a)
        {
            if (a == CutDirection.LEFT || a == CutDirection.UP_LEFT || a == CutDirection.DOWN_LEFT)
            {
                return true;
            }

            return false;
        }

        bool IsSimilarDirection(int a, int b)
        {
            if (a == CutDirection.ANY || b == CutDirection.ANY)
            {
                return true;
            }

            if (IsUpDirection(a) && IsUpDirection(b))
            {
                return true;
            }

            if (IsDownDirection(a) && IsDownDirection(b))
            {
                return true;
            }

            if (IsRightDirection(a) && IsRightDirection(b))
            {
                return true;
            }

            if (IsLeftDirection(a) && IsLeftDirection(b))
            {
                return true;
            }

            return false;
        }

        int IntDiff(int a, int b)
        {
            int diff = a - b;
            return (diff > 0) ? diff : -diff;
        }

        bool IsValidDirectionForSwing(int direction, bool leftSwinging, bool rightSwinging, bool downSwinging, bool upSwinging)
        {
            if (direction == CutDirection.ANY)
            {
                return true;
            }

            if (leftSwinging
                && upSwinging
                && (direction == CutDirection.UP_LEFT /*|| direction == CutDirection.UP || direction == CutDirection.LEFT*/))
            {
                return true;
            }

            if (rightSwinging
                && upSwinging
                && (direction == CutDirection.UP_RIGHT /*|| direction == CutDirection.UP || direction == CutDirection.RIGHT*/))
            {
                return true;
            }

            if (leftSwinging
                && downSwinging
                && (direction == CutDirection.DOWN_LEFT /*|| direction == CutDirection.DOWN || direction == CutDirection.LEFT*/))
            {
                return true;
            }

            if (rightSwinging
                && downSwinging
                && (direction == CutDirection.DOWN_RIGHT /*|| direction == CutDirection.DOWN || direction == CutDirection.RIGHT*/))
            {
                return true;
            }

            if (upSwinging
                && (/*direction == CutDirection.UP_LEFT ||*/ direction == CutDirection.UP /*|| direction == CutDirection.UP_RIGHT*/))
            {
                return true;
            }

            if (downSwinging
                && (/*direction == CutDirection.DOWN_LEFT ||*/ direction == CutDirection.DOWN /*|| direction == CutDirection.DOWN_RIGHT*/))
            {
                return true;
            }

            if (leftSwinging
                && (/*direction == CutDirection.DOWN_LEFT ||*/ direction == CutDirection.LEFT /*|| direction == CutDirection.UP_LEFT*/))
            {
                return true;
            }

            if (rightSwinging
                && (/*direction == CutDirection.DOWN_RIGHT ||*/ direction == CutDirection.RIGHT /*|| direction == CutDirection.UP_RIGHT*/))
            {
                return true;
            }

            return false;
        }

        int IsValidStartFrom(ChainOption option, ColorNote redPreviousNote, ColorNote bluePreviousNote)
        {
            bool redGood = false;
            bool blueGood = false;

            ChainData? red = null;
            ChainData? blue = null;

            // Find the first of each color...
            foreach (ChainData cd in option.data)
            {
                if (red == null && cd.color1 == ColorType.RED)
                {
                    red = cd;

                    if ((red.line1 == redPreviousNote.line && red.layer1 == redPreviousNote.layer)
                        || (red.line1 == bluePreviousNote.line && red.layer1 == bluePreviousNote.layer))
                    {// Don't use when the line and layer of a start note is on the same layer and line of the previous notes...
                        return -1;
                    }
                }

                if (blue == null && cd.color1 == ColorType.BLUE)
                {
                    blue = cd;

                    if ((blue.line1 == redPreviousNote.line && blue.layer1 == redPreviousNote.layer)
                        || (blue.line1 == bluePreviousNote.line && blue.layer1 == bluePreviousNote.layer))
                    {// Don't use when the line and layer of a start note is on the same layer and line of the previous notes...
                        return -1;
                    }
                }

                if (red == null && cd.color2 == ColorType.RED)
                {
                    red = cd;

                    if ((red.line2 == redPreviousNote.line && red.layer2 == redPreviousNote.layer)
                        || (red.line2 == bluePreviousNote.line && red.layer2 == bluePreviousNote.layer))
                    {// Don't use when the line and layer of a start note is on the same layer and line of the previous notes...
                        return -1;
                    }
                }

                if (blue == null && cd.color2 == ColorType.BLUE)
                {
                    blue = cd;

                    if ((blue.line2 == redPreviousNote.line && blue.layer2 == redPreviousNote.layer)
                        || (blue.line2 == bluePreviousNote.line && blue.layer2 == bluePreviousNote.layer))
                    {// Don't use when the line and layer of a start note is on the same layer and line of the previous notes...
                        return -1;
                    }
                }

                if (red != null && blue != null)
                {// Done...
                    break;
                }
            }

            if (red != null)
            {// Check directionality...
                bool leftSwinging = false;
                bool rightSwinging = false;
                bool downSwinging = false;
                bool upSwinging = false;
                int direction = CutDirection.ANY;

                if (red.color1 == ColorType.RED)
                {
                    leftSwinging = red.line1 < redPreviousNote.line;
                    rightSwinging = red.line1 > redPreviousNote.line;
                    downSwinging = red.layer1 < redPreviousNote.layer;
                    upSwinging = red.layer1 > redPreviousNote.layer;
                    direction = red.direction1;
                }
                else
                {
                    leftSwinging = red.line2 < redPreviousNote.line;
                    rightSwinging = red.line2 > redPreviousNote.line;
                    downSwinging = red.layer2 < redPreviousNote.layer;
                    upSwinging = red.layer2 > redPreviousNote.layer;
                    direction = red.direction2;
                }

                redGood = IsValidDirectionForSwing(direction, leftSwinging, rightSwinging, downSwinging, upSwinging);
            }

            if (blue != null)
            {// Check directionality...
                bool leftSwinging = false;
                bool rightSwinging = false;
                bool downSwinging = false;
                bool upSwinging = false;
                int direction = CutDirection.ANY;

                if (blue.color1 == ColorType.BLUE)
                {
                    leftSwinging = blue.line1 < bluePreviousNote.line;
                    rightSwinging = blue.line1 > bluePreviousNote.line;
                    downSwinging = blue.layer1 < bluePreviousNote.layer;
                    upSwinging = blue.layer1 > bluePreviousNote.layer;
                    direction = blue.direction1;
                }
                else
                {
                    leftSwinging = blue.line2 < bluePreviousNote.line;
                    rightSwinging = blue.line2 > bluePreviousNote.line;
                    downSwinging = blue.layer2 < bluePreviousNote.layer;
                    upSwinging = blue.layer2 > bluePreviousNote.layer;
                    direction = blue.direction2;
                }

                blueGood = IsValidDirectionForSwing(direction, leftSwinging, rightSwinging, downSwinging, upSwinging);
            }

            if (redGood && blue == null)
            {// No blues in this pattern to worry about...
                return 1;
            }

            if (blueGood && red == null)
            {// No reds in this pattern to worry about...
                return 1;
            }

            return (redGood && blueGood) ? 1 : 0;
        }

        int NoteMatchesChainStart(ChainOption option, ColorNote redPreviousNote, ColorNote bluePreviousNote)
        {
            bool redExists = false;
            bool redSimilar = false;
            bool blueExists = false;
            bool blueSimilar = false;

            int validStart = IsValidStartFrom(option, redPreviousNote, bluePreviousNote);

            if (validStart == 1)
            {// Looks ok procedurally...
                return 4;
            }
            else if (validStart == -1)
            {// Don't use this, starts on the same layer and line as a last note...
                return 0;
            }

            // Has this transition been seen in the mapping data?
            foreach (var reds in option.previousRedNotes)
            {
                if (redPreviousNote.line == reds.line
                    && redPreviousNote.layer == reds.layer
                    && redPreviousNote.color == reds.color
                    && redPreviousNote.direction == reds.direction
                    /*&& redPreviousNote.angle == reds.angle*/)
                {
                    redExists = true;
                    break;
                }
            }

            foreach (var blues in option.previousBlueNotes)
            {
                if (bluePreviousNote.line == blues.line
                    && bluePreviousNote.layer == blues.layer
                    && bluePreviousNote.color == blues.color
                    && bluePreviousNote.direction == blues.direction
                    /*&& bluePreviousNote.angle == blues.angle*/)
                {
                    blueExists = true;
                    break;
                }
            }

            if (redExists && blueExists)
            {
                return 4;
            }
            else
            {
                if (!redExists)
                {// Find similar...
                    foreach (var reds in option.previousRedNotes)
                    {
                        if (IntDiff(redPreviousNote.line, reds.line) <= 1
                            && IntDiff(redPreviousNote.layer, reds.layer) <= 1
                            //&& redPreviousNote.color == reds.color
                            //&& IsSimilarDirection(redPreviousNote.direction, reds.direction)
                            && (redPreviousNote.color == reds.color || redPreviousNote.color == reds.color)
                            /*&& redPreviousNote.angle == reds.angle*/)
                        {
                            redSimilar = true;
                            break;
                        }
                    }
                }
                else
                {// We have an exact match... Use it as a similar...
                    redSimilar = true;
                }

                if (!blueExists)
                {// Find similar...
                    foreach (var blues in option.previousBlueNotes)
                    {
                        if (IntDiff(bluePreviousNote.line, blues.line) <= 1
                            && IntDiff(bluePreviousNote.layer, blues.layer) <= 1
                            //&& bluePreviousNote.color == blues.color
                            //&& IsSimilarDirection(bluePreviousNote.direction, blues.direction)
                            && (bluePreviousNote.color == blues.color || bluePreviousNote.color == blues.color)
                            /*&& bluePreviousNote.angle == blues.angle*/)
                        {
                            blueSimilar = true;
                            break;
                        }
                    }
                }
                else
                {// We have an exact match... Use it as a similar...
                    blueSimilar = true;
                }

                if (redSimilar && blueSimilar)
                {
                    return 3;
                }
                else if (redExists || blueExists)
                {
                    /*string redDebug = "Wanted Red:\nli: " + redPreviousNote.line + " la: " + redPreviousNote.layer + " d: " + redPreviousNote.direction + "\nKnown Red:\n";
                    string blueDebug = "\n\nWanted Blue:\nli: " + bluePreviousNote.line + " la: " + bluePreviousNote.layer + " d: " + bluePreviousNote.direction + "\nKnown Blue:\n";
                    bool first = true;

                    redDebug += "known count: " + option.previousRedNotes.Count + "\n";
                    foreach (var reds in option.previousRedNotes)
                    {
                        if (first)
                        {
                            redDebug += "li: ";
                        }
                        else
                        {
                            redDebug += ", li: ";
                        }

                        redDebug += reds.line.ToString() + " la: " + reds.layer.ToString() + " d: " + reds.direction.ToString();
                        first = false;
                    }

                    first = true;
                    blueDebug += "known count: " + option.previousBlueNotes.Count + "\n";
                    foreach (var blues in option.previousBlueNotes)
                    {
                        if (first)
                        {
                            blueDebug += "li: ";
                        }
                        else
                        {
                            blueDebug += ", li: ";
                        }

                        blueDebug += blues.line.ToString() + " la: " + blues.layer.ToString() + " d: " + blues.direction.ToString();
                        first = false;
                    }

                    MessageBox.Show(redDebug + blueDebug);*/

                    return 2;
                }
                else if (redSimilar || blueSimilar)
                {
                    return 1;
                }
            }

            return 0;
        }

        public ChainOption? SelectChainOfLength(int length, ColorNote? redPreviousNote = null, ColorNote? bluePreviousNote = null)
        {
            try
            {
                int weightUsed = 4;

                List<ChainOption> options = new List<ChainOption>();
                List<ChainOption> secondaryOptions = new List<ChainOption>();
                List<ChainOption> tertiaryOptions = new List<ChainOption>();
                List<ChainOption> fortiaryOptions = new List<ChainOption>();

                if (redPreviousNote != null && bluePreviousNote != null)
                {// If previous notes are specified, then try to find one with a matching start setup...
                    foreach (ChainOption chain in chainOptions)
                    {
                        if (chain.data.Count >= length || chain.data.Count == maxAvailableChainLength)
                        {
                            int chainStartWeight = NoteMatchesChainStart(chain, redPreviousNote, bluePreviousNote);

                            if (chainStartWeight == 4)
                            {// Perfect match...
                                options.Add(chain);
                            }
                            else if (chainStartWeight == 3)
                            {// Similar match...
                                secondaryOptions.Add(chain);
                            }
                            else if (chainStartWeight == 2)
                            {// One perfect hand...
                                tertiaryOptions.Add(chain);
                            }
                            else if (chainStartWeight == 1)
                            {// One hand was similar...
                                fortiaryOptions.Add(chain);
                            }
                        }
                    }

                    if (options.Count <= 0)
                    {// Got nuffin perfect, try shorter chains... Starting with the next highest count, and moving down until something good is found...
                        int testLength = length;

                        while (options.Count <= 0 && testLength > 0)
                        {
                            testLength--;

                            foreach (ChainOption chain in chainOptions)
                            {
                                if (chain.data.Count == testLength)
                                {
                                    int chainStartWeight = NoteMatchesChainStart(chain, redPreviousNote, bluePreviousNote);

                                    if (chainStartWeight == 4)
                                    {// Perfect match...
                                        options.Add(chain);
                                    }
                                }
                            }
                        }
                    }
                }

                if (options.Count <= 0)
                {// Got nuffin, cap!
                    if (secondaryOptions.Count > 0)
                    {// We at least found a good secondary option or two, use those instead...
                        options = secondaryOptions;
                        //MessageBox.Show("using " + options.Count + " secondary options.");
                        weightUsed = 3;
                    }
                    else
                    {
                        if (tertiaryOptions.Count > 0)
                        {// We at least found a good secondary option or two, use those instead...
                            options = tertiaryOptions;
                            //MessageBox.Show("using " + options.Count + " tertiary options.");
                            weightUsed = 2;
                        }
                        else
                        {
                            if (fortiaryOptions.Count > 0)
                            {// We at least found a good secondary option or two, use those instead...
                                options = fortiaryOptions;
                                //MessageBox.Show("using " + options.Count + " fortiary options.");
                                weightUsed = 1;
                            }
                            else
                            {
                                // Fallback, select anything valid...
                                //MessageBox.Show("found no good options, trying any option.");
                                weightUsed = 0;

                                foreach (ChainOption chain in chainOptions)
                                {
                                    if (chain.data.Count == length)
                                    {
                                        options.Add(chain);
                                    }
                                }

                                if (options.Count <= 0)
                                {// Got nuffin, cap!
                                    //MessageBox.Show("Got nuffin, cap!");
                                    return null;
                                }
                            }
                        }
                    }
                }
                /*else
                {
                    MessageBox.Show("found " + options.Count + " primary options.");
                }*/

                //MessageBox.Show("wanted= " + length + " choices= " + options.Count);

                int index = rnd.Next(options.Count);
                //MessageBox.Show("wanted= " + length + " choices= " + options.Count + " selected= " + index);
                numWeightUsages[weightUsed]++;
                return options[index];
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }

            return null;
        }

        public int GetMaxAvailableChainLength()
        {
            return maxAvailableChainLength;
        }
    }
}
