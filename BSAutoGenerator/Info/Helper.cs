#define _FLOW_PLACEMENT_

using BSAutoGenerator.Data.Structure;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using static BSAutoGenerator.Info.Enumerator;
using static BSAutoGenerator.Info.Utils;

namespace BSAutoGenerator.Info
{
    internal static class Helper
    {
        static bool double_chain_vertical = false;
        static int double_chain_count = 0;

        static public (ColorNote, ColorNote) FixDoublePlacement(ColorNote red, ColorNote blue, ColorNote previous_note, ColorNote previous_blue, ColorNote previous_red)
        {
#if _FLOW_PLACEMENT_
            if (double_chain_count > 2 + RandNumber(0, 3))
            {// We need a double chain switch...
                double_chain_vertical = !double_chain_vertical;
                double_chain_count = 0;
            }

            double_chain_count++;

            if (double_chain_vertical)
            {// Make vertical stack...
                if (previous_note.color == ColorType.BLUE)
                {// Copy red from blue...
                    red.line = blue.line;
                    red.angle = blue.angle = 0;
                    red.layer = layerForVerticalDouble(blue.layer, blue.color);

                    red.direction = blue.direction = directionFromPreviousNote(previous_blue, blue.line, blue.layer, false);
                    //red.direction = directionFromPreviousNote(previous_red, red.line, red.layer, false);
                }
                else
                {// Copy blue from red...
                    blue.line = red.line;
                    blue.angle = red.angle = 0;
                    blue.layer = layerForVerticalDouble(red.layer, red.color);

                    red.direction = blue.direction = directionFromPreviousNote(previous_red, red.line, red.layer, false);
                    //blue.direction = directionFromPreviousNote(previous_blue, blue.line, blue.layer, false);
                }
            }
            else
            {// Make horizontal stack...
                if (previous_note.color == ColorType.BLUE)
                {// Copy red from blue...
                    red.layer = blue.layer;
                    red.angle = blue.angle = 0;
                    red.line = lineForHorizontalDouble(blue.line, blue.color);

                    if (red.line > blue.line)
                    {// Flip them, don't do inverted hands on horizontal duo's...
                        int tmp = red.line;
                        red.line = blue.line;
                        blue.line = tmp;
                    }

                    red.direction = blue.direction = directionFromPreviousNote(previous_blue, blue.line, blue.layer, true);
                    //red.direction = directionFromPreviousNote(previous_red, red.line, red.layer, true);
                }
                else
                {// Copy blue from red...
                    blue.layer = red.layer;
                    blue.angle = red.angle = 0;
                    blue.line = lineForHorizontalDouble(red.line, red.color);

                    if (red.line > blue.line)
                    {// Flip them, don't do inverted hands on horizontal duo's...
                        int tmp = red.line;
                        red.line = blue.line;
                        blue.line = tmp;
                    }

                    red.direction = blue.direction = directionFromPreviousNote(previous_red, red.line, red.layer, true);
                    //red.direction = directionFromPreviousNote(previous_red, red.line, red.layer, true);
                }

                if ((blue.layer == Layer.TOP || blue.layer == Layer.BOTTOM) && RandNumber(0, 4) >= 3)
                {// Sometimes spread these ones out, for effect...
                    blue.line = Line.RIGHT;
                    red.line = Line.LEFT;
                }
                /*else if (blue.line == Line.RIGHT && red.line == Line.MIDDLE_RIGHT && RandNumber(0, 4) >= 2)
                {// Sometimes make these diagonal, for effect...
                    if (blue.layer == Layer.TOP)
                    {
                        red.layer = Layer.MIDDLE;
                        red.direction = blue.direction = flowDirectionFromPreviousNote(previous_note, blue.line, blue.layer);
                        red.angle = blue.angle = blue.direction;
                    }
                    else if (blue.layer == Layer.BOTTOM)
                    {
                        blue.layer = Layer.MIDDLE;
                        red.direction = blue.direction = flowDirectionFromPreviousNote(previous_note, blue.line, blue.layer);
                        red.angle = blue.angle = blue.direction;
                    }
                }
                else if (blue.line == Line.MIDDLE_LEFT && red.line == Line.LEFT && RandNumber(0, 4) >= 2)
                {// Sometimes make these diagonal, for effect...
                    if (blue.layer == Layer.TOP)
                    {
                        blue.layer = Layer.MIDDLE;
                        red.direction = blue.direction = flowDirectionFromPreviousNote(previous_note, blue.line, blue.layer);
                        red.angle = blue.angle = blue.direction;
                    }
                    else if (blue.layer == Layer.BOTTOM)
                    {
                        blue.layer = Layer.MIDDLE;
                        red.direction = blue.direction = flowDirectionFromPreviousNote(previous_note, blue.line, blue.layer);
                        red.angle = blue.angle = blue.direction;
                    }
                }*/
            }

#else
            // Both on same layer
            if (red.layer == blue.layer && red.line != blue.line)
            {
                // Change the layer of one of the note
                if(SwingType.Horizontal.Contains(red.direction))
                {
                    if(red.layer == Layer.BOTTOM)
                    {
                        if(red.line == Line.LEFT)
                        {
                            red.layer++;
                        }
                        else if(red.line == Line.MIDDLE_LEFT || red.line == Line.MIDDLE_RIGHT)
                        {
                            red.layer = Layer.TOP;
                        }
                        else
                        {
                            if(SwingType.Up.Contains(blue.direction))
                            {
                                blue.layer = Layer.TOP;
                            }
                            else
                            {
                                 red.layer++;
                            }
                        }
                    }
                    else if(red.layer == Layer.TOP)
                    {
                        if (red.line == Line.LEFT)
                        {
                            red.layer--;
                        }
                        else if (red.line == Line.MIDDLE_LEFT || red.line == Line.MIDDLE_RIGHT)
                        {
                            red.layer = Layer.BOTTOM;
                        }
                        else
                        {
                            red.layer--;
                        }
                    }
                }
                else if(SwingType.Horizontal.Contains(blue.direction))
                {
                    if (blue.layer == Layer.BOTTOM)
                    {
                        if (blue.line == Line.RIGHT)
                        {
                            blue.layer++;
                        }
                        else if (blue.line == Line.MIDDLE_LEFT || blue.line == Line.MIDDLE_RIGHT)
                        {
                            blue.layer = Layer.TOP;
                        }
                        else
                        {
                            if (SwingType.Up.Contains(red.direction))
                            {
                                red.layer = Layer.TOP;
                            }
                            else
                            {
                                blue.layer++;
                            }
                        }
                    }
                    else if (blue.layer == Layer.TOP)
                    {
                        if (blue.line == Line.RIGHT)
                        {
                            blue.layer--;
                        }
                        else if (blue.line == Line.MIDDLE_LEFT || blue.line == Line.MIDDLE_RIGHT)
                        {
                            blue.layer = Layer.BOTTOM;
                        }
                        else if (blue.layer == Layer.TOP)
                        {
                            if (blue.line == Line.LEFT)
                            {
                                blue.layer--;
                            }
                            else if (blue.line == Line.MIDDLE_LEFT || blue.line == Line.MIDDLE_RIGHT)
                            {
                                blue.layer = Layer.BOTTOM;
                            }
                            else
                            {
                                blue.layer--;
                            }
                        }
                    }
                }
            }
            // Both on the same line
            else if (red.line == blue.line && red.layer != blue.layer)
            {
                if(SwingType.Vertical.Contains(red.direction))
                {
                    // Change the line of one of the notes
                    if (red.line > 1)
                    {
                        if (red.layer != Layer.MIDDLE)
                        {
                            if (red.line != Line.LEFT)
                            {
                                red.line--;
                            }
                            else
                            {
                                red.line++;
                            }
                        }
                        else if (blue.layer == 0)
                        {
                            red.layer = 2;
                            if (red.line != Line.LEFT)
                            {
                                red.line--;
                            }
                            else
                            {
                                red.line++;
                            }
                        }
                        else if (blue.layer == 2)
                        {
                            red.layer = 0;
                            if (red.line != Line.LEFT)
                            {
                                red.line--;
                            }
                            else
                            {
                                red.line++;
                            }
                        }
                    }
                    else
                    {
                        if (blue.line != Line.RIGHT)
                        {
                            blue.line++;
                        }
                        else if(red.line != Line.LEFT)
                        {
                            red.line--;
                        }
                    }
                }
                else if(SwingType.Vertical.Contains(blue.direction))
                {
                    if (blue.line < 2)
                    {
                        if (blue.layer != Layer.MIDDLE)
                        {
                            if (blue.line != Line.RIGHT)
                            {
                                blue.line++;
                            }
                            else
                            {
                                blue.line--;
                            }
                        }
                        else if (red.layer == 0)
                        {
                            blue.layer = 2;
                            if (blue.line != Line.RIGHT)
                            {
                                blue.line++;
                            }
                            else
                            {
                                blue.line--;
                            }
                        }
                        else if (red.layer == 2)
                        {
                            blue.layer = 0;
                            if (blue.line != Line.RIGHT)
                            {
                                blue.line++;
                            }
                            else
                            {
                                blue.line--;
                            }
                        }
                    }
                    else
                    {
                        if (red.line != Line.LEFT)
                        {
                            red.line--;
                        }
                        else if(blue.line != Line.RIGHT)
                        {
                            blue.line++;
                        }
                    }
                }
            } 
            
            // Diagonal
            if(SwingType.Diagonal.Contains(red.direction) || SwingType.Diagonal.Contains(blue.direction))
            {
                if (red.line == blue.line - 1 && red.layer == blue.layer - 1)
                {
                    if (blue.layer != 2)
                    {
                        blue.layer++;
                    }
                    else
                    {
                        red.layer--;
                    }
                }
                else if (red.line == blue.line - 1 && red.layer == blue.layer + 1)
                {
                    if (blue.layer != 0)
                    {
                        blue.layer--;
                    }
                    else
                    {
                        red.layer++;
                    }
                }
                else if (red.line == blue.line + 1 && red.layer == blue.layer + 1)
                {
                    if (blue.layer != 2)
                    {
                        (red.line, blue.line) = (blue.line, red.line);
                        (red.layer, blue.layer) = (blue.layer, red.layer);
                        blue.layer++;
                    }
                    else
                    {
                        (red.line, blue.line) = (blue.line, red.line);
                        (red.layer, blue.layer) = (blue.layer, red.layer);
                        red.layer--;
                    }
                }
                else if (red.line == blue.line + 1 && red.layer == blue.layer - 1)
                {
                    if (blue.layer != 0)
                    {
                        (red.line, blue.line) = (blue.line, red.line);
                        (red.layer, blue.layer) = (blue.layer, red.layer);
                        blue.layer--;
                    }
                    else
                    {
                        (red.line, blue.line) = (blue.line, red.line);
                        (red.layer, blue.layer) = (blue.layer, red.layer);
                        red.layer++;
                    }
                }
            }
#endif

            return (red, blue);
        }

#if _FLOW_PLACEMENT_
        static public int layerForVerticalDouble(int otherLayer, int otherColor)
        {
            if (otherLayer == Layer.BOTTOM)
            {// Bottom-most layer, always use MIDDLE for secondary...
                return Layer.MIDDLE;
            }
            else if (otherLayer == Layer.TOP)
            {// Upper-most layer, always use MIDDLE for secondary...
                return Layer.MIDDLE;
            }
            
            // We can choose...
            if (otherColor == 1)
            {
                return Layer.TOP;
            }
            
            return Layer.BOTTOM;
        }

