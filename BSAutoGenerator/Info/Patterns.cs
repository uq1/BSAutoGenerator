//#define _CHECK_NOTE_FLOWS_

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
using static BSAutoGenerator.Info.Patterns.Patterns;
using static BSAutoGenerator.Info.Enumerator;
using static BSAutoGenerator.Info.Utils;
using static BSAutoGenerator.MainWindow;
using static System.Reflection.Metadata.BlobBuilder;
using static System.Windows.Forms.Design.AxImporter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static BSAutoGenerator.Info.Chains.Chains;
using System.Windows.Forms;

namespace BSAutoGenerator.Info.Patterns
{
    internal class Patterns
    {
        float MAX_PATTERN_LENGTH = 16;
        //float MAX_PATTERN_LENGTH = 8;

        static Random rnd = new Random();

        public int maxAvailablePatternLength = 0;

        public int[] numWeightUsages = new int[5];

        public class PatternData
        {
            public int line;
            public int layer;
            public int color;
            public int direction;
            public int angle;
        }

        public class PatternInputNote
        {
            public int line;
            public int layer;
            public int color;
            public int direction;
            public int angle;
        }

        public class PatternOption
        {
            public List<PatternData> data = new List<PatternData>();
            public List<Obstacle> obstacles = new List<Obstacle>();
            public List<PatternInputNote> previousRedNotes = new List<PatternInputNote>();
            public List<PatternInputNote> previousBlueNotes = new List<PatternInputNote>();
        }

        public List<PatternOption> patternOptions = new List<PatternOption>();

        PatternInputNote CreatePatternInputNoteFromNote(ColorNote note)
        {
            PatternInputNote input = new PatternInputNote();
            input.line = note.line;
            input.layer = note.layer;
            input.color = note.color;
            input.direction = note.direction;
            input.angle = note.angle;
            return input;
        }

        bool CheckIfPatternInputNoteExists(List<PatternInputNote> inputNotes, PatternInputNote newNote)
        {
            foreach (PatternInputNote note in inputNotes)
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

        bool CheckIfPatternOptionFlows(List<PatternData> newPattern)
        {
#if _CHECK_NOTE_FLOWS_
            ColorNote? previousRed = null;
            ColorNote? previousBlue = null;

            for (int j = 0; j < newPattern.Count; j++)
            {
                PatternData data = newPattern[j];

                ColorNote note = new ColorNote(0, data.color, data.line, data.layer, data.direction);

                int valid = IsNoteFlowValid(note, previousRed, previousBlue);

                if (valid < 1)
                {
                    return false;
                }

                if (note.color == ColorType.RED)
                {
                    previousRed = note;
                }
                else if (note.color == ColorType.BLUE)
                {
                    previousBlue = note;
                }
            }
#endif //_CHECK_NOTE_FLOWS_

            return true;
        }

        PatternOption? CheckIfPatternOptionExists(List<PatternData> newPattern, int checkLength = -1)
        {
            if (checkLength == -1)
            {
                checkLength = newPattern.Count;
            }

            foreach (PatternOption oldPattern in patternOptions)
            {
                if (oldPattern.data.Count == checkLength)
                {
                    bool same = true;

                    for (int j = 0; j < oldPattern.data.Count && j < checkLength; j++)
                    {
                        //if (!oldPattern.data[j].Equals(newPattern[j]))
                        if (!(oldPattern.data[j].line == newPattern[j].line
                            && oldPattern.data[j].layer == newPattern[j].layer
                            && oldPattern.data[j].color == newPattern[j].color
                            && oldPattern.data[j].direction == newPattern[j].direction
                            && oldPattern.data[j].angle == newPattern[j].angle))
                        {
                            same = false;
                            break;
                        }
                    }

                    if (same)
                    {
                        return oldPattern;
                    }
                }
            }

            return null;
        }

        public void InitPatterns()
        {
            maxAvailablePatternLength = 0;

            for (int i = 0; i < 5; i++)
                numWeightUsages[i] = 0;
        }

        public void LoadExampleData(DifficultyData dd)
        {
            try
            {
                List<ColorNote> notes = dd.colorNotes;
                List<Obstacle> obstacles = dd.obstacles;

                ColorNote? previousRed = null;
                ColorNote? previousBlue = null;

                if (notes[0].color == ColorType.RED)
                {
                    previousRed = notes[0];
                }
                else if (notes[0].color == ColorType.BLUE)
                {
                    previousBlue = notes[0];
                }

                // Now extract all patterns into our data structures...
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
                    int patternCounter = 0;

                    // Look for new patterns...
                    for (int j = i - 1; j < i + (MAX_PATTERN_LENGTH - 1) && j < notes.Count - 1; j += 2)
                    {
                        if (notes[j].beat != notes[j + 1].beat && patternCounter < MAX_PATTERN_LENGTH)
                        {// Continue chain count...
                            skipTo = j - 1;
                            patternCounter = (j - (i - 1));
                        }
                        else
                        {// Pattern ends here...
                            break;
                        }
                    }

                    if (patternCounter > 0)
                    {// Have a chain, record it...
                        PatternOption newPattern = new PatternOption();
                        List<PatternData> data = new List<PatternData>();
                        List<Obstacle> walls = new List<Obstacle>();

                        for (int j = i - 1; j <= skipTo && j < notes.Count - 1; j += 2)
                        {
                            ColorNote note1 = notes[j];
                            ColorNote note2 = notes[j + 1];

                            PatternData item = new PatternData();
                            item.line = note1.line;
                            item.layer = note1.layer;
                            item.color = note1.color;
                            item.direction = note1.direction;
                            item.angle = note1.angle;
                            
                            data.Add(item);

                            PatternData item2 = new PatternData();
                            item2.line = note2.line;
                            item2.layer = note2.layer;
                            item2.color = note2.color;
                            item2.direction = note2.direction;
                            item2.angle = note2.angle;

                            data.Add(item2);
                        }

                        if (MainWindow.ENABLE_OBSTACLES)
                        {
                            float startBeat = notes[i - 1].beat;
                            float endBeat = notes[skipTo - 1].beat;
                            float patternDuration = endBeat - startBeat;

                            for (int j = 0; j < obstacles.Count; j++)
                            {
                                Obstacle obstacle = obstacles[j];

                                if (obstacle.beat >= startBeat && obstacle.beat <= endBeat /*&& MathF.Min(obstacle.duration, patternDuration) >= 1.0f*/)
                                {
                                    Obstacle item = new Obstacle(obstacle.beat - startBeat, obstacle.index, obstacle.layer, MathF.Min(obstacle.duration, patternDuration), obstacle.width, obstacle.height);
                                    walls.Add(item);
                                }
                            }
                        }

                        newPattern.data = data;
                        newPattern.obstacles = walls;

                        bool checkPattern = CheckIfPatternOptionFlows(data);

                        if (!checkPattern)
                        {// This pattern in the original file has some flow issues, ignore it...
                            continue;
                        }

                        PatternOption? existingPattern = CheckIfPatternOptionExists(data);

                        if (existingPattern == null)
                        {
                            newPattern.previousRedNotes = new List<PatternInputNote>();
                            newPattern.previousBlueNotes = new List<PatternInputNote>();

                            /*string redDebug = "Wanted Red:\nli: " + previousRed.line + " la: " + previousRed.layer + " d: " + previousRed.direction + "\n";
                            string blueDebug = "\n\nWanted Blue:\nli: " + previousBlue.line + " la: " + previousBlue.layer + " d: " + previousBlue.direction + "\n";
                            MessageBox.Show(redDebug + blueDebug);*/

                            if (previousRed != null)
                            {
                                PatternInputNote newInput = CreatePatternInputNoteFromNote(previousRed);

                                //MessageBox.Show("newInput Red:\nli: " + newInput.line + " la: " + newInput.layer + " d: " + newInput.direction + "\n");


                                if (!CheckIfPatternInputNoteExists(newPattern.previousRedNotes, newInput))
                                {
                                    newPattern.previousRedNotes.Add(newInput);
                                }
                            }

                            if (previousBlue != null)
                            {
                                PatternInputNote newInput = CreatePatternInputNoteFromNote(previousBlue);

                                //MessageBox.Show("newInput Blue:\nli: " + newInput.line + " la: " + newInput.layer + " d: " + newInput.direction + "\n");

                                if (!CheckIfPatternInputNoteExists(newPattern.previousBlueNotes, newInput))
                                {
                                    newPattern.previousBlueNotes.Add(newInput);
                                }
                            }

                            patternOptions.Add(newPattern);
                        }
                        else
                        {// Still add the previous red and blue notes to the previous lists...
                            if (previousRed != null)
                            {
                                PatternInputNote newInput = CreatePatternInputNoteFromNote(previousRed);

                                if (!CheckIfPatternInputNoteExists(existingPattern.previousRedNotes, newInput))
                                {
                                    existingPattern.previousRedNotes.Add(newInput);
                                }
                            }

                            if (previousBlue != null)
                            {
                                PatternInputNote newInput = CreatePatternInputNoteFromNote(previousBlue);

                                if (!CheckIfPatternInputNoteExists(existingPattern.previousBlueNotes, newInput))
                                {
                                    existingPattern.previousBlueNotes.Add(newInput);
                                }
                            }
                        }
                    }
                    else
                    {// A single note, or a loner double... Maybe do these into structures later as well, to also script those...
                        if (notes[i - 1].beat != notes[i].beat)
                        {// Add a single note to the list...
                            PatternOption newPattern = new PatternOption();
                            List<PatternData> data = new List<PatternData>();

                            ColorNote note1 = notes[i - 1];
                            ColorNote note2 = notes[i];

                            PatternData item = new PatternData();
                            item.line = note1.line;
                            item.layer = note1.layer;
                            item.color = note1.color;
                            item.direction = note1.direction;
                            item.angle = note1.angle;

                            data.Add(item);

                            newPattern.data = data;

                            bool checkPattern = CheckIfPatternOptionFlows(data);

                            if (!checkPattern)
                            {// This pattern in the original file has some flow issues, ignore it...
                                continue;
                            }

                            PatternOption? existingPattern = CheckIfPatternOptionExists(data);

                            if (existingPattern == null)
                            {
                                newPattern.previousRedNotes = new List<PatternInputNote>();
                                newPattern.previousBlueNotes = new List<PatternInputNote>();

                                if (previousRed != null)
                                {
                                    PatternInputNote newInput = CreatePatternInputNoteFromNote(previousRed);

                                    if (!CheckIfPatternInputNoteExists(newPattern.previousRedNotes, newInput))
                                    {
                                        newPattern.previousRedNotes.Add(newInput);
                                    }
                                }

                                if (previousBlue != null)
                                {
                                    PatternInputNote newInput = CreatePatternInputNoteFromNote(previousBlue);

                                    if (!CheckIfPatternInputNoteExists(newPattern.previousBlueNotes, newInput))
                                    {
                                        newPattern.previousBlueNotes.Add(newInput);
                                    }
                                }

                                patternOptions.Add(newPattern);
                            }
                            else
                            {// Still add the previous red and blue notes to the previous lists...
                                if (previousRed != null)
                                {
                                    PatternInputNote newInput = CreatePatternInputNoteFromNote(previousRed);

                                    if (!CheckIfPatternInputNoteExists(existingPattern.previousRedNotes, newInput))
                                    {
                                        existingPattern.previousRedNotes.Add(newInput);
                                    }
                                }

                                if (previousBlue != null)
                                {
                                    PatternInputNote newInput = CreatePatternInputNoteFromNote(previousBlue);

                                    if (!CheckIfPatternInputNoteExists(existingPattern.previousBlueNotes, newInput))
                                    {
                                        existingPattern.previousBlueNotes.Add(newInput);
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
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
        }

        public void CompleteParternsInfo(int numInputFiles)
        {
            int[] numOptions = new int[16];

            for (int i = 0; i < 16; i++)
            {
                numOptions[i] = 0;
            }

            foreach (var cd in patternOptions)
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
                            int origPatternLength = j;

                            for (int co = 0; co < patternOptions.Count; co++)
                            {
                                PatternOption cd = patternOptions[co];

                                if (cd.data.Count == origPatternLength)
                                {
                                    //MessageBox.Show("Found same length for " + i + " from " + co + " (length " + cd.data.Count + ")");

                                    /*if (CheckIfPatternOptionExists(cd.data, i) == null)
                                    {
                                        continue;
                                    }*/

                                    //MessageBox.Show("Copying option for length " + i + " from " + co + " (length " + cd.data.Count + ")");

                                    PatternOption newPattern = new PatternOption();
                                    List<PatternData> data = new List<PatternData>();

                                    for (int c = 0; c < i; c++)
                                    {
                                        PatternData old = cd.data[c];

                                        PatternData item = new PatternData();
                                        item.line = old.line;
                                        item.layer = old.layer;
                                        item.color = old.color;
                                        item.direction = old.direction;
                                        item.angle = old.angle;

                                        data.Add(item);
                                    }

                                    newPattern.data = data;

                                    // Copy the previous note options lists here as well...
                                    newPattern.previousRedNotes = cd.previousRedNotes;
                                    newPattern.previousBlueNotes = cd.previousBlueNotes;

                                    if (CheckIfPatternOptionExists(data) == null)
                                    {
                                        patternOptions.Add(newPattern);
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
            foreach (PatternOption option1 in patternOptions)
            {
                foreach (PatternOption option2 in patternOptions)
                {
                    if (option1 == option2)
                    {
                        continue;
                    }

                    //if (option1.data[0].Equals(option2.data[0]))
                    if (option1.data[0].line == option2.data[0].line
                        && option1.data[0].layer == option2.data[0].layer
                        //&& option1.data[0].color == option2.data[0].color
                        && option1.data[0].direction == option2.data[0].direction
                        && option1.data[0].angle == option2.data[0].angle)
                    {// These start in the same place... Copy over known start positions to option1...
                        foreach (PatternInputNote previous in option2.previousRedNotes)
                        {
                            if (!CheckIfPatternInputNoteExists(option1.previousRedNotes, previous))
                            {
                                option1.previousRedNotes.Add(previous);
                            }

                            // Testing... Adding reds & blues to both options, should be fine?!?!?!?
                            if (!CheckIfPatternInputNoteExists(option1.previousBlueNotes, previous))
                            {
                                option1.previousBlueNotes.Add(previous);
                            }
                        }

                        foreach (PatternInputNote previous in option2.previousBlueNotes)
                        {
                            if (!CheckIfPatternInputNoteExists(option1.previousBlueNotes, previous))
                            {
                                option1.previousBlueNotes.Add(previous);
                            }

                            // Testing... Adding reds & blues to both options, should be fine?!?!?!?
                            if (!CheckIfPatternInputNoteExists(option1.previousRedNotes, previous))
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

            foreach (var cd in patternOptions)
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

            maxAvailablePatternLength = highest;

            // For debugging...
            
            /*
            // Final counts, for debug display...
            string patternsCountInfo = "";

            for (int i = 1; i < 16; i++)
            {
                patternsCountInfo += i.ToString() + " - has " + numOptions[i].ToString() + " possibilities.\n";
            }

            MessageBox.Show("Calculated " + patternOptions.Count + " pattern options from " + numInputFiles + " example difficulty dat files.\n\n" + patternsCountInfo);
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

        bool IsValidDirectionForSwing(int direction, bool leftSwinging, bool rightSwinging, bool downSwinging, bool upSwinging, int previousDirection)
        {
            if (direction == CutDirection.ANY)
            {
                return true;
            }

            if (direction == previousDirection)
            {
                return false;
            }

            if ((leftSwinging || upSwinging)
                && (direction == CutDirection.UP_LEFT || direction == CutDirection.UP || direction == CutDirection.LEFT))
            {
                return true;
            }

            if ((rightSwinging || upSwinging)
                && (direction == CutDirection.UP_RIGHT || direction == CutDirection.UP || direction == CutDirection.RIGHT))
            {
                return true;
            }

            if ((leftSwinging || downSwinging)
                && (direction == CutDirection.DOWN_LEFT || direction == CutDirection.DOWN || direction == CutDirection.LEFT))
            {
                return true;
            }

            if ((rightSwinging || downSwinging)
                && (direction == CutDirection.DOWN_RIGHT || direction == CutDirection.DOWN || direction == CutDirection.RIGHT))
            {
                return true;
            }

            if (upSwinging
                && (direction == CutDirection.UP))
            {
                return true;
            }

            if (downSwinging
                && (direction == CutDirection.DOWN))
            {
                return true;
            }

            if (leftSwinging
                && (direction == CutDirection.LEFT))
            {
                return true;
            }

            if (rightSwinging
                && (direction == CutDirection.RIGHT))
            {
                return true;
            }
            
            return false;
        }

        int IsNoteFlowValid(ColorNote note, ColorNote? redPreviousNote, ColorNote? bluePreviousNote)
        {
            if (note.color == ColorType.RED)
            {// Check directionality...
                if (redPreviousNote == null)
                {// Transitions are checked elsewhere...
                    return 1;
                }

                bool leftSwinging = note.line < redPreviousNote.line;
                bool rightSwinging = note.line > redPreviousNote.line;
                bool downSwinging = note.layer < redPreviousNote.layer;
                bool upSwinging = note.layer > redPreviousNote.layer;
                int direction = note.direction;

                bool redGood = IsValidDirectionForSwing(direction, leftSwinging, rightSwinging, downSwinging, upSwinging, redPreviousNote.direction);
                return (redGood) ? 1 : 0;
            }

            if (note.color == ColorType.BLUE)
            {// Check directionality...
                if (bluePreviousNote == null)
                {// Transitions are checked elsewhere...
                    return 1;
                }

                bool leftSwinging = note.line < bluePreviousNote.line;
                bool rightSwinging = note.line > bluePreviousNote.line;
                bool downSwinging = note.layer < bluePreviousNote.layer;
                bool upSwinging = note.layer > bluePreviousNote.layer;
                int direction = note.direction;

                bool blueGood = IsValidDirectionForSwing(direction, leftSwinging, rightSwinging, downSwinging, upSwinging, bluePreviousNote.direction);
                return (blueGood) ? 1 : 0;
            }

            return 0;
        }

        int IsValidStartFrom(PatternOption option, ColorNote redPreviousNote, ColorNote bluePreviousNote)
        {
            bool redGood = false;
            bool blueGood = false;

            PatternData? red = null;
            PatternData? blue = null;

            // Find the first of each color...
            foreach (PatternData cd in option.data)
            {
                if (red == null && cd.color == ColorType.RED)
                {
                    red = cd;

                    if ((red.line == redPreviousNote.line && red.layer == redPreviousNote.layer)
                        || (red.line == bluePreviousNote.line && red.layer == bluePreviousNote.layer))
                    {// Don't use when the line and layer of a start note is on the same layer and line of the previous notes...
                        return -1;
                    }

                    if (red.direction == redPreviousNote.direction)
                    {// Don't use if the direction is the same as the last direction...
                        return -1;
                    }
                }

                if (blue == null && cd.color == ColorType.BLUE)
                {
                    blue = cd;

                    if ((blue.line == redPreviousNote.line && blue.layer == redPreviousNote.layer)
                        || (blue.line == bluePreviousNote.line && blue.layer == bluePreviousNote.layer))
                    {// Don't use when the line and layer of a start note is on the same layer and line of the previous notes...
                        return -1;
                    }

                    if (blue.direction == bluePreviousNote.direction)
                    {// Don't use if the direction is the same as the last direction...
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
                bool leftSwinging = red.line < redPreviousNote.line;
                bool rightSwinging = red.line > redPreviousNote.line;
                bool downSwinging = red.layer < redPreviousNote.layer;
                bool upSwinging = red.layer > redPreviousNote.layer;
                int direction = red.direction;

                redGood = IsValidDirectionForSwing(direction, leftSwinging, rightSwinging, downSwinging, upSwinging, redPreviousNote.direction);
            }

            if (blue != null)
            {// Check directionality...
                bool leftSwinging = blue.line < bluePreviousNote.line;
                bool rightSwinging = blue.line > bluePreviousNote.line;
                bool downSwinging = blue.layer < bluePreviousNote.layer;
                bool upSwinging = blue.layer > bluePreviousNote.layer;
                int direction = blue.direction;

                blueGood = IsValidDirectionForSwing(direction, leftSwinging, rightSwinging, downSwinging, upSwinging, bluePreviousNote.direction);
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

        int NoteMatchesPatternStart(PatternOption option, ColorNote redPreviousNote, ColorNote bluePreviousNote)
        {
            bool redExists = false;
            bool redSimilar = false;
            bool blueExists = false;
            bool blueSimilar = false;

            /*int validStart = IsValidStartFrom(option, redPreviousNote, bluePreviousNote);

            if (validStart == -1)
            {// Don't use this, starts on the same layer and line as a last note...
                return 0;
            }*/

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
                /*if (validStart == 1)
                {// Looks ok procedurally...
                    //return 4;
                    return 3;
                }
                */
                if (!redExists)
                {// Find similar...
                    foreach (var reds in option.previousRedNotes)
                    {
                        if (IntDiff(redPreviousNote.line, reds.line) <= 1
                            && IntDiff(redPreviousNote.layer, reds.layer) <= 1
                            //&& redPreviousNote.color == reds.color
                            //&& redPreviousNote.direction == reds.direction
                            //&& IsSimilarDirection(redPreviousNote.direction, reds.direction)
                            && (redPreviousNote.color == reds.color || redPreviousNote.direction == reds.direction)
                            //&& redPreviousNote.angle == reds.angle
                            )
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
                            //&& bluePreviousNote.direction == blues.direction
                            //&& IsSimilarDirection(bluePreviousNote.direction, blues.direction)
                            && (bluePreviousNote.color == blues.color || bluePreviousNote.direction == blues.direction)
                            //&& bluePreviousNote.angle == blues.angle
                            )
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
#if _DEBUGGING_
                    string redDebug = "Wanted Red:\nli: " + redPreviousNote.line + " la: " + redPreviousNote.layer + " d: " + redPreviousNote.direction + "\nKnown Red:\n";
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

                    MessageBox.Show(redDebug + blueDebug);
#endif //_DEBUGGING_

                    return 2;
                }
                else if (redSimilar || blueSimilar)
                {
                    return 1;
                }
            }

            return 0;
        }

        public PatternOption? SelectPatternOfLength(int length, ColorNote? redPrev = null, ColorNote? bluePrev = null)
        {
            try
            {
                int weightUsed = 4;

                ColorNote? redPreviousNote = redPrev;
                ColorNote? bluePreviousNote = bluePrev;

                if (redPreviousNote != null && bluePreviousNote != null)
                {// Check the times, if one is ages ago, null it...
                    bool first = (redPreviousNote.beat <= bluePreviousNote.beat) ? false : true;

                    if (first)
                    {
                        if (bluePreviousNote.beat - redPreviousNote.beat >= 10.0f)
                        {// Player has probably reseted this hand...
                            redPreviousNote = null;
                        }
                    }
                    else
                    {
                        if (redPreviousNote.beat - bluePreviousNote.beat >= 10.0f)
                        {// Player has probably reseted this hand...
                            bluePreviousNote = null;
                        }
                    }
                }

                List<PatternOption> options = new List<PatternOption>();
                List<PatternOption> secondaryOptions = new List<PatternOption>();
                List<PatternOption> tertiaryOptions = new List<PatternOption>();
                List<PatternOption> fortiaryOptions = new List<PatternOption>();

                if (redPreviousNote != null && bluePreviousNote != null)
                {// If previous notes are specified, then try to find one with a matching start setup...
                    foreach (PatternOption chain in patternOptions)
                    {
                        if (chain.data.Count >= length || chain.data.Count == maxAvailablePatternLength)
                        {
                            int chainStartWeight = NoteMatchesPatternStart(chain, redPreviousNote, bluePreviousNote);

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

                            foreach (PatternOption chain in patternOptions)
                            {
                                if (chain.data.Count == testLength)
                                {
                                    int chainStartWeight = NoteMatchesPatternStart(chain, redPreviousNote, bluePreviousNote);

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

                                foreach (PatternOption chain in patternOptions)
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

        public int GetMaxAvailablePatternLength()
        {
            return maxAvailablePatternLength;
        }
    }
}
