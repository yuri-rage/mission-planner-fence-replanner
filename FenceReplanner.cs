using ClipperLib;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static MAVLink;

namespace FenceReplanner
{
    public class FenceReplanner : Plugin
    {
        public override string Name => "Fence Replanner";
        public override string Version => "v0.1.2-alpha";
        public override string Author => "Yuri Rage";

        private const double CLPR_UTM_SCALE_FACTOR = 1000.0;
        private FenceReplannerUI form;
        private MyDataGridView commands;
        private Dictionary<int, mavlink_mission_item_int_t> fencepoints;

        //[DebuggerHidden]
        public override bool Init()
        {
            loopratehz = 1;
            return true;
        }

        public override bool Loaded()
        {
            var contextMenuItem = new ToolStripMenuItem("Fence Replanner")
            {
                DropDownItems =
                    {
                        new ToolStripMenuItem("Trim Polygon", null, TrimPolygonClick),
                        new ToolStripMenuItem("Replan Mission", null, ReplanMissionClick),
                        new ToolStripMenuItem("Options", null, OpenDialog)
                    }
            };
            int insertIndex = Math.Min(3, Host.FPMenuMap.Items.Count); // just in case context menu shrinks someday
            Host.FPMenuMap.Items.Insert(insertIndex, contextMenuItem);

            // waypoint/fence grid on FlightPlanner GCS view
            commands = Host.MainForm.FlightPlanner.Commands;

            form = new FenceReplannerUI
            {
                Text = $"{Name} {Version}"
            };
            LoadSetting("FenceReplanner_ArcSegmentLength", form.num_ArcSegmentLength);
            LoadSetting("FenceReplanner_FenceMargin", form.num_FenceMargin);
            LoadSetting("FenceReplanner_MinDistance", form.num_MinDistance);
            form.but_TrimPolygon.Click += TrimPolygonClick;
            form.but_ReplanMission.Click += ReplanMissionClick;
            form.FormClosing += Form_FormClosing;
            return true;
        }

        public override bool Loop()
        {
            UpdateFencePoints(); // ugly hack to poach fences from UI while user is editing them
            return true;
        }

        public override bool Exit()
        {
            return true;
        }

        public void OpenDialog(object sender, EventArgs e)
        {
            form.ShowDialog();
        }

        private void TrimPolygonClick(object sender, EventArgs e)
        {
            if (Host.FPDrawnPolygon == null || Host.FPDrawnPolygon.Points.Count < 3) { return; }

            var exclusionFencesUTM = ConvertFencesToPolygonsUTM(
                GetExclusionFences(),
                (double)form.num_ArcSegmentLength.Value,
                (double)form.num_FenceMargin.Value,
                out int utmZone);

            if (exclusionFencesUTM == null || exclusionFencesUTM.Count < 1) { return; }

            // convert user polygon to UTM
            var drawnPolygon = new List<PointLatLngAlt>();
            Host.FPDrawnPolygon.Points.ForEach(p => drawnPolygon.Add(p));
            var drawnPolygonUTM = GeodeticPolygonToUTM(drawnPolygon, utmZone);

            // now difference each exclusion fence with the drawn polygon
            var subject = PolygonUTMToClipperIntPt(drawnPolygonUTM);

            var clipperSubject = new Clipper();
            clipperSubject.AddPath(subject, PolyType.ptSubject, true);

            foreach (var exclusion in exclusionFencesUTM)
            {
                var clip = PolygonUTMToClipperIntPt(exclusion);
                clipperSubject.AddPath(clip, PolyType.ptClip, true);
            }

            var solution = new List<List<IntPoint>>();
            clipperSubject.Execute(
                ClipType.ctDifference,
                solution,
                PolyFillType.pftNonZero,
                PolyFillType.pftNonZero
            );

            if (solution.Count < 1)
            {
                return;
            }

            // first result should be the altered subject polygon
            // subsequent results are fully contained polygons that do not intersect the perimeter
            var resultUTM = ClipperIntPtToPolygonUTM(solution[0], utmZone);
            var resultLLA = PolygonUTMToGeodetic(resultUTM);
            Host.MainForm.FlightPlanner.redrawPolygonSurvey(resultLLA);
        }