        static public int lineForHorizontalDouble(int otherLine, int otherColor)
        {
            if (otherLine == Line.LEFT)
            {// Left-most line, always use MIDDLE_LEFT for secondary...
                return Line.MIDDLE_LEFT;
            }
            else if (otherLine == Line.RIGHT)
            {// Right-most line, always use MIDDLE_RIGHT for secondary...
                return Line.MIDDLE_RIGHT;
            }
            else if (otherLine == Line.MIDDLE_LEFT)
            {// Left-most line, always use RIGHT for secondary...
                return Line.LEFT;
            }
            else //if (otherLine == Line.MIDDLE_RIGHT)
            {// Right-most line, always use LEFT for secondary...
                return Line.RIGHT;
            }
        }

        static public int directionFromPreviousNote(ColorNote previous_note, int this_line, int this_layer, bool horizontal)
        {
            int best = CutDirection.ANY;

            int hand_layer = previous_note.layer;
            int hand_line = previous_note.line;

            switch (previous_note.direction)
            {
                case CutDirection.RIGHT:
                    hand_line++;
                    break;
                case CutDirection.LEFT:
                    hand_line--;
                    break;
                case CutDirection.UP_RIGHT:
                    hand_line++;
                    hand_layer++;
                    break;
                case CutDirection.UP_LEFT:
                    hand_line--;
                    hand_layer++;
                    break;
                case CutDirection.DOWN_RIGHT:
                    hand_line++;
                    hand_layer--;
                    break;
                case CutDirection.DOWN_LEFT:
                    hand_line--;
                    hand_layer--;
                    break;
                case CutDirection.UP:
                    hand_layer++;
                    break;
                case CutDirection.DOWN:
                    hand_layer--;
                    break;
                case CutDirection.ANY:
                    /*if (this_line < hand_line)
                    {
                        hand_line--;
                    }
                    else if (this_line > hand_line)
                    {
                        hand_line++;
                    }

                    if (this_layer < hand_layer)
                    {
                        hand_layer--;
                    }
                    else if (this_layer > hand_layer)
                    {
                        hand_layer++;
                    }*/
                    break;
            }

            if (horizontal)
            {
                if (this_layer < hand_layer)
                {// primary, try using layer...
                    best = CutDirection.DOWN;
                }
                else if (this_layer > hand_layer)
                {// primary, try using layer...
                    best = CutDirection.UP;
                }
                /*else if (this_line < hand_line)
                {// secondary, try using line...
                    best = CutDirection.LEFT;
                }
                else if (this_line > hand_line)
                {// secondary, try using line...
                    best = CutDirection.RIGHT;
                }*/
                else
                {// anything goes!
                    best = CutDirection.ANY;
                }
            }
            else
            {
                if (this_line < hand_line)
                {// primary, try using line...
                    best = CutDirection.LEFT;
                }
                else if (this_line > hand_line)
                {// primary, try using line...
                    best = CutDirection.RIGHT;
                }
                /*else if (this_layer < hand_layer)
                {// secondary, try using layer...
                    best = CutDirection.DOWN;
                }
                else if (this_layer > hand_layer)
                {// secondary, try using layer...
                    best = CutDirection.UP;
                }*/
                else
                {// anything goes!
                    best = CutDirection.ANY;
                }
            }

            return best;
        }

        static public bool verticalOrHorizontalFromPreviousDirection(int previous_direction)
        {
            bool best = false;

            switch (previous_direction)
            {
                case CutDirection.RIGHT:
                    best = true;
                    break;
                case CutDirection.LEFT:
                    best = true;
                    break;
                case CutDirection.UP_RIGHT:
                    best = (RandNumber(0, 2) == 1) ? false : true;
                    break;
                case CutDirection.UP_LEFT:
                    best = (RandNumber(0, 2) == 1) ? false : true;
                    break;
                case CutDirection.DOWN_RIGHT:
                    best = (RandNumber(0, 2) == 1) ? false : true;
                    break;
                case CutDirection.DOWN_LEFT:
                    best = (RandNumber(0, 2) == 1) ? false : true;
                    break;
                case CutDirection.UP:
                    best = false;
                    break;
                case CutDirection.DOWN:
                    best = false;
                    break;
            }

            return best;
        }
#endif

        /// <summary>
        /// Verify if the note placement match the situation and return the value
        /// </summary>
        /// <param name="lastLine">Last note line</param>
        /// <param name="lastLayer">Last note layer</param>
        /// <param name="type">Note color</param>
        /// <returns>Line, Layer</returns>
        static public (int, int) PlacementCheck(int direction, int type, ColorNote lastNote)
        {
            // Next possible line and layer
            int line = -1;
            int layer = -1;
            int rand;

            do
            {
                if (type == ColorType.RED)
                {
                    //System.Windows.MessageBox.Show("RED: " + direction + " / " + PossibleRedPlacement.placement.Count());

                    rand = RandNumber(0, PossibleRedPlacement.placement[direction].Count());
                    line = PossibleRedPlacement.placement[direction][rand][0];
                    layer = PossibleRedPlacement.placement[direction][rand][1];
                }
                else if(type == ColorType.BLUE)
                {
                    //System.Windows.MessageBox.Show("BLUE: " + direction + " / " + PossibleBluePlacement.placement.Count());

                    rand = RandNumber(0, PossibleBluePlacement.placement[direction].Count());
                    line = PossibleBluePlacement.placement[direction][rand][0];
                    layer = PossibleBluePlacement.placement[direction][rand][1];
                }

                // Fix possible fused notes
                if (lastNote.line == line && lastNote.layer == layer)
                {
                    continue;
                }

                return (line, layer);
            } while (true);
        }

