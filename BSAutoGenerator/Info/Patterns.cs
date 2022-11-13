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

namespace BSAutoGenerator.Info.Patterns
{
    internal class Patterns
    {
        float MAX_PATTERN_LENGTH = 16;
        //float MAX_PATTERN_LENGTH = 8;

        static Random rnd = new Random();

        public int maxAvailablePatternLength = 0;

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
            public int pattern_length;
            public List<PatternData> data = new List<PatternData>();
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
        }

        public void LoadExampleData(DifficultyData dd)
        {
            try
            {
                List<ColorNote> notes = dd.colorNotes;

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

                        newPattern.data = data;

                        //newPattern.pattern_length = patternCounter;
                        newPattern.pattern_length = data.Count;

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

                            //newPattern.pattern_length = patternCounter;
                            newPattern.pattern_length = data.Count;

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
                numOptions[cd.pattern_length]++;
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

                                if (cd.pattern_length == origPatternLength)
                                {
                                    //MessageBox.Show("Found same length for " + i + " from " + co + " (length " + cd.pattern_length + ")");

                                    /*if (CheckIfPatternOptionExists(cd.data, i) == null)
                                    {
                                        continue;
                                    }*/

                                    //MessageBox.Show("Copying option for length " + i + " from " + co + " (length " + cd.pattern_length + ")");

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

                                    newPattern.pattern_length = data.Count;

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
                numOptions[cd.pattern_length]++;
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
            return (diff > 0) ? diff : diff;
        }

        int NoteMatchesPatternStart(PatternOption option, ColorNote redPreviousNote, ColorNote bluePreviousNote)
        {
            bool redExists = false;
            bool redSimilar = false;
            bool blueExists = false;
            bool blueSimilar = false;

            foreach (var reds in option.previousRedNotes)
            {
                if (redPreviousNote.line == reds.line
                    && redPreviousNote.layer == reds.layer
                    //&& redPreviousNote.color == reds.color
                    && redPreviousNote.direction == reds.direction
                    && redPreviousNote.angle == reds.angle)
                {
                    redExists = true;
                    break;
                }
            }

            foreach (var blues in option.previousBlueNotes)
            {
                if (bluePreviousNote.line == blues.line
                    && bluePreviousNote.layer == blues.layer
                    //&& bluePreviousNote.color == blues.color
                    && bluePreviousNote.direction == blues.direction
                    && bluePreviousNote.angle == blues.angle)
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
                            && IsSimilarDirection(redPreviousNote.direction, reds.direction)
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
                            && IsSimilarDirection(bluePreviousNote.direction, blues.direction)
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

        public PatternOption? SelectPatternOfLength(int length, ColorNote? redPreviousNote = null, ColorNote? bluePreviousNote = null)
        {
            try
            {
                List<PatternOption> options = new List<PatternOption>();
                List<PatternOption> secondaryOptions = new List<PatternOption>();
                List<PatternOption> tertiaryOptions = new List<PatternOption>();
                List<PatternOption> fortiaryOptions = new List<PatternOption>();

                if (redPreviousNote != null && bluePreviousNote != null)
                {// If previous notes are specified, then try to find one with a matching start setup...
                    foreach (PatternOption chain in patternOptions)
                    {
                        if (chain.data.Count == length)
                        /*int diff = chain.data.Count - length;
                        if (chain.data.Count == length
                            || (diff >= 0 && diff <= 2 && length >= 4)
                            || (diff >= 0 && diff <= 3 && length >= 6)
                            || (diff >= 0 && diff <= 4 && length >= 8)
                            || (diff >= 0 && diff <= 5 && length >= 10)
                            || (diff >= 0 && diff <= 6 && length >= 12))*/
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
                }

                if (options.Count <= 0)
                {// Got nuffin, cap!
                    if (secondaryOptions.Count > 0)
                    {// We at least found a good secondary option or two, use those instead...
                        options = secondaryOptions;
                        //MessageBox.Show("using " + options.Count + " secondary options.");
                    }
                    else
                    {
                        if (tertiaryOptions.Count > 0)
                        {// We at least found a good secondary option or two, use those instead...
                            options = tertiaryOptions;
                            //MessageBox.Show("using " + options.Count + " tertiary options.");
                        }
                        else
                        {
                            if (fortiaryOptions.Count > 0)
                            {// We at least found a good secondary option or two, use those instead...
                                options = fortiaryOptions;
                                //MessageBox.Show("using " + options.Count + " fortiary options.");
                            }
                            else
                            {
                                // Fallback, select anything valid...
                                //MessageBox.Show("found no good options, trying any option.");

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