        private void ReplanMissionClick(object sender, EventArgs e)
        {
            var exclusionFencesUTM = ConvertFencesToPolygonsUTM(
                GetExclusionFences(),
                (double)form.num_ArcSegmentLength.Value,
                (double)form.num_FenceMargin.Value,
                out int utmZone);

            if (exclusionFencesUTM == null || exclusionFencesUTM.Count < 1) { return; }

            var waypointsUTM = GetWaypointsUTM(utmZone);

            if (waypointsUTM == null || waypointsUTM.Count < 1)
            {
                return; // silently exit (will leave mission as is)
            }

            var interimPlanUTM = new List<utmpos> { waypointsUTM[0] };

            // handle waypoints inside fences and build interimPlanUTM to avoid them
            int idx = 1;
            while (idx < waypointsUTM.Count)
            {
                bool prevInside = exclusionFencesUTM.Any(f => PointInPolygonUTM(waypointsUTM[idx - 1], f));
                List<utmpos> offendingFence = exclusionFencesUTM.FirstOrDefault(f => PointInPolygonUTM(waypointsUTM[idx], f));
                if (!prevInside && offendingFence != null)
                {
                    // entering a fence: segment from outside to inside
                    // track the specific fence being entered
                    var entrySegment = new List<utmpos> { waypointsUTM[idx - 1], waypointsUTM[idx] };
                    for (int j = idx + 1; j < waypointsUTM.Count; j++)
                    {
                        bool nextOutside = !PointInPolygonUTM(waypointsUTM[j], offendingFence);
                        if (nextOutside)
                        {
                            // exiting the fence: segment from last inside to first outside
                            var exitSegment = new List<utmpos> { waypointsUTM[j - 1], waypointsUTM[j] };
                            // find a path around the specific offending fence
                            var detour = GetPathAroundPolygon(entrySegment, exitSegment, offendingFence);
                            if (detour != null && detour.Count > 0)
                            {
                                interimPlanUTM.AddRange(detour);
                            }
                            else
                            {
                                // if no detour found, just skip to the exit point
                                interimPlanUTM.Add(waypointsUTM[j]);
                            }
                            idx = j;
                            break;
                        }
                    }
                    // no exit, break out (rest are inside fence)
                    if (idx < waypointsUTM.Count && PointInPolygonUTM(waypointsUTM[idx], offendingFence))
                    {
                        break;
                    }
                }
                else if (!exclusionFencesUTM.Any(f => PointInPolygonUTM(waypointsUTM[idx], f)))
                {
                    // normal case: add waypoint if not inside a fence
                    interimPlanUTM.Add(waypointsUTM[idx]);
                    idx++;
                }
                else
                {
                    // if both prev and curr are inside, skip this point
                    idx++;
                }
            }

            var replannedMissionUTM = new List<utmpos> { interimPlanUTM[0] };

            // handle segments that cross fences
            for (int i = 1; i < interimPlanUTM.Count; i++)
            {
                var segment = new List<utmpos> { interimPlanUTM[i - 1], interimPlanUTM[i] };
                var replanPaths = new List<List<utmpos>>();
                exclusionFencesUTM.ForEach(f =>
                {
                    var newPath = GetPathAroundPolygon(segment, f);
                    if (newPath.Count > 2) { replanPaths.Add(newPath); }
                });

                // sort by distance from segment origin
                var sortedPaths = replanPaths
                    .OrderBy(path => segment[0].GetDistance(path[0]))
                    .ToList();

                sortedPaths.ForEach(path => replannedMissionUTM.AddRange(path));

                replannedMissionUTM.Add(interimPlanUTM[i]);
            }

            // remove duplicate/very close waypoints
            var filteredMissionUTM = new List<utmpos> { replannedMissionUTM[0] };
            var filterCount = 0;
            for (int i = 1; i < replannedMissionUTM.Count; i++)
            {
                if (replannedMissionUTM[i].GetDistance(replannedMissionUTM[i - 1]) > (double)form.num_MinDistance.Value)
                {
                    filteredMissionUTM.Add(replannedMissionUTM[i]);
                }
                else
                {
                    filterCount++;
                }
            }

            Console.WriteLine("Filtered waypoints: " + filterCount);

            var replannedMissionLLA = filteredMissionUTM.Select(p => p.ToLLA()).ToList();
            WriteWaypointMission(replannedMissionLLA);
        }

        private void WriteWaypointMission(List<PointLatLngAlt> waypoints)
        {
            commands.Rows.Clear();
            waypoints.ForEach(wp =>
            {
                int idx = commands.Rows.Add();
                commands.Rows[idx].Cells[0].Value = "WAYPOINT";
                commands.Rows[idx].Cells[5].Value = wp.Lat.ToString();
                commands.Rows[idx].Cells[6].Value = wp.Lng.ToString();
                commands.Rows[idx].Cells[7].Value = wp.Alt.ToString();
            });
            // redraw the map
            Host.MainForm.FlightPlanner.writeKML();
        }

        private void UpdateFencePoints()
        {
            // no need to try and poach fences from the UI if a vehicle is connected and has fences onboard
            if (MainV2.comPort.MAV.fencepoints.Count > 0 || commands.Rows.Count == 0)
            {
                return;
            }

            // if the first item isn't a fence, ignore all
            if (!commands.Rows[0].Cells[0].Value.ToString().StartsWith("FENCE"))
            {
                return;
            }

            fencepoints = new Dictionary<int, mavlink_mission_item_int_t>();

            foreach (DataGridViewRow cmd in commands.Rows)
            {
                if (Enum.TryParse<MAV_CMD>(cmd.Cells[0].Value.ToString(), out var parsedCommand) &&
                    float.TryParse(cmd.Cells[1].Value?.ToString(), out float param1) &&
                    float.TryParse(cmd.Cells[2].Value?.ToString(), out float param2) &&
                    float.TryParse(cmd.Cells[3].Value?.ToString(), out float param3) &&
                    float.TryParse(cmd.Cells[4].Value?.ToString(), out float param4) &&
                    double.TryParse(cmd.Cells[5].Value?.ToString(), out double lat) &&
                    double.TryParse(cmd.Cells[6].Value?.ToString(), out double lng) &&
                    double.TryParse(cmd.Cells[7].Value?.ToString(), out double alt))
                {
                    var item = new mavlink_mission_item_int_t()
                    {
                        command = (ushort)parsedCommand,
                        param1 = param1,
                        param2 = param2,
                        param3 = param3,
                        param4 = param4,
                        x = (int)(lat * 1e7),
                        y = (int)(lng * 1e7),
                        z = (int)(alt * 1e7),
                    };
                    fencepoints.Add(fencepoints.Count, item);
                }
            }
        }

        private List<IEnumerable<KeyValuePair<int, mavlink_mission_item_int_t>>> GetExclusionFences()
        {
            if (fencepoints == null || MainV2.comPort.MAV.fencepoints.Count > 0)
            {
                fencepoints = new Dictionary<int, mavlink_mission_item_int_t>(MainV2.comPort.MAV.fencepoints);
            }

            // filter all but exclusion fences
            var exclusionFences = fencepoints
                .Where(a =>
                    a.Value.command == (ushort)MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION ||
                    a.Value.command == (ushort)MAV_CMD.FENCE_CIRCLE_EXCLUSION)
                .ChunkByField((a, b, count) =>
                {
                    // circles stand alone
                    if (a.Value.command == (ushort)MAV_CMD.FENCE_CIRCLE_EXCLUSION)
                        return false;

                    // param1 stores expected vertex count
                    if (count >= b.Value.param1)
                        return false;

                    return a.Value.command == b.Value.command;
                })
                .ToList();

            return exclusionFences;
        }

