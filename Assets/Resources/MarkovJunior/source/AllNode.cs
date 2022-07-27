﻿// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MarkovJunior
{

    /// <summary>
    /// An 'all' node applies its rewrite rules to all possible non-overlapping
    /// matches on each execution step. If there are any overlapping matches, then
    /// a maximal non-overlapping set is chosen at random.
    /// </summary>
    class AllNode : RuleNode
    {
        override protected bool Load(XElement xelem, bool[] parentSymmetry, Grid grid)
        {
            if (!base.Load(xelem, parentSymmetry, grid)) return false;
            matches = new List<(int, int, int, int)>();
            matchMask = AH.Array2D(rules.Length, grid.state.Length, false);
            return true;
        }

        /// <summary>
        /// Applies the rule <c>r</c> at position (x, y, z) in the grid, if it does
        /// not overlap with another rewrite which was made on the same step.
        /// </summary>
        /// <param name="r">The index of the rule in the <see cref="RuleNode.rules">rules</see> array.</param>
        /// <param name="x">The x coordinate to apply the rule at.</param>
        /// <param name="y">The y coordinate to apply the rule at.</param>
        /// <param name="z">The z coordinate to apply the rule at.</param>
        /// <param name="newstate">A mask of which cells have been changed so far.</param>
        /// <param name="MX"><inheritdoc cref="Grid.MX" path="/summary"/></param>
        /// <param name="MY"><inheritdoc cref="Grid.MY" path="/summary"/></param>
        void Fit(int r, int x, int y, int z, bool[] newstate, int MX, int MY)
        {
            Rule rule = rules[r];
            // check if the match overlaps with a previous one
            for (int dz = 0; dz < rule.OMZ; dz++) for (int dy = 0; dy < rule.OMY; dy++) for (int dx = 0; dx < rule.OMX; dx++)
                    {
                        byte value = rule.output[dx + dy * rule.OMX + dz * rule.OMX * rule.OMY];
                        if (value != 0xff && newstate[x + dx + (y + dy) * MX + (z + dz) * MX * MY]) return;
                    }
            // apply the rewrite
            last[r] = true;
            for (int dz = 0; dz < rule.OMZ; dz++) for (int dy = 0; dy < rule.OMY; dy++) for (int dx = 0; dx < rule.OMX; dx++)
                    {
                        byte newvalue = rule.output[dx + dy * rule.OMX + dz * rule.OMX * rule.OMY];
                        if (newvalue != 0xff)
                        {
                            int sx = x + dx, sy = y + dy, sz = z + dz;
                            int i = sx + sy * MX + sz * MX * MY;
                            newstate[i] = true;
                            grid.state[i] = newvalue;
                            ip.changes.Add((sx, sy, sz));
                        }
                    }
        }

        override public bool Go()
        {
            if (!base.Go()) return false;
            lastMatchedTurn = ip.counter;

            if (trajectory != null)
            {
                if (counter >= trajectory.Length) return false;
                Array.Copy(trajectory[counter], grid.state, grid.state.Length);
                counter++;
                return true;
            }

            if (matchCount == 0) return false;

            int MX = grid.MX, MY = grid.MY;
            if (potentials != null)
            {
                double firstHeuristic = 0;
                bool firstHeuristicComputed = false;

                List<(int, double)> list = new();
                for (int m = 0; m < matchCount; m++)
                {
                    var (r, x, y, z) = matches[m];
                    double? heuristic = Field.DeltaPointwise(grid.state, rules[r], x, y, z, fields, potentials, grid.MX, grid.MY);
                    if (heuristic != null)
                    {
                        double h = (double)heuristic;
                        if (!firstHeuristicComputed)
                        {
                            firstHeuristic = h;
                            firstHeuristicComputed = true;
                        }
                        double u = ip.random.NextDouble();
                        list.Add((m, temperature > 0 ? Math.Pow(u, Math.Exp((h - firstHeuristic) / temperature)) : -h + 0.001 * u));
                    }
                }
                (int, double)[] ordered = list.OrderBy(pair => -pair.Item2).ToArray();
                for (int k = 0; k < ordered.Length; k++)
                {
                    var (r, x, y, z) = matches[ordered[k].Item1];
                    matchMask[r][x + y * MX + z * MX * MY] = false;
                    Fit(r, x, y, z, grid.mask, MX, MY);
                }
            }
            else
            {
                // matches may overlap, so apply in a random order
                int[] shuffle = new int[matchCount];
                shuffle.Shuffle(ip.random);
                for (int k = 0; k < shuffle.Length; k++)
                {
                    var (r, x, y, z) = matches[shuffle[k]];
                    matchMask[r][x + y * MX + z * MX * MY] = false;
                    Fit(r, x, y, z, grid.mask, MX, MY);
                }
            }

            // reset the grid.mask buffer
            for (int n = ip.first[lastMatchedTurn]; n < ip.changes.Count; n++)
            {
                var (x, y, z) = ip.changes[n];
                grid.mask[x + y * MX + z * MX * MY] = false;
            }
            counter++;
            matchCount = 0;
            return true;
        }
    }
}