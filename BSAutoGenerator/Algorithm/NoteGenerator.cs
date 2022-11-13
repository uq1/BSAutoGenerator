﻿//#define _SCRIPTED_FLOW_

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

namespace BSAutoGenerator.Algorithm
{
    internal class NoteGenerator
    {
        static Patterns scripted_patterns = new Patterns();
        static Chains scripted_chains = new Chains();

        /// <summary>
        /// Method to generate new ColorNote and BurstSliderData from timings (in beat).
        /// This will create a new Beat Saber map from scratch (minus the timings).
        /// </summary>
        /// <param name="timings">Time (in beat) to generate the note</param>
        /// <param name="bpm">Main BPM of the song</param>
        /// <param name="limiter">Allow backhanded when off</param>
        /// <returns>Notes and Chains (for now)</returns>
        static public (List<ColorNote>, List<BurstSliderData>) AutoMapper(List<float> timings, float bpm, bool limiter)
        {
            // Our main list where we will store the generated Notes and Chains.
            List<ColorNote> notes = new();
            List<BurstSliderData> chains = new();

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
                    /*for (int j = i; j < i + scripted_patterns.GetMaxAvailablePatternLength() && j < notes.Count - 1; j += 2)
                    {
                        if (notes[j].beat != notes[j + 1].beat && patternCounter < scripted_patterns.GetMaxAvailablePatternLength())
                        {// Continue chain count...
                            skipTo = j;
                            patternCounter = (j - i);
                        }
                        else
                        {// Chain ends here...
                            break;
                        }
                    }*/
                    for (int j = 0; j < scripted_patterns.GetMaxAvailablePatternLength(); j += 2)
                    {
                        if (j + i + 1 >= notes.Count) break;

                        if (notes[i + j].beat != notes[i + j + 1].beat && patternCounter < scripted_patterns.GetMaxAvailablePatternLength())
                        {// Continue chain count...
                            skipTo = i + j + 1;// - 1;
                            patternCounter = j;//(j - (i - 1)) / 2;
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
                            }
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

                            for (int j = i - 1; j <= skipTo && j < notes.Count; j += 2)
                            {
                                //MessageBox.Show("j= " + j + " (" + (j + 1) + ")  upto= " + upto + " chainCount=" + chain.data.Count + " skipTo = " + skipTo + " notesCount= " + notes.Count);

                                if (upto >= chain.data.Count)
                                {
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

            // We're done
            return (notes, chains);
        }
    }
}