        private List<utmpos> GetWaypointsUTM(int zone)
        {
            // return empty list if there are no waypoints to process
            if (commands.Rows.Count < 1) { return new List<utmpos>(); }

            // if the first item is a fence, ignore all
            if (commands.Rows[0].Cells[0].Value.ToString().StartsWith("FENCE")) { return new List<utmpos>(); }

            var waypoints = commands.Rows
                .Cast<DataGridViewRow>()
                .Where(row =>
                    row.Cells[0].Value?.ToString() == "WAYPOINT" &&
                    double.TryParse(row.Cells[5].Value?.ToString(), out _) &&
                    double.TryParse(row.Cells[6].Value?.ToString(), out _) &&
                    double.TryParse(row.Cells[7].Value?.ToString(), out _))
                .Select(row =>
                {
                    double.TryParse(row.Cells[5].Value?.ToString(), out double lat);
                    double.TryParse(row.Cells[6].Value?.ToString(), out double lng);
                    double.TryParse(row.Cells[7].Value?.ToString(), out double alt);
                    return new PointLatLngAlt(lat, lng, alt);
                })
                .ToList();

            return utmpos.ToList(PointLatLngAlt.ToUTM(zone, waypoints), zone);

        }

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }

        private void LoadSetting(string key, Control item)
        {
            if (Host.config.ContainsKey(key))
            {
                if (item is NumericUpDown ud)
                {
                    ud.Value = decimal.Parse(Host.config[key].ToString());
                }
                else if (item is ComboBox cb)
                {
                    cb.Text = Host.config[key].ToString();
                }
                else if (item is CheckBox chk)
                {
                    chk.Checked = bool.Parse(Host.config[key].ToString());
                }
                else if (item is RadioButton rb)
                {
                    rb.Checked = bool.Parse(Host.config[key].ToString());
                }
            }
        }

        private void SaveSettings()
        {
            Host.config["FenceReplanner_ArcSegmentLength"] = form.num_ArcSegmentLength.Value.ToString();
            Host.config["FenceReplanner_FenceMargin"] = form.num_FenceMargin.Value.ToString();
            Host.config["FenceReplanner_MinDistance"] = form.num_MinDistance.Value.ToString();
        }

        //*** UTILITY FUNCTIONS ***//

        /**
         * Convert exclusion fences to UTM polygons
         * 
         * @param {List<IEnumerable<KeyValuePair<int, mavlink_mission_item_int_t>>>} fences
         * @param {double} arcSegmentLength - target for min distance between points (m)
         * @param {double} margin - optionally offset (grow/shrink) polygons by this amount (m)
         * @param {out int} zone - by reference - UTM zone
         * @returns {List<List<utmpos>>} - nested list of fence polygons
         */
        public static List<List<utmpos>> ConvertFencesToPolygonsUTM(
            List<IEnumerable<KeyValuePair<int, mavlink_mission_item_int_t>>> fences,
            double arcSegmentLength, double margin, out int zone)
        {
            var polygons = new List<List<utmpos>>();

            if (fences == null || fences.Count < 1)
            {
                zone = -1; // error value
                return polygons;
            }

            // convert fences to UTM
            polygons = FencesToPolygonsUTM(
                fences, out zone,
                arcSegmentLength, margin);

            return JoinIntersectingFences(polygons);
        }

        /**
         * Offset (grow/shrink) a polygon in UTM coordinates
         * 
         * @param {List<utmpos>} polygon
         * @param {int} zone - UTM zone
         * @param {double} margin - margin by which to offset (negative values shrink)
         * @returns {List<utmpos>} - offset polygon (or empty list if no solution)
         */
        public static List<utmpos> OffsetPolygonUTM(List<utmpos> polygon, int zone, double margin)
        {
            var clipperOffset = new ClipperOffset();
            var points = PolygonUTMToClipperIntPt(polygon);
            clipperOffset.AddPath(points, JoinType.jtMiter, EndType.etClosedPolygon);
            var solution = new List<List<IntPoint>>();
            clipperOffset.Execute(ref solution, margin * CLPR_UTM_SCALE_FACTOR);
            if (solution.Count < 1)
            {
                return new List<utmpos>(); // return empty list
            }
            return ClipperIntPtToPolygonUTM(solution[0], zone);
        }

