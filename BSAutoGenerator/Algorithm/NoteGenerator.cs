//#define _SCRIPTED_FLOW_
//#define _DEBUG_PATTERN_USAGE_
//#define _SCRIPTED_OBSTACLES_
#define _PROCEDURAL_OBSTACLES_

using BSAutoGenerator.Data.Structure;
using BSAutoGenerator.Data.V2;
using BSAutoGenerator.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static BSAutoGenerator.Info.Enumerator;
using static BSAutoGenerator.Info.Helper;
using BSAutoGenerator.Info.Chains;
using static BSAutoGenerator.Info.Chains.Chains;
using static System.Windows.Forms.Design.AxImporter;
using System.Reflection;
using System.Windows;
using BSAutoGenerator.Info.Patterns;
using static BSAutoGenerator.Info.Patterns.Patterns;
using System.CodeDom;

namespace BSAutoGenerator.Algorithm
{
    internal class NoteGenerator
    {
        static Patterns scripted_patterns = new Patterns();
        static Chains scripted_chains = new Chains();

        static Random rnd = new Random();

        static int IntDiff(int a, int b)
        {
            int diff = a - b;
            return (diff >= 0) ? diff : -diff;
        }

        /// <summary>
        /// Method to generate new ColorNote and BurstSliderData from timings (in beat).
        /// This will create a new Beat Saber map from scratch (minus the timings).
        /// </summary>
        /// <param name="timings">Time (in beat) to generate the note</param>
        /// <param name="bpm">Main BPM of the song</param>
        /// <param name="limiter">Allow backhanded when off</param>
        /// <returns>Notes and Chains (for now)</returns>
        static public (List<ColorNote>, List<BurstSliderData>, List<Obstacle>) AutoMapper(List<float> timings, float bpm, bool limiter)
        {
            // Our main list where we will store the generated Notes and Chains.
            List<ColorNote> notes = new();
            List<BurstSliderData> chains = new();
            List<Obstacle> obstacles = new();

            ColorNote previous_blue = null;
            ColorNote previous_red = null;

            // Selected line and layer
            int line = 0;
            int layer = 0;

#if _ORIG_FLOW_
            // Keep the player wrist rotation via direction.
            // LEFT SIDE
            // Upper limit (tech): 0 (Up), lower limit (tech): 2 (Left)
            // Upper limit: 3 (Right), lower limit: 6 (Down-Left)
            // RIGHT SIDE (vertical mirror)
            // Upper limit (tech): 0 (Up), lower limit (tech): 3 (Right)
            // Upper limit: 2 (Left), lower limit: 7 (Down-Right)
            int leftDirection = 1;
            int rightDirection = 1;
            // The last swing. Upswing = 0, Downswing = 1
            int leftSwing = 1;
            int rightSwing = 1;

            // To know which hand (color). 0 is red, 1 is blue
            int hand = 1;

            // The current direction being selected for the next note.
            int direction = -1;

            // The expected speed, used to choose between tech or normal type of flow (in beat). 1+ beat = extreme, 0.5 - 1 beat = tech, 0.5- = normal.
            // Based 
            float speed;
            float lastLeft = 0;
            float lastRight = 0;

            // Select all directions
            foreach (float timing in timings)
            {
                // For simplicity sake, the first two notes will start with specific value.
                // First note will be a blue down and second note will be a red down, in the bottom middle.
                if(notes.Count == 0)
                {
                    ColorNote n = new(timing, 1, 2, 0, 1);
                    notes.Add(n);
                    lastRight = timing;

                    previous_blue = n;
                    continue;
                }
                else if(notes.Count == 1)
                {
                    ColorNote n = new(timing, 0, 1, 0, 1);
                    notes.Add(n);
                    lastLeft = timing;

                    previous_red = n;
                    continue;
                }

                // Direction are separated for each hand and each timing in step of 2.
                if (hand == 0) // Red
                {
                    // Get the current expected speed
                    speed = timing - lastLeft;
                    // If the BPM is above 250, we want to start restricting the speed
                    if(bpm >= 250)
                    {
                        speed = 250 / bpm * speed;
                    }

                    direction = NextDirection(leftDirection, leftSwing, hand, speed, limiter);

                    // We track the data for the next note
                    if (leftSwing == 0)
                    {
                        leftSwing = 1;
                    }
                    else if(leftSwing == 1)
                    {
                        leftSwing = 0;
                    }
                    leftDirection = direction;
                    lastLeft = timing;
                }
                else if(hand == 1) // Blue
                {
                    // Get the current expected speed
                    speed = timing - lastRight;
                    // If the BPM is above 250, we want to start restricting the speed
                    if (bpm >= 250)
                    {
                        speed = 250 / bpm * speed;
                    }

                    direction = NextDirection(rightDirection, rightSwing, hand, speed, limiter);

                    // We track the data for the next note
                    if (rightSwing == 0)
                    {
                        rightSwing = 1;
                    }
                    else if (rightSwing == 1)
                    {
                        rightSwing = 0;
                    }
                    rightDirection = direction;
                    lastRight = timing;
                }

                // Create the note and add it to the list
                if(hand == 1)
                {
                    ColorNote note = new(timing, hand, 2, 0, direction);
                    notes.Add(note);
                    hand = 0; // Switch hand for the next note
                }
                else
                {
                    ColorNote note = new(timing, hand, 1, 0, direction);
                    notes.Add(note);
                    hand = 1; // Switch hand for the next note
                }
            }
#else //!_ORIG_FLOW_
            if (scripted_patterns.patternOptions == null || scripted_patterns.patternOptions.Count <= 0)
            {
                scripted_patterns.InitPatterns();
            }

            if (scripted_chains.chainOptions == null || scripted_chains.chainOptions.Count <= 0)
            {
                scripted_chains.LoadExampleData(scripted_patterns);
            }

            // Create fake notes for blue and red to begin with...
            previous_red = new(timings[1], ColorType.RED, Line.MIDDLE_LEFT, Layer.BOTTOM, CutDirection.DOWN);
            previous_blue = new(timings[0], ColorType.BLUE, Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN);

            notes.Add(previous_blue);
            notes.Add(previous_red);

            int currentColor = 0;

            bool skip = false;

            for (int t = 2; t < timings.Count; t++)
            {
                try
                {

                    if (skip)
                    {
                        skip = false;
                        continue;
                    }

                    float timing = timings[t];
                    float nextTiming = 0.0f;

                    if (t + 1 < timings.Count)
                    {
                        nextTiming = timings[t + 1];
                    }

                    if (nextTiming > 0.0f && (nextTiming - timing >= -0.02 && nextTiming - timing <= 0.02))
                    {// Doing a double box...
                     // blue
                        ColorNote noteL = new(timing, ColorType.RED, Line.MIDDLE_LEFT, Layer.BOTTOM, CutDirection.DOWN); // line, layer, direction gets replaced later...
                        notes.Add(noteL);

                        // red
                        ColorNote noteR = new(nextTiming, ColorType.BLUE, Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN); // line, layer, direction gets replaced later...
                        notes.Add(noteR);

                        skip = true; // already added the next note...
                    }
                    else
                    {// Single box...
                        if (Utils.RandNumber(0, 5) >= 3)
                        {// Switch colors...
                            if (currentColor == ColorType.BLUE)
                            {
                                currentColor = ColorType.RED;
                            }
                            else
                            {
                                currentColor = ColorType.BLUE;
                            }
                        }

                        ColorNote note = new(timing, currentColor, (currentColor == ColorType.RED) ? Line.MIDDLE_LEFT : Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN); // line, layer, direction gets replaced later...
                        notes.Add(note);
                    }
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show(e.ToString());
                }
            }
#endif //_ORIG_FLOW_

#if __MY_OLD_FLOW__
            try
            {
                // Select all lines and layers (should probably be done together)
                for (int i = 2; i < notes.Count; i++)
                {
                    /*System.Windows.MessageBox.Show("previous_blue: C=" + previous_blue.color + " LI=" + previous_blue.line + " LA=" + previous_blue.layer 
                        + ". previous_red: C=" + previous_red.color + " LI=" + previous_red.line + " LA=" + previous_red.layer);*/

                    //System.Windows.MessageBox.Show("note: " + i + " / " + notes.Count);

                    if (notes[i].color == ColorType.RED)
                    {
                        (line, layer) = PlacementCheck(notes[i].direction, notes[i].color, previous_red/*notes[i - 1]*/);

                        // UQ1: Don't spam on the same line twice in a row...
                        if (line == previous_red.line) line = lineForHorizontalDouble(previous_red.layer, previous_red.color);

                        // UQ1: Don't spawn colors way on the other side, it breaks flow...
                        if (line > Line.MIDDLE_RIGHT) line = Line.MIDDLE_RIGHT;

                        // UQ1: Don't spam on the same layer twice in a row...
                        if (layer == previous_red.layer) layer = layerForVerticalDouble(previous_red.layer, previous_red.color);

                        // UQ1: Yeah, and completely replace the old directions, I want actual flow...
                        notes[i].direction = flowDirectionFromPreviousNote(previous_red, line, layer);
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        (line, layer) = PlacementCheck(notes[i].direction, notes[i].color, previous_blue/*notes[i - 1]*/);

                        // UQ1: Don't spam on the same line twice in a row...
                        if (line == previous_blue.line) line = lineForHorizontalDouble(previous_blue.layer, previous_blue.color);

                        // UQ1: Don't spawn colors way on the other side, it breaks flow...
                        if (line < Line.MIDDLE_LEFT) line = Line.MIDDLE_LEFT;

                        // UQ1: Don't spam on the same layer twice in a row...
                        if (layer == previous_blue.layer) layer = layerForVerticalDouble(previous_blue.layer, previous_blue.color);

                        // UQ1: Yeah, and completely replace the old directions, I want actual flow...
                        notes[i].direction = flowDirectionFromPreviousNote(previous_blue, line, layer);
                    }

                    notes[i].line = line;
                    notes[i].layer = layer;

                    if (notes[i].beat - notes[i - 1].beat >= -0.02 && notes[i].beat - notes[i - 1].beat <= 0.02)
                    {
                        if (notes[i].color == ColorType.RED)
                        {
                            (notes[i], notes[i - 1]) = FixDoublePlacement(notes[i], notes[i - 1], previous_red, previous_blue, previous_red);
                        }
                        else if (notes[i].color == ColorType.BLUE)
                        {
                            (notes[i - 1], notes[i]) = FixDoublePlacement(notes[i - 1], notes[i], previous_blue, previous_blue, previous_red);
                        }
                    }

                    if (notes[i].color == ColorType.RED)
                    {
                        previous_red = notes[i];
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        previous_blue = notes[i];
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
#elif _SCRIPTED_FLOW_
            Chains scripted_chains = new Chains();
            scripted_chains.LoadExampleData();

            try
            {
                // Select all lines and layers (should probably be done together)
                for (int i = 2; i < notes.Count; i++)
                {

                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
#else //!__MY_OLD_FLOW__
            try
            {
                previous_red = new(timings[1], ColorType.RED, Line.MIDDLE_LEFT, Layer.BOTTOM, CutDirection.DOWN);
                previous_blue = new(timings[0], ColorType.BLUE, Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN);

                // Select all lines and layers (should probably be done together)
                for (int i = 2; i < notes.Count; i++)
                {
                    /*System.Windows.MessageBox.Show("previous_blue: C=" + previous_blue.color + " LI=" + previous_blue.line + " LA=" + previous_blue.layer 
                        + ". previous_red: C=" + previous_red.color + " LI=" + previous_red.line + " LA=" + previous_red.layer);*/

                    //System.Windows.MessageBox.Show("note: " + i + " / " + notes.Count);

                    if (notes[i].color == ColorType.RED)
                    {
                        (line, layer) = PlacementCheck(notes[i].direction, notes[i].color, previous_red/*notes[i - 1]*/);

                        // UQ1: Don't spam on the same line twice in a row...
                        if (line == previous_red.line) line = lineForHorizontalDouble(previous_red.layer, previous_red.color);

                        // UQ1: Don't spawn colors way on the other side, it breaks flow...
                        if (line > Line.MIDDLE_RIGHT) line = Line.MIDDLE_RIGHT;

                        // UQ1: Don't spam on the same layer twice in a row...
                        if (layer == previous_red.layer) layer = layerForVerticalDouble(previous_red.layer, previous_red.color);

                        // UQ1: Yeah, and completely replace the old directions, I want actual flow...
                        notes[i].direction = flowDirectionFromPreviousNote(previous_red, line, layer);
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        (line, layer) = PlacementCheck(notes[i].direction, notes[i].color, previous_blue/*notes[i - 1]*/);

                        // UQ1: Don't spam on the same line twice in a row...
                        if (line == previous_blue.line) line = lineForHorizontalDouble(previous_blue.layer, previous_blue.color);

                        // UQ1: Don't spawn colors way on the other side, it breaks flow...
                        if (line < Line.MIDDLE_LEFT) line = Line.MIDDLE_LEFT;

                        // UQ1: Don't spam on the same layer twice in a row...
                        if (layer == previous_blue.layer) layer = layerForVerticalDouble(previous_blue.layer, previous_blue.color);

                        // UQ1: Yeah, and completely replace the old directions, I want actual flow...
                        notes[i].direction = flowDirectionFromPreviousNote(previous_blue, line, layer);
                    }

                    notes[i].line = line;
                    notes[i].layer = layer;

                    // Merge close (doubles) notes to the same time...
                    if (notes[i].beat - notes[i - 1].beat >= -0.02 && notes[i].beat - notes[i - 1].beat <= 0.02)
                    {
                        notes[i - 1].beat = notes[i].beat;
                    }

                    if (notes[i].color == ColorType.RED)
                    {
                        previous_red = notes[i];
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        previous_blue = notes[i];
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }

            try
            {
                // Do awesomely fun non-scripted double chains...
                previous_red = new(timings[1], ColorType.RED, Line.MIDDLE_LEFT, Layer.BOTTOM, CutDirection.DOWN);
                previous_blue = new(timings[0], ColorType.BLUE, Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN);

                for (int i = 1; i < notes.Count; i++)
                {
                    if (notes[i].beat - notes[i - 1].beat == 0.0 && notes[i].beat - notes[i - 1].beat == 0.0)
                    {
                        if (notes[i].color == ColorType.RED)
                        {
                            (notes[i], notes[i - 1]) = FixDoublePlacement(notes[i], notes[i - 1], previous_red, previous_blue, previous_red);
                        }
                        else if (notes[i].color == ColorType.BLUE)
                        {
                            (notes[i - 1], notes[i]) = FixDoublePlacement(notes[i - 1], notes[i], previous_blue, previous_blue, previous_red);
                        }
                    }

                    if (notes[i].color == ColorType.RED)
                    {
                        previous_red = notes[i];
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        previous_blue = notes[i];
                    }
                }

                //
                //
                // At this point, we have a functioning fun map, with some flow... but let's improve on it, based on examples!
                //
                //

                
                //
                // Firstly, over-ride the non-scripted patterns created above with scripted favorate ones provided by the user...
                //
                int skipTo = 0;
                
                previous_red = new(timings[1], ColorType.RED, Line.MIDDLE_LEFT, Layer.BOTTOM, CutDirection.DOWN);
                previous_blue = new(timings[0], ColorType.BLUE, Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN);

                for (int i = 0; i < notes.Count - 1; i++)
                {
                    if (i <= skipTo && skipTo != 0)
                    {// Already skip to the next double in chain...
                        if (notes[i].color == ColorType.RED)
                        {
                            previous_red = notes[i];
                        }
                        else if (notes[i].color == ColorType.BLUE)
                        {
                            previous_blue = notes[i];
                        }

                        continue;
                    }

                    skipTo = 0;
                    int patternCounter = 0;

                    // Look for new chains...
                    for (int j = 0; j < scripted_patterns.GetMaxAvailablePatternLength(); j++)
                    {
                        if (j + i + 1 >= notes.Count) break;

                        if (notes[i + j].beat != notes[i + j + 1].beat && patternCounter < scripted_patterns.GetMaxAvailablePatternLength())
                        {// Continue chain count...
                            skipTo = i + j;
                            patternCounter = j;
                        }
                        else
                        {// Chain ends here...
                            break;
                        }
                    }

                    if (patternCounter > 0)
                    {// Have a chain, record it...
                        PatternOption? chain = scripted_patterns.SelectPatternOfLength(patternCounter, previous_red, previous_blue);

                        if (chain != null)
                        {// We have a player provided example chain to use, lets do that!
                            //MessageBox.Show("wanted= " + patternCounter + " returned= " + chain.data.Count);
                            List<ColorNote> added = new List<ColorNote>();

                            //for (int j = i - 1; j <= skipTo && j < notes.Count - 1; j += 2)
                            for (int j = 0; j < patternCounter; j++)
                            {
                                //MessageBox.Show("j= " + j + " (" + (j + 1) + ")  upto= " + upto + " chainCount=" + chain.data.Count + " skipTo = " + skipTo + " notesCount= " + notes.Count);

                                if (j >= chain.data.Count)
                                {
                                    skipTo = 0;
                                    patternCounter = 0;
                                    break;
                                }

                                ColorNote note1 = notes[i + j];

                                PatternData cd = chain.data[j];
                                note1.line = cd.line;
                                note1.layer = cd.layer;
                                note1.color = cd.color;
                                note1.direction = cd.direction;
                                note1.angle = cd.angle;

                                added.Add(note1);
                            }

#if _SCRIPTED_OBSTACLES_
if (added.Count > 0)        if (MainWindow.ENABLE_OBSTACLES)
                            {
                                if (chain.obstacles.Count > 0)
                                {// Add walls as well...
                                    List<Obstacle> addedObs = new();

                                    for (int j = 0; j < chain.obstacles.Count; j++)
                                    {
                                        Obstacle wall = chain.obstacles[j];

                                        float startBeat = notes[i].beat + wall.beat + 0.1f;
                                        float endBeat = 0;

                                        foreach (ColorNote n in added)
                                        {
                                            if (n.beat < startBeat)
                                            {
                                                continue;
                                            }
                                        
                                            if (wall.height == 5)
                                            {// Wall...
                                                if (IntDiff(n.line, wall.index) <= wall.width)
                                                {// Would be a note inside this wall at this point...
                                                    break;
                                                }
                                            }
                                            else
                                            {// Roof...
                                                //MessageBox.Show("ROOF: layer: " + wall.layer + " width: " + wall.width + " height: " + wall.height);

                                                if (n.layer >= wall.layer - 1)
                                                {// Would be a note inside this wall at this point...
                                                    break;
                                                }
                                            }
                                        
                                            endBeat = n.beat;
                                        }

                                        float patternDuration = endBeat - startBeat;

                                        if (patternDuration >= 0.25f)
                                        {
                                            // Check overlaps first... Since I extended them to max duration above...
                                            bool overlapped = false;

                                            foreach (Obstacle oldWall in addedObs)
                                            {
                                                if (wall.height == 5)
                                                {// Wall...
                                                    if (wall.index == oldWall.index)
                                                    {
                                                        overlapped = true;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    if (wall.layer == oldWall.layer)
                                                    {
                                                        overlapped = true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (!overlapped)
                                            {
                                                Obstacle newWall = new Obstacle(startBeat, wall.index, wall.layer, /*MathF.Min(wall.duration, */patternDuration/*)*/, wall.width, wall.height);
                                                obstacles.Add(newWall);
                                                addedObs.Add(newWall);
                                            }
                                        }
                                    }
                                }
                            }
#endif //_SCRIPTED_OBSTACLES_
                        }
                    }
                    else
                    {// A single note, or a loner double... Maybe do these into structures later as well, to also script those...
                        if (notes[i].beat != notes[i + 1].beat)
                        {// Add a lone double possibility to the list...
                            //MessageBox.Show("Double without chain.");
                            PatternOption? chain = scripted_patterns.SelectPatternOfLength(1, previous_red, previous_blue);

                            if (chain != null)
                            {// We have a player provided example chain to use, lets do that!
                                //MessageBox.Show("wanted= " + doubleChainCounter + " returned= " + chain.data.Count);

                                ColorNote note = notes[i];
                                PatternData cd = chain.data[0];

                                note.line = cd.line;
                                note.layer = cd.layer;
                                note.color = cd.color;
                                note.direction = cd.direction;
                                note.angle = cd.angle;
                                skipTo = 0;
                            }
                        }
                    }

                    if (notes[i].color == ColorType.RED)
                    {
                        previous_red = notes[i];
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        previous_blue = notes[i];
                    }
                }
                




                //
                // Now over-ride the non-scripted double chains created above with scripted favorate ones provided by the user...
                //
                skipTo = 0;

                previous_red = new(timings[1], ColorType.RED, Line.MIDDLE_LEFT, Layer.BOTTOM, CutDirection.DOWN);
                previous_blue = new(timings[0], ColorType.BLUE, Line.MIDDLE_RIGHT, Layer.BOTTOM, CutDirection.DOWN);

                for (int i = 1; i < notes.Count; i++)
                {
                    if (i <= skipTo && skipTo != 0)
                    {// Already skip to the next double in chain...
                        if (notes[i].color == ColorType.RED)
                        {
                            previous_red = notes[i];
                        }
                        else if (notes[i].color == ColorType.BLUE)
                        {
                            previous_blue = notes[i];
                        }

                        continue;
                    }

                    skipTo = 0;
                    int doubleChainCounter = 0;

                    // Look for new chains...
                    //for (int j = i - 1; j < i + scripted_chains.GetMaxAvailableChainLength() * 2 && j < notes.Count; j += 2)
                    for (int j = 0; j < scripted_chains.GetMaxAvailableChainLength() * 2; j += 2)
                    {
                        if (j + i >= notes.Count) break;

                        if (notes[j + i].beat == notes[j + i - 1].beat && doubleChainCounter < scripted_chains.GetMaxAvailableChainLength())
                        {// Continue chain count...
                            skipTo = i + j;// - 1;
                            doubleChainCounter = j / 2;//(j - (i - 1)) / 2;
                        }
                        else
                        {// Chain ends here...
                            break;
                        }
                    }

                    if (doubleChainCounter > 0)
                    {// Have a chain, record it...
                        ChainOption? chain = scripted_chains.SelectChainOfLength(doubleChainCounter, previous_red, previous_blue);

                        if (chain != null)
                        {// We have a player provided example chain to use, lets do that!
                            int upto = 0;

                            //MessageBox.Show("wanted= " + doubleChainCounter + " returned= " + chain.data.Count);

                            float startBeat = notes[i - 1].beat;
                            float endBeat = notes[skipTo - 1].beat;
                            float patternDuration = endBeat - startBeat;

                            for (int j = i - 1; j <= skipTo && j < notes.Count; j += 2)
                            {
                                //MessageBox.Show("j= " + j + " (" + (j + 1) + ")  upto= " + upto + " chainCount=" + chain.data.Count + " skipTo = " + skipTo + " notesCount= " + notes.Count);

                                if (upto >= chain.data.Count)
                                {
                                    // Store this early-end point value for walls...
                                    endBeat = notes[j].beat;
                                    patternDuration = endBeat - startBeat;

                                    skipTo = 0;
                                    doubleChainCounter = 0;
                                    break;
                                }

                                ColorNote note1 = notes[j];
                                ColorNote note2 = notes[j + 1];

                                ChainData cd = chain.data[upto];

                                note1.line = cd.line1;
                                note1.layer = cd.layer1;
                                note1.color = cd.color1;
                                note1.direction = cd.direction1;
                                note1.angle = cd.angle1;

                                note2.line = cd.line2;
                                note2.layer = cd.layer2;
                                note2.color = cd.color2;
                                note2.direction = cd.direction2;
                                note2.angle = cd.angle2;

                                upto++;
                            }

#if _SCRIPTED_OBSTACLES_
                            /*
                            if (MainWindow.ENABLE_OBSTACLES)
                            {
                                if (chain.obstacles.Count > 0)
                                {// Add walls as well...
                                    for (int j = 0; j < chain.obstacles.Count; j++)
                                    {
                                        Obstacle wall = chain.obstacles[j];
                                        Obstacle newWall = new Obstacle(startBeat + wall.beat, wall.index, wall.layer, MathF.Min(wall.duration, patternDuration), wall.width, wall.height);
                                        obstacles.Add(newWall);
                                    }
                                }
                            }*/
#endif //_SCRIPTED_OBSTACLES_
                        }
                    }
                    else
                    {// A single note, or a loner double... Maybe do these into structures later as well, to also script those...
                        if (notes[i - 1].beat == notes[i].beat)
                        {// Add a lone double possibility to the list...
                            //MessageBox.Show("Double without chain.");
                            ChainOption? chain = scripted_chains.SelectChainOfLength(1, previous_red, previous_blue);

                            if (chain != null)
                            {// We have a player provided example chain to use, lets do that!
                                int upto = 0;

                                //MessageBox.Show("wanted= " + doubleChainCounter + " returned= " + chain.data.Count);

                                for (int j = i - 1; j <= skipTo && j < notes.Count; j += 2)
                                {
                                    //MessageBox.Show("j= " + j + " (" + (j + 1) + ")  upto= " + upto + " chainCount=" + chain.data.Count + " skipTo = " + skipTo + " notesCount= " + notes.Count);

                                    ColorNote note1 = notes[j];
                                    ColorNote note2 = notes[j + 1];

                                    ChainData cd = chain.data[upto];

                                    note1.line = cd.line1;
                                    note1.layer = cd.layer1;
                                    note1.color = cd.color1;
                                    note1.direction = cd.direction1;
                                    note1.angle = cd.angle1;

                                    note2.line = cd.line2;
                                    note2.layer = cd.layer2;
                                    note2.color = cd.color2;
                                    note2.direction = cd.direction2;
                                    note2.angle = cd.angle2;

                                    upto++;
                                }
                            }
                        }
                    }

                    if (notes[i].color == ColorType.RED)
                    {
                        previous_red = notes[i];
                    }
                    else if (notes[i].color == ColorType.BLUE)
                    {
                        previous_blue = notes[i];
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
#endif //__MY_OLD_FLOW__

#if _PROCEDURAL_OBSTACLES_
            try
            {
                if (MainWindow.ENABLE_OBSTACLES)
                {
                    float OBSTACLE_MIN_FREE_LANE_TIME = 2.0f;
                    float OBSTACLE_BUFFER = 0.5f;
                    float OBSTACLE_MIN_TIME = 1.0f;

                    float[] laneLastUsed = new float[5] { 8, 8, 8, 8, 8 };

                    //foreach (ColorNote note in notes)
                    for (int i = 0; i < notes.Count; i++)
                    {// Sigh, there is something very, very, wrong with the obstacles structure code, or the game code for it, lines are all wrong, beats are off.. IDK...
                        ColorNote note = notes[i];
                        ColorNote? nextNote = (i + 1 < notes.Count) ? notes[i + 1] : null;

                        if (laneLastUsed[0] < note.beat - OBSTACLE_MIN_FREE_LANE_TIME && laneLastUsed[1] < note.beat - OBSTACLE_MIN_FREE_LANE_TIME
                            && (nextNote == null || (nextNote.line != 0 && nextNote.line != 1))
                            && rnd.Next(2) == 1)
                        {// Lane 0+1+2 available for a wall...
                            float start = MathF.Max(laneLastUsed[0], laneLastUsed[1]) + OBSTACLE_BUFFER;
                            float end = note.beat - OBSTACLE_BUFFER;
                            float duration = end - start;

                            if (duration >= OBSTACLE_MIN_TIME)
                            {
                                Obstacle newWall = new Obstacle(start, -1, 0, duration, 2, 5);
                                obstacles.Add(newWall);

                                laneLastUsed[0] = note.beat;
                                laneLastUsed[1] = note.beat;
                                laneLastUsed[2] = note.beat;
                            }
                        }
                        /*else if (laneLastUsed[0] < note.beat - OBSTACLE_MIN_FREE_LANE_TIME
                            && (nextNote == null || (nextNote.line != 0))
                            && rnd.Next(2) == 1)
                        {// Lane 0+1 available for a wall...
                            float start = laneLastUsed[0] + OBSTACLE_BUFFER;
                            float end = note.beat - OBSTACLE_BUFFER;
                            float duration = end - start;

                            if (duration >= OBSTACLE_MIN_TIME)
                            {
                                Obstacle newWall = new Obstacle(start, -1, 0, duration, 1, 5);
                                obstacles.Add(newWall);

                                laneLastUsed[0] = note.beat;
                                laneLastUsed[1] = note.beat;
                                laneLastUsed[2] = note.beat; // just to stop the player being trapped by walls on each side...
                            }
                        }
                        else*/
                        if (laneLastUsed[2] < note.beat - OBSTACLE_MIN_FREE_LANE_TIME && laneLastUsed[3] < note.beat - OBSTACLE_MIN_FREE_LANE_TIME
                     && (nextNote == null || (nextNote.line != 2 && nextNote.line != 3))
                     && rnd.Next(2) == 1)
                        {// Lane 1+2+3 available for a wall...
                            float start = MathF.Max(laneLastUsed[2], laneLastUsed[3]) + OBSTACLE_BUFFER;
                            float end = note.beat - OBSTACLE_BUFFER;
                            float duration = end - start;

                            if (duration >= OBSTACLE_MIN_TIME)
                            {
                                Obstacle newWall = new Obstacle(start, 4, 0, duration, 2, 5);
                                obstacles.Add(newWall);

                                laneLastUsed[1] = note.beat;
                                laneLastUsed[2] = note.beat;
                                laneLastUsed[3] = note.beat;
                            }
                        }
                        /*else if (laneLastUsed[3] < note.beat - OBSTACLE_MIN_FREE_LANE_TIME
                            && (nextNote == null || (nextNote.line != 3))
                            && rnd.Next(2) == 1)
                        {// Lane 2+3 available for a wall...
                            float start = laneLastUsed[3] + OBSTACLE_BUFFER;
                            float end = note.beat - OBSTACLE_BUFFER;
                            float duration = end - start;

                            if (duration >= OBSTACLE_MIN_TIME)
                            {
                                Obstacle newWall = new Obstacle(start, 4, 0, duration, 1, 5);
                                obstacles.Add(newWall);

                                laneLastUsed[1] = note.beat; // just to stop the player being trapped by walls on each side...
                                laneLastUsed[2] = note.beat;
                                laneLastUsed[3] = note.beat;
                            }
                        }*/

                        // Add this new note's time to it's lane...
                        if (note.line < 5)
                        {
                            laneLastUsed[note.line] = note.beat;
                        }
                    }

                    //MessageBox.Show("Added obstabcles: " + obstacles.Count);
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.ToString());
            }
#endif //_PROCEDURAL_OBSTACLES_

#if _DEBUG_PATTERN_USAGE_
            // For debugging...
            string patternsUsages = "";

            if (scripted_patterns.patternOptions != null)
            {
                patternsUsages += "Pattern Usages:\n";

                for (int i = 4; i >= 0; i--)
                {
                    patternsUsages += "[" + i + "] " + scripted_patterns.numWeightUsages[i] + "\n";
                }

                patternsUsages += "\n";
            }

            if (scripted_chains.chainOptions != null)
            {
                patternsUsages += "Double Pattern Usages:\n";

                for (int i = 4; i >= 0; i--)
                {
                    patternsUsages += "[" + i + "] " + scripted_chains.numWeightUsages[i] + "\n";
                }

                patternsUsages += "\n";
            }

            MessageBox.Show(patternsUsages);
#endif //_DEBUG_PATTERN_USAGE_

            // We're done
            return (notes, chains, obstacles);
        }
    }
}