        static public int flowDirectionFromPreviousNote(ColorNote previous_note, int this_line, int this_layer)
        {
            int best = CutDirection.ANY;

            int hand_layer = previous_note.layer;
            int hand_line = previous_note.line;

            switch (previous_note.direction)
            {
                case CutDirection.RIGHT:
                    hand_line++;
                    break;
                case CutDirection.LEFT:
                    hand_line--;
                    break;
                case CutDirection.UP_RIGHT:
                    hand_line++;
                    hand_layer++;
                    break;
                case CutDirection.UP_LEFT:
                    hand_line--;
                    hand_layer++;
                    break;
                case CutDirection.DOWN_RIGHT:
                    hand_line++;
                    hand_layer--;
                    break;
                case CutDirection.DOWN_LEFT:
                    hand_line--;
                    hand_layer--;
                    break;
                case CutDirection.UP:
                    hand_layer++;
                    break;
                case CutDirection.DOWN:
                    hand_layer--;
                    break;
                case CutDirection.ANY:
                    /*if (this_line < hand_line)
                    {
                        hand_line--;
                    }
                    else if (this_line > hand_line)
                    {
                        hand_line++;
                    }

                    if (this_layer < hand_layer)
                    {
                        hand_layer--;
                    }
                    else if (this_layer > hand_layer)
                    {
                        hand_layer++;
                    }*/
                    break;
            }

            if (this_line < hand_line)
            {// primary, try using line...
                if (this_layer < hand_layer)
                {
                    best = CutDirection.DOWN_LEFT;
                }
                else if (this_layer > hand_layer)
                {
                    best = CutDirection.UP_LEFT;
                }
                else
                {
                    best = CutDirection.LEFT;
                }
            }
            else if (this_line > hand_line)
            {// primary, try using line...
                if (this_layer < hand_layer)
                {
                    best = CutDirection.DOWN_RIGHT;
                }
                else if (this_layer > hand_layer)
                {
                    best = CutDirection.UP_RIGHT;
                }
                else
                {
                    best = CutDirection.RIGHT;
                }
            }
            else if (this_layer < hand_layer)
            {// secondary, try using layer...
                if (this_line < hand_line)
                {
                    best = CutDirection.DOWN_LEFT;
                }
                else if (this_line > hand_line)
                {
                    best = CutDirection.DOWN_RIGHT;
                }
                else
                {
                    best = CutDirection.DOWN;
                }
            }
            else if (this_layer > hand_layer)
            {// secondary, try using layer...
                if (this_line < hand_line)
                {
                    best = CutDirection.UP_LEFT;
                }
                else if (this_line > hand_line)
                {
                    best = CutDirection.UP_RIGHT;
                }
                else
                {
                    best = CutDirection.UP;
                }
            }
            else
            {// anything goes!
                best = CutDirection.ANY;
            }


            return best;
        }

        /// <summary>
        /// Verify if the next direction selected match the situation (flow free) and return the value
        /// </summary>
        /// <param name="last">Last direction</param>
        /// <param name="swing">Up or down swing (wrist)</param>
        /// <param name="hand">Current hand</param>
        /// <param name="speed"></param>
        /// <returns>Direction</returns>
        static public int NextDirection(int last, int swing, int hand, float speed, bool limiter)
        {
            // Store all the possible next cut direction, we will use some logic to find if the next direction match
            int[] possibleNext = {0, 0};
            // Type of flow
            int flow = 0;
            // Next direction
            int next;

            // Get the direction based on speed
            if(hand == 0)
            {
                if (speed < 0.5) // Under half a beat
                {
                    possibleNext = PossibleFlow.normalRed[last];
                    flow = 2;
                }
                else if (speed >= 0.5 && speed < 1) // Half to under a beat
                {
                    possibleNext = PossibleFlow.techRed[last];
                    flow = 1;
                }
                else // Anything above a beat is pretty slow usually
                {
                    possibleNext = PossibleFlow.extremeRed[last];
                    flow = 0;
                }
            }
            else if (hand == 1)
            {
                if (speed < 0.5) // Under half a beat
                {
                    possibleNext = PossibleFlow.normalBlue[last];
                    flow = 2;
                }
                else if (speed >= 0.5 && speed < 1) // Half to under a beat
                {
                    possibleNext = PossibleFlow.techBlue[last];
                    flow = 1;
                }
                else // Anything above a beat is pretty slow usually
                {
                    possibleNext = PossibleFlow.extremeBlue[last];
                    flow = 0;
                }
            }


            do
            {
                next = Utils.RandNumber(0, 8);

                if(hand == 0)
                {
                    if (PossibleFlow.extremeRed[last].Contains(next) && !PossibleFlow.techRed[last].Contains(next)) // Extreme roll again
                    {
                        next = Utils.RandNumber(0, 8);
                    }
                    if ((PossibleFlow.extremeRed[last].Contains(next) || PossibleFlow.techRed[last].Contains(next)) && !PossibleFlow.normalRed[last].Contains(next)) // Extreme and tech roll again
                    {
                        next = Utils.RandNumber(0, 8);
                    }
                }
                else if (hand == 1)
                {
                    if (PossibleFlow.extremeBlue[last].Contains(next) && !PossibleFlow.techBlue[last].Contains(next)) // Extreme roll again
                    {
                        next = Utils.RandNumber(0, 8);
                    }
                    if ((PossibleFlow.extremeBlue[last].Contains(next) || PossibleFlow.techBlue[last].Contains(next)) && !PossibleFlow.normalBlue[last].Contains(next)) // Extreme and tech roll again
                    {
                        next = Utils.RandNumber(0, 8);
                    }
                }




                // We check if the possible next direction match with the last one before any logic.
                if (possibleNext.Contains(next))
                {
                    // Each hand and type of swing have to be treated differently
                    if (hand == 0) // Red
                    {
                        if (swing == 0) // Up Swing
                        {
                            if(limiter && (next == CutDirection.LEFT || next == CutDirection.UP_RIGHT || next == CutDirection.UP)) // Too far
                            {
                                continue;
                            }

                            if(last == CutDirection.LEFT)
                            {
                                if(next == CutDirection.DOWN && flow != 0) // Meh
                                {
                                    continue; 
                                }
                            }

                            if (last == CutDirection.DOWN || last == CutDirection.DOWN_LEFT) // Down, maximum range of the wrist from the left (extreme)
                            {
                                if (next == CutDirection.LEFT || next == CutDirection.UP_LEFT) // Impossible
                                {
                                    continue;
                                }
                            }
                            else if (last == CutDirection.RIGHT || last == CutDirection.UP_RIGHT) // Right, maximum range of the wrist from the right (extreme)
                            {
                                if (next == CutDirection.UP || next == CutDirection.UP_LEFT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.UP)
                            {
                                if ((next == CutDirection.RIGHT) && flow != 0) // Not extreme
                                {
                                    continue;
                                }
                                if (next == CutDirection.DOWN_LEFT && flow != 0) // Meh
                                {
                                    continue;
                                }
                            }
                        }
                        else if (swing == 1) // Down Swing
                        {
                            if (limiter && (next == CutDirection.RIGHT || next == CutDirection.UP_RIGHT || next == CutDirection.DOWN || next == CutDirection.DOWN_LEFT)) // Too far
                            {
                                continue;
                            }

                            if(last == CutDirection.RIGHT) //Meh
                            {
                                if(next == CutDirection.UP && flow != 0)
                                {
                                    continue;
                                }
                            }

                            if (last == CutDirection.UP || last == CutDirection.UP_RIGHT) // Up, maximum range of the wrist from the left (extreme)
                            {
                                if (next == CutDirection.RIGHT || next == CutDirection.DOWN_RIGHT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.LEFT || last == CutDirection.DOWN_LEFT) // Left, maximum range of the wrist from the right (extreme)
                            {
                                if (next == CutDirection.DOWN || next == CutDirection.DOWN_RIGHT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.DOWN)
                            {
                                if ((next == CutDirection.RIGHT || next == CutDirection.UP_RIGHT) && flow != 0) // Not extreme
                                {
                                    continue;
                                }
                                if(next == CutDirection.LEFT && flow != 0) // Meh
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    else if (hand == 1) // Blue
                    {
                        if (swing == 0) // Up Swing
                        {
                            if (limiter && (next == CutDirection.RIGHT || next == CutDirection.UP || next == CutDirection.UP_LEFT)) // Too far
                            {
                                continue;
                            }

                            if (last == CutDirection.RIGHT)
                            {
                                if (next == CutDirection.DOWN && flow != 0) // Meh
                                {
                                    continue;
                                }
                            }

                            if (last == CutDirection.DOWN || last == CutDirection.DOWN_RIGHT) // Down, maximum range of the wrist from the right (extreme)
                            {
                                if (next == CutDirection.RIGHT || next == CutDirection.UP_RIGHT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.LEFT || last == CutDirection.UP_LEFT) // Left, maximum range of the wrist from the left (extreme)
                            {
                                if (next == CutDirection.UP || next == CutDirection.UP_RIGHT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.UP)
                            {
                                if (next == CutDirection.LEFT && flow != 0) // Not extreme
                                {
                                    continue;
                                }
                                if(next == CutDirection.DOWN_RIGHT && flow != 0) // Meh
                                {
                                    continue;
                                }
                            }
                        }
                        else if (swing == 1) // Down Swing
                        {
                            if (limiter && (next == CutDirection.LEFT || next == CutDirection.UP_LEFT || next == CutDirection.DOWN || next == CutDirection.DOWN_RIGHT)) // Too far
                            {
                                continue;
                            }

                            if (last == CutDirection.LEFT) //Meh
                            {
                                if (next == CutDirection.UP && flow != 0)
                                {
                                    continue;
                                }
                            }

                            if (last == CutDirection.UP || last == CutDirection.UP_LEFT) // Up, maximum range of the wrist from the right (extreme)
                            {
                                if (next == CutDirection.LEFT || next == CutDirection.DOWN_LEFT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.RIGHT || last == CutDirection.DOWN_RIGHT) // Right, maximum range of the wrist from the left (extreme)
                            {
                                if (next == CutDirection.DOWN || next == CutDirection.DOWN_LEFT) // Impossible
                                {
                                    continue;
                                }
                            }
                            if (last == CutDirection.DOWN)
                            {
                                if ((next == CutDirection.LEFT || next == CutDirection.UP_LEFT) && flow != 0) // Not extreme
                                {
                                    continue;
                                }
                                if (next == CutDirection.RIGHT && flow != 0) // Meh
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    return next;
                }
            } while (true);
        }

        /// <summary>
        /// Method to find Arc between two notes based on their cut direction
        /// </summary>
        /// <param name="notes">List of ColorNote</param>
        /// <returns>List of SliderData</returns>
        static public List<SliderData> FindArc(List<ColorNote> notes)
        {
            List<SliderData> sliderData = new();

            for (int i = 0; i < notes.Count - 1; i++)
            {
                if (notes[i + 1].direction == CutDirection.DOWN_LEFT || notes[i + 1].direction == CutDirection.UP_LEFT || notes[i + 1].direction == CutDirection.LEFT)
                {
                    
                    sliderData.Add(new(notes[i], notes[i + 1], 0.8f, 0.8f, SliderMidAnchorMode.CLOCKWISE));
                }
                else if (notes[i + 1].direction == CutDirection.DOWN_RIGHT || notes[i + 1].direction == CutDirection.UP_RIGHT || notes[i + 1].direction == CutDirection.RIGHT)
                {
                    sliderData.Add(new(notes[i], notes[i + 1], 0.8f, 0.8f, SliderMidAnchorMode.COUNTER_CLOCKWISE));
                }
                else
                {
                    sliderData.Add(new(notes[i], notes[i + 1], 0.8f, 0.8f, SliderMidAnchorMode.STRAIGHT));
                }
            }

            return sliderData;
        }

        /// <summary>
        /// Method to find specific pattern and remove the notes of the pattern from the main list of note
        /// </summary>
        /// <param name="notes">List of ColorNote</param>
        /// <returns>List of list of ColorNote (Pattern) and modified List of ColorNote</returns>
        static public (List<List<ColorNote>>, List<ColorNote>) FindPattern(List<ColorNote> notes)
        {
            // List of list to keep thing like sliders/stack/window/tower etc
            List<List<ColorNote>> patterns = new();

            // Stock pattern notes
            List<ColorNote> pattern = new();

            // To know if a pattern was found
            bool found = false;

            // Find all notes sliders/stack/window/tower
            for (int i = 0; i < notes.Count; i++)
            {
                if (i == notes.Count - 1)
                {
                    if (found)
                    {
                        pattern.Add(new(notes[i]));
                        notes.RemoveAt(i);
                        patterns.Add(new(pattern));
                        found = false;
                    }
                    break;
                }

                ColorNote now = notes[i];
                ColorNote next = notes[i + 1];

                if (next.beat - now.beat >= 0 && next.beat - now.beat < 0.1)
                {
                    if (!found)
                    {
                        pattern = new();
                        found = true;
                    }
                    pattern.Add(new(notes[i]));
                    notes.RemoveAt(i);
                    i--;
                }
                else
                {
                    if (found)
                    {
                        pattern.Add(new(notes[i]));
                        notes.RemoveAt(i);
                        i--;
                        patterns.Add(new(pattern));
                    }

                    found = false;
                }
            }

            return (patterns, notes);
        }

        /// <summary>
        /// Method to find the possible Head of a chain
        /// </summary>
        /// <param name="note">Current note</param>
        /// <param name="next">Next note</param>
        /// <param name="direction">Cut direction</param>
        /// <returns>True = Current note win over the next one as head</returns>
        internal static bool PossibleHead(ColorNote note, ColorNote next, int direction)
        {
            if (note.direction == CutDirection.ANY && next.direction == CutDirection.ANY && direction != 8)
            {
                if (next.line > note.line && (direction == 3 || direction == 5 || direction == 7))
                {
                    return true;
                }
                if (next.line < note.line && (direction == 2 || direction == 4 || direction == 6))
                {
                    return true;
                }
                if (next.layer > note.layer && (direction == 0 || direction == 4 || direction == 5))
                {
                    return true;
                }
                if (next.layer < note.layer && (direction == 1 || direction == 6 || direction == 7))
                {
                    return true;
                }
            }
            else if (note.direction == CutDirection.DOWN || note.direction == CutDirection.DOWN_LEFT || note.direction == CutDirection.DOWN_RIGHT)
            {
                if (next.direction == CutDirection.ANY || next.layer < note.layer)
                {
                    return true;
                }
            }
            if (note.direction == CutDirection.UP || note.direction == CutDirection.UP_LEFT || note.direction == CutDirection.UP_RIGHT)
            {
                if (next.direction == CutDirection.ANY || next.layer > note.layer)
                {
                    return true;
                }
            }
            if (note.direction == CutDirection.LEFT || note.direction == CutDirection.UP_LEFT || note.direction == CutDirection.DOWN_LEFT)
            {
                if (next.direction == CutDirection.ANY || next.line < note.line)
                {
                    return true;
                }
            }
            if (note.direction == CutDirection.RIGHT || note.direction == CutDirection.UP_RIGHT || note.direction == CutDirection.DOWN_RIGHT)
            {
                if (next.direction == CutDirection.ANY || next.line > note.line)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
