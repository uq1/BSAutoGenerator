//#define _FULL_INVERSION_ // I only want to invert lines, drections, and colors instead.

using BSAutoGenerator.Data.Structure;
using BSAutoGenerator.Data.V2;
using System.Collections.Generic;
using static BSAutoGenerator.Info.Enumerator;

namespace BSAutoGenerator.Algorithm
{
    class Invert
    {
        static public (List<ColorNote>, List<Obstacle>) MakeInvert(List<ColorNote> noteTemp, List<Obstacle> obstacles, double Limiter, bool IsLimited)
        {
#if _FULL_INVERSION_
            // Current note
            ColorNote n;

            // Found something that we don't want to invert
            bool found;

            for (int i = noteTemp.Count - 1; i > -1; i--) // Reverse-order
            {
                n = noteTemp[i];

                found = false;

                foreach (ColorNote temp in noteTemp)
                {
                    if (n.beat == temp.beat && n.color == temp.color && n.direction == temp.direction && !IsLimited)
                    {
                        //Loloppe notes
                        break;
                    }
                    if (((n.beat - temp.beat < Limiter && n.beat - temp.beat > 0) || (temp.beat - n.beat < Limiter && temp.beat - n.beat > 0)) && temp.color == n.color)
                    {
                        found = true;
                        break;
                    }
                    else if (temp.beat == n.beat && temp.color == n.color && n != temp)
                    {
                        found = true;
                        break;
                    }
                    else if (temp.beat == n.beat && temp.color != n.color && n != temp && (temp.line == n.line || temp.layer == n.layer))
                    {
                        found = true;
                        break;
                    }
                }

                if (found) //If found, then skip
                {
                    continue;
                }

                switch (n.direction) //Based on the cut direction, change the layer of the note.
                {
                    case CutDirection.UP:
                        n.layer = Layer.BOTTOM;
                        break;
                    case CutDirection.DOWN:
                        n.layer = Layer.TOP;
                        break;
                    case CutDirection.LEFT:
                        n.line = Line.RIGHT;
                        break;
                    case CutDirection.RIGHT:
                        n.line = Line.LEFT;
                        break;
                    case CutDirection.UP_LEFT:
                        n.layer = Layer.BOTTOM;
                        break;
                    case CutDirection.UP_RIGHT:
                        n.layer = Layer.BOTTOM;
                        break;
                    case CutDirection.DOWN_LEFT:
                        n.layer = Layer.TOP;
                        break;
                    case CutDirection.DOWN_RIGHT:
                        n.layer = Layer.TOP;
                        break;
                    case CutDirection.ANY:
                        break;
                }
            }
            return (noteTemp, obstacles);

#else //!_FULL_INVERSION_
            foreach (ColorNote note in noteTemp)
            {
                if (note.color == ColorType.RED)
                {
                    note.color = ColorType.BLUE;
                }
                else if (note.color == ColorType.BLUE)
                {
                    note.color = ColorType.RED;
                }

                note.line = 4 - note.line;
                note.direction = GetInvertedDirection(note.direction);
            }

            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle.height == 5)
                {// Wall...
                    obstacle.index = 4 - obstacle.index;
                }
                else
                {// Roof, leave as is...

                }
            }

            return (noteTemp, obstacles);
#endif //_FULL_INVERSION_
        }

        static public int GetInvertedDirection(int direction)
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
                    //return CutDirection.DOWN_RIGHT;
                    return CutDirection.UP_RIGHT;
                case CutDirection.UP_RIGHT:
                    //return CutDirection.DOWN_LEFT;
                    return CutDirection.UP_LEFT;
                case CutDirection.DOWN_LEFT:
                    //return CutDirection.UP_RIGHT;
                    return CutDirection.DOWN_RIGHT;
                case CutDirection.DOWN_RIGHT:
                    //return CutDirection.UP_LEFT;
                    return CutDirection.DOWN_LEFT;
            }

            return CutDirection.ANY;
        }
    }
}