        /**
         * Convert UTM coordinates to Clipper IntPt
         * 
         * @param {List<utmpos>} polygon
         * @returns {List<IntPoint>}
         */
        public static List<IntPoint> PolygonUTMToClipperIntPt(List<utmpos> polygon)
        {
            return polygon
                .Select(p => new IntPoint((long)(p.x * CLPR_UTM_SCALE_FACTOR), (long)(p.y * CLPR_UTM_SCALE_FACTOR)))
                .ToList();
        }

        /**
         * Convert Clipper IntPt to UTM coordinates
         * 
         * @param {List<IntPoint>} path
         * @param {int} zone - UTM zone
         * @returns {List<utmpos>}
         */
        public static List<utmpos> ClipperIntPtToPolygonUTM(List<IntPoint> path, int zone)
        {
            return path.Select(pt => new utmpos(pt.X / CLPR_UTM_SCALE_FACTOR, pt.Y / CLPR_UTM_SCALE_FACTOR, zone)).ToList();
        }

        public static List<PointLatLngAlt> PolygonUTMToGeodetic(List<utmpos> polygon)
        {
            return polygon.Select(p => p.ToLLA()).ToList();
        }

        /**
         * Convert LLA polygon to UTM coordinates
         * 
         * @param {List<PointLatLngAlt>} polygon
         * @param {int} zone - UTM zone
         * @param {double} margin - optionally offset (grow/shrink) polygon by this amount (m)
         * @returns {List<utmpos>}
         */
        public static List<utmpos> GeodeticPolygonToUTM(List<PointLatLngAlt> polygon, int zone, double margin = 0)
        {
            var polygonUTM = utmpos.ToList(PointLatLngAlt.ToUTM(zone, polygon), zone);

            if (margin > 0)
            {
                polygonUTM = OffsetPolygonUTM(polygonUTM, zone, margin);
            }

            // ensure clockwise wrap direction for future clipper operations
            return EnsureClockwise(polygonUTM);
        }

        /**
         * Convert circular exclusion fence to segmented polygon in UTM
         * 
         * @param {mavlink_mission_item_int_t} fence
         * @param {int} zone - UTM zone
         * @param {double} minSegmentLength - arc segments will be at least this long (m)
         * @param {double} margin - optionally offset (grow/shrink) polygon by this amount (m)
         * @returns {List<utmpos>}
         */
        public static List<utmpos> CircularExclusionToPolygonUTM(
            mavlink_mission_item_int_t fence, int zone, double minSegmentLength, double margin = 0)
        {
            double radius = fence.param1;
            var center = new PointLatLngAlt { Lat = fence.x / 1e7, Lng = fence.y / 1e7 };
            var result = new List<PointLatLngAlt>();
            int segments = (int)Math.Ceiling(Math.PI * radius * 2.0 / minSegmentLength);
            segments = Math.Max(segments, 6); // make a hexagon, minimum
            for (int i = 0; i < segments; i++)
            {
                double angleDeg = i * (360f / segments);
                var vertex = center.newpos(angleDeg, radius + margin);
                result.Add(vertex);
            }

            return GeodeticPolygonToUTM(result, zone);
        }

        /**
         * Convert exclusion fence collection to UTM polygons
         * 
         * @param {List<IEnumerable<KeyValuePair<int, mavlink_mission_item_int_t>>>} fences
         * @param {int} zone - UTM zone (by reference - modified to zone used for conversion)
         * @param {double} arcSegmentLength - target for min distance between points (m)
         * @param {double} margin - optionally offset (grow/shrink) polygons by this amount (m)
         * @returns {List<List<utmpos>>} - nested list of fence polygons
         */
        public static List<List<utmpos>> FencesToPolygonsUTM(
            List<IEnumerable<KeyValuePair<int, mavlink_mission_item_int_t>>> fences,
             out int zone, double arcSegmentLength, double margin = 0)
        {
            var polygonsUTM = new List<List<utmpos>>();
            zone = -1;
            foreach (var chunk in fences)
            {
                if (!chunk.Any())
                {
                    continue;
                }

                mavlink_mission_item_int_t firstItem = chunk.First().Value;
                var command = (MAV_CMD)firstItem.command;

                if (zone < 0)
                {
                    var firstPoint = new PointLatLngAlt
                    {
                        Lat = firstItem.x / 1e7,
                        Lng = firstItem.y / 1e7,
                    };
                    zone = firstPoint.GetUTMZone();
                }

                if (command == MAV_CMD.FENCE_CIRCLE_EXCLUSION)
                {
                    // circular fence to polygonal estimate
                    polygonsUTM.Add(CircularExclusionToPolygonUTM(firstItem, zone, arcSegmentLength, margin));
                }
                else if (command == MAV_CMD.FENCE_POLYGON_VERTEX_EXCLUSION)
                {
                    var poly = chunk.Select(kvp => new PointLatLngAlt
                    {
                        Lat = kvp.Value.x / 1e7,
                        Lng = kvp.Value.y / 1e7
                    }).ToList();
                    polygonsUTM.Add(GeodeticPolygonToUTM(poly, zone, margin));
                }
            }
            return polygonsUTM;
        }

        /**
         * Check if a UTM point is inside or on a UTM polygon using Clipper's PointInPolygon
         * 
         * @param {utmpos} point - the point to check
         * @param {List<utmpos>} polygon - the polygon
         * @returns {bool} - true if inside or on the polygon, false if outside
         */
        public static bool PointInPolygonUTM(utmpos point, List<utmpos> polygon)
        {
            var pt = new IntPoint((long)(point.x * CLPR_UTM_SCALE_FACTOR), (long)(point.y * CLPR_UTM_SCALE_FACTOR));
            var poly = PolygonUTMToClipperIntPt(polygon);
            return Clipper.PointInPolygon(pt, poly) != 0;
        }

        /**
         * Insert intersections into a polygon
         * 
         * @param {List<utmpos>} segment - segment that intersects the polygon
         * @param {List<utmpos>} polygon
         * @param {List<int>} indices - by reference - indices of the polygon intersections
         * @returns {List<utmpos>} - resulting polygon with intersections inserted
         */
        public static List<utmpos> InsertIntersectionsIntoPolygon(List<utmpos> segment, List<utmpos> polygon, ref List<int> indices)
        {
            var resultPolygon = new List<utmpos>();
            if (indices == null || indices.Count == 0)
            {
                indices = new List<int>();
            }

            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % n];
                resultPolygon.Add(new utmpos(a));

                if (LineSegmentsIntersect(segment[0], segment[1], a, b, out var intersection))
                {
                    resultPolygon.Add(intersection);
                    indices.Add(resultPolygon.Count - 1);
                }
            }

            return resultPolygon;
        }

        /**
         * Find the index of a point in a polygon
         * 
         * @param {List<utmpos>} poly
         * @param {utmpos} pt
         * @param {double} tol
         * @returns {int}
         */
        public static int FindIndexByValue(List<utmpos> poly, utmpos pt, double tol = 1e-6)
        {
            for (int i = 0; i < poly.Count; i++)
                if (poly[i].GetDistance(pt) < tol)
                    return i;
            return -1;
        }

        /**
         * Get a path around a polygon
         * 
         * @param {List<utmpos>} segment1 - first segment that intersects the polygon
         * @param {List<utmpos>} segment2 - second segment that intersects the polygon
         * @param {List<utmpos>} polygon
         * @returns {List<utmpos>} - resulting path around the polygon, connecting the two segments
         */
        public static List<utmpos> GetPathAroundPolygon(List<utmpos> segment1, List<utmpos> segment2, List<utmpos> polygon)
        {
            // find and save intersections of segment1 with polygon
            var indicesSegment1 = new List<int>();
            var intersectedPolygon = InsertIntersectionsIntoPolygon(segment1, polygon, ref indicesSegment1);
            var intersectionsSegment1 = indicesSegment1.Select(idx => intersectedPolygon[idx]).ToList();

            var indices = new List<int>();
            intersectedPolygon = InsertIntersectionsIntoPolygon(segment2, intersectedPolygon, ref indices);

            // there is either no intersection or no valid path around
            if (indicesSegment1.Count + indices.Count < 2) { return new List<utmpos>(); }

            // re-insert intersections of segment1 into polygon (was mutated by segment2)
            intersectionsSegment1.ForEach(pt =>
            {
                var idx = FindIndexByValue(intersectedPolygon, pt);
                if (idx != -1)
                {
                    indices.Add(idx);
                }
            });

            // sort indices by distance from segment1 start point
            var sorted = indices
                .OrderBy(idx => segment1[0].GetDistance(intersectedPolygon[idx]))
                .ToList();

            // use closest and farthest intersections to traverse the polygon
            int startIndex = sorted.First();
            int endIndex = sorted.Last();

            var cwPath = Traverse(intersectedPolygon, startIndex, endIndex, +1, out double cwDistance);
            var ccwPath = Traverse(intersectedPolygon, startIndex, endIndex, -1, out double ccwDistance);
            return cwDistance <= ccwDistance ? cwPath : ccwPath;
        }

        /**
         * Get a path around a polygon
         * 
         * @param {List<utmpos>} segment - segment that intersects the polygon
         * @param {List<utmpos>} polygon
         * @returns {List<utmpos>} - resulting path around the polygon
         */
        public static List<utmpos> GetPathAroundPolygon(List<utmpos> segment, List<utmpos> polygon)
        {
            // just return the segment argument if we can't apply the algorithm
            if (segment.Count != 2 || polygon.Count < 3) { return segment; }

            var indices = new List<int>();
            var intersectedPolygon = InsertIntersectionsIntoPolygon(segment, polygon, ref indices);

            if (indices.Count < 2) { return segment; } // there is either no intersection or no valid path around

            var sorted = indices
                .OrderBy(idx => segment[0].GetDistance(intersectedPolygon[idx]))
                .ToList();
            int startIndex = sorted.First();
            int endIndex = sorted.Last();

            var cwPath = Traverse(intersectedPolygon, startIndex, endIndex, +1, out double cwDistance);
            var ccwPath = Traverse(intersectedPolygon, startIndex, endIndex, -1, out double ccwDistance);
            return cwDistance <= ccwDistance ? cwPath : ccwPath;
        }

        /**
         * Traverse a path in a given direction
         * 
         * @param {List<utmpos>} path
         * @param {int} start - starting index
         * @param {int} end - ending index
         * @param {int} direction - direction to traverse (1 for clockwise, -1 for counterclockwise)
         * @param {out double} distance - by reference - total distance traversed
         * @returns {List<utmpos>} - resulting path
         */
        public static List<utmpos> Traverse(List<utmpos> path, int start, int end, int direction, out double distance)
        {
            var resultPath = new List<utmpos>();
            distance = 0;
            utmpos prev = default;
            bool first = true;
            int count = path.Count;

            for (int i = start; i != end; i = (i + direction + count) % count)
            {
                var current = path[i];
                resultPath.Add(current);
                if (!first)
                {
                    distance += prev.GetDistance(current);
                }
                else first = false;
                prev = current;
            }

            resultPath.Add(path[end]);
            distance += prev.GetDistance(path[end]);
            return resultPath;
        }

        /**
         * Check if two line segments intersect
         * 
         * @param {utmpos} p1 - first point of first segment
         * @param {utmpos} p2 - second point of first segment
         * @param {utmpos} q1 - first point of second segment
         * @param {utmpos} q2 - second point of second segment
         * @param {out utmpos} intersection - by reference
         * @returns {bool}
         */
        public static bool LineSegmentsIntersect(utmpos p1, utmpos p2, utmpos q1, utmpos q2, out utmpos intersection)
        {
            intersection = default;

            double a1 = p2.y - p1.y;
            double b1 = p1.x - p2.x;
            double c1 = a1 * p1.x + b1 * p1.y;

            double a2 = q2.y - q1.y;
            double b2 = q1.x - q2.x;
            double c2 = a2 * q1.x + b2 * q1.y;

            double det = a1 * b2 - a2 * b1;

            if (Math.Abs(det) < 1e-10)
                return false; // parallel lines

            double x = (b2 * c1 - b1 * c2) / det;
            double y = (a1 * c2 - a2 * c1) / det;

            // check if intersection is within both segments
            if (IsBetween(p1.x, p2.x, x) && IsBetween(p1.y, p2.y, y) &&
                IsBetween(q1.x, q2.x, x) && IsBetween(q1.y, q2.y, y))
            {
                intersection = new utmpos(x, y, p1.zone);
                return true;
            }

            return false;
        }

        /**
         * Check if a value is between two other values (with some epsilon/tolerance)
         * 
         * @param {double} a
         * @param {double} b
         * @param {double} val
         * @returns {bool}
         */
        public static bool IsBetween(double a, double b, double val)
        {
            const double epsilon = 1e-6;
            return (val >= Math.Min(a, b) - epsilon) && (val <= Math.Max(a, b) + epsilon);
        }

        /**
         * Join intersecting fences
         * 
         * @param {List<List<utmpos>>} fences
         * @returns {List<List<utmpos>>} - joined fences (using clipper union operations)
         */
        public static List<List<utmpos>> JoinIntersectingFences(List<List<utmpos>> fences)
        {
            if (fences == null || fences.Count == 0)
                return new List<List<utmpos>>();

            int zone = fences[0].Count > 0 ? fences[0][0].zone : 0;

            var paths = fences.Select(PolygonUTMToClipperIntPt).ToList();

            // union all polygons using Clipper
            // those that do not intersect will appear separately in the result
            var clipper = new Clipper();
            foreach (var path in paths)
            {
                clipper.AddPath(path, PolyType.ptSubject, true);
            }
            var solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            var result = solution.Select(path => ClipperIntPtToPolygonUTM(path, zone)).ToList();

            return result;
        }

        /**
         * Check if a polygon is clockwise
         * 
         * @param {List<utmpos>} poly
         * @returns {bool}
         */
        public static bool IsClockwise(List<utmpos> poly)
        {
            double sum = 0;
            for (int i = 0; i < poly.Count - 1; i++)
            {
                sum += (poly[i + 1].x - poly[i].x) * (poly[i + 1].y + poly[i].y);
            }
            return sum > 0;
        }

        /**
         * Ensure a polygon is clockwise
         * 
         * @param {List<utmpos>} poly
         * @returns {List<utmpos>}
         */
        public static List<utmpos> EnsureClockwise(List<utmpos> poly)
        {
            if (!IsClockwise(poly)) { poly.Reverse(); }
            return poly;
        }

        /**
         * Print fences to console in QGC file format
         * 
         * @param {List<List<utmpos>>} fences
         */
        public static void PrintFences(List<List<utmpos>> fences)
        {
            if (fences == null || fences.Count == 0)
            {
                Console.WriteLine("Fence Replanner: No fences to print.");
                return;
            }

            int wpIndex = 0;
            var home = fences[0][0].ToLLA();  // "home" is just the first point of the first fence
            Console.WriteLine("QGC WPL 110");
            Console.WriteLine($"{wpIndex++} 1 0 16 0 0 0 0 {home.Lat} {home.Lng} {home.Alt} 1");
            fences.ForEach(f =>
            {
                f.ForEach(p =>
                {
                    PointLatLngAlt lla = p.ToLLA();
                    Console.WriteLine($"{wpIndex++} 0 3 5002 {f.Count} 0 0 0 {lla.Lat} {lla.Lng} {lla.Alt} 0 1");
                });
            });
        }
    }
}