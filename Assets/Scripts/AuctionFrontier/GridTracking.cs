using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace
{
    public class GridTracking : MonoBehaviour
    {
        public int width;
        public int height;
        public int cellSize;
        public static int TotalNumberOfCells;

        private Mesh m_Mesh;
        private static GridWorld m_GridWorld;
        private static Vector3 _cellsizeVec = new Vector3(5f, 1f, 5f);
        private static Vector3 _offsetVec = new Vector3(50f, 0f, 50f);
        
        public static HashSet<Vector3> FRONTIERS = new HashSet<Vector3>();

        // Frontier Exploration
        private static AuctionFrontierUtil.CELL_STATE[,] m_CellState;

        public void Awake()
        {
            m_Mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = m_Mesh;
            SetGridWorld(new GridWorld(width, height, cellSize));
            m_CellState = new AuctionFrontierUtil.CELL_STATE[height, width];
            TotalNumberOfCells = width * height;
        }

        public void Reset()
        {
            m_CellState = new AuctionFrontierUtil.CELL_STATE[height, width];
            m_GridWorld.ResetGrid();
        }
        
        /*
        public void Start()
        {
            var agents = GameObject.FindGameObjectsWithTag("agent").Where(a => a.layer == 0);
        }
        */

        public static void GetXZ(Vector3 wolrdPosition, out int x, out int z)
        {
            m_GridWorld.GetXZOffset(wolrdPosition, m_GridWorld.Offset(), out x, out z);
        }
        

        public void SetGridWorld(GridWorld world)
        {
            m_GridWorld = world;
        }

        public void SetValue(Vector3 worldPosition, int value)
        {
            m_GridWorld.SetValue(worldPosition, value);
        }

        public void SetValue(int x, int z, int value)
        {
            m_GridWorld.SetValue(x, z, value);
        }

        public int GetGridValue(Vector3 worldPosition)
        {
            return m_GridWorld.GetValue(worldPosition);
        }

        public int GetGridValue(int x, int z)
        {
            return m_GridWorld.GetGridValue(x, z);
        }

        public static bool GridWorldComplete()
        {
            return m_GridWorld.GridCompleted();
        }

        public static void MarkStationsInGrid()
        {
            var stations = GameObject.FindGameObjectsWithTag("station");
            int x, z;
            foreach (var station in stations)
            {
                Vector3 pos = station.transform.position;
                GetXZ(pos, out x, out z);
                m_GridWorld.SetValue(x, z, 1);
                var adj_cells = GetCellAdjacentFromXZ(x, z);
                foreach (var cell in adj_cells)
                {
                    m_GridWorld.SetValue((int) cell.x, (int) cell.z, 1);
                    m_CellState[(int)cell.x, (int)cell.z] = AuctionFrontierUtil.CELL_STATE.MAP_OPEN;
                }
            }
        }

        public static void GridTrackingReset()
        {
            m_CellState = new AuctionFrontierUtil.CELL_STATE[(int) GetWidth(),(int) GetHeight()];
            m_GridWorld.ResetGrid();
            MarkStationsInGrid();
        }

        public static Vector3 WFD(Vector3 worldPosition, float drawDuration = 1f)
        {
            var frontier_size_limit = 25; // To allow for more frontier points
            var queue_m = new Queue<Vector3>(); 
            int x, z;
            GetXZ(worldPosition, out x, out z);
            var CellState = (AuctionFrontierUtil.CELL_STATE[,]) m_CellState.Clone();
            
            CellState[z, x] = AuctionFrontierUtil.CELL_STATE.MAP_OPEN;
            queue_m.Enqueue(new Vector3(x, 1f, z));
            var frontier = new List<(Vector3, float)>();
            //var prev_pos = worldPosition;
            //Debug.DrawRay(worldPosition, Vector3.up * 10f, Color.blue, 10f);
            
            while (queue_m.Count > 0)
            {
                var elem = queue_m.Dequeue();
                
                /* DEBUG - DRAW VISITED CELL */
                //var debug_draw = GridCoordToWorld(elem);
                //Debug.DrawRay(debug_draw, Vector3.up * 10f, Color.magenta, drawDuration);
                
                if (CellState[(int) elem.z, (int) elem.x] == AuctionFrontierUtil.CELL_STATE.MAP_CLOSE 
                    || CellState[(int) elem.z, (int) elem.x] == AuctionFrontierUtil.CELL_STATE.NONE) continue;

                if (IsCellFrontier(elem))
                {
                    var counter = 0;
                    var queue_f = new Queue<Vector3>();
                    var new_frontier = new List<(Vector3, float)>();
                    
                    
                    // Mark current pos as Frontier Open
                    CellState[(int)elem.z, (int)elem.x] = AuctionFrontierUtil.CELL_STATE.FRONTIER_OPEN;
                    queue_f.Enqueue(elem);
                    
                    // Extract connected frontier cells (Cap frontier to atmost 10 connected points)
                    while (queue_f.Count > 0 && counter < frontier_size_limit)
                    {
                        
                        var cell = queue_f.Dequeue();
                        var cell_state = CellState[(int)cell.z, (int)cell.x];
                        if (cell_state == AuctionFrontierUtil.CELL_STATE.MAP_CLOSE 
                            || cell_state == AuctionFrontierUtil.CELL_STATE.FRONTIER_CLOSE) continue;

                        if (IsCellFrontier(cell))
                        {
                            /* DEBUG - DRAW FRONTIER POINTS */
                            var draw = GridCoordToWorld(cell);
                            Debug.DrawRay(draw, Vector3.up * 10f, Color.cyan, drawDuration);
                            
                            new_frontier.Add((cell, Vector3.Distance(worldPosition, GridCoordToWorld(cell))));
                            var cell_adj = GetCellAdjacent(cell);
                            foreach (var w in cell_adj)
                            {
                                var w_cellstate = CellState[(int)w.z, (int)w.x];
                                if (w_cellstate == AuctionFrontierUtil.CELL_STATE.MAP_OPEN)
                                {
                                    if (counter < frontier_size_limit)
                                    {
                                        queue_f.Enqueue(w);
                                        counter++;
                                    }

                                    CellState[(int)w.z, (int)w.x] = AuctionFrontierUtil.CELL_STATE.FRONTIER_OPEN;
                                }
                            }
                        }

                        CellState[(int)cell.z, (int)cell.x] = AuctionFrontierUtil.CELL_STATE.FRONTIER_CLOSE;
                    }

                    // Calculate median for new frontier and add to list over frontiers
                    var tmp = new_frontier.OrderBy(x => x.Item2).ToList();
                    if (tmp.Count > 0)
                    {
                        var median = tmp[tmp.Count / 2];
                        Debug.DrawRay(GridCoordToWorld(median.Item1), Vector3.up * 10f, Color.red, drawDuration);
                        frontier.Add(median);
                        FRONTIERS.Add(median.Item1);
                        
                        /* DEBUG - DRAW FRONTIER MEDIAN */
                        //Debug.DrawRay(GridCoordToWorld(median.Item1), Vector3.up * 10f, Color.red, drawDuration);

                    }

                    // Mark all points in frontier as map_close
                    foreach (var fcell in new_frontier) { CellState[(int)fcell.Item1.z, (int)fcell.Item1.x] = AuctionFrontierUtil.CELL_STATE.MAP_CLOSE; }
                }
                else
                {
                    FRONTIERS.Remove(elem);
                }

                var elem_adj = GetCellAdjacent(elem);
                foreach (var neightbour in elem_adj)
                {
                    var neightbour_state = CellState[(int)neightbour.z, (int)neightbour.x];
                    if (//neightbour_state != AuctionFrontierUtil.CELL_STATE.MAP_OPEN &&
                        neightbour_state != AuctionFrontierUtil.CELL_STATE.MAP_CLOSE &&
                        neightbour_state != AuctionFrontierUtil.CELL_STATE.NONE &&
                        HaveAdjOpenCell(neightbour))
                    {
                        queue_m.Enqueue(neightbour);
                        //Debug.DrawRay(GridCoordToWorld(neightbour), Vector3.up * 10f, Color.magenta, 10f);
                        CellState[(int)neightbour.z, (int)neightbour.x] = AuctionFrontierUtil.CELL_STATE.MAP_OPEN;
                    }
                }

                CellState[(int)elem.z, (int)elem.x] = AuctionFrontierUtil.CELL_STATE.MAP_CLOSE;
            }

            if (frontier.Count > 0)
            {
                return GridCoordToWorld(frontier.OrderBy(x => x.Item2).ToList()[0].Item1);
            }

            return Vector3.up;
        }

        public static bool IsCellFrontier(Vector3 cell)
        {
            float h = GetHeight();
            float w = GetWidth();
            
            var z_lower = Math.Clamp(cell.z-1, 0f, h-1);
            var z_upper = Math.Clamp(cell.z+1, 0f, h-1);
            
            var x_lower = Math.Clamp(cell.x-1, 0f, w-1);
            var x_upper = Math.Clamp(cell.x+1, 0f, w-1);

            int found = 0;
            for (float z = z_lower; z <= z_upper; z++)
            {
                for (float x = x_lower; x <= x_upper; x++)
                {
                    if (m_GridWorld.GetGridValue((int)x, (int)z) == 0 && cell.z != z && cell.x != x)
                    {
                        found++;
                        break;
                    }
                }
            }

            return found > 0;
        }
        
        public static bool HaveAdjOpenCell(Vector3 cell)
        {
            float h = GetHeight();
            float w = GetWidth();
            
            var z_lower = Math.Clamp(cell.z-1, 0f, h-1);
            var z_upper = Math.Clamp(cell.z+1, 0f, h-1);
            
            var x_lower = Math.Clamp(cell.x-1, 0f, w-1);
            var x_upper = Math.Clamp(cell.x+1, 0f, w-1);

            int found = 0;
            for (float z = z_lower; z <= z_upper; z++)
            {
                for (float x = x_lower; x <= x_upper; x++)
                {
                    if (m_GridWorld.GetGridValue((int)x, (int)z) == 1 && cell.z != z && cell.x != x) found++;
                }
            }

            return found > 0; 
        }

        public static IEnumerable<Vector3> GetCellAdjacent(Vector3 cell)
        {
            return m_GridWorld.GetAdjacentCells(cell.x, cell.z);
        }
        
        public static IEnumerable<Vector3> GetCellAdjacentFromXZ(int x, int z)
        {
            return m_GridWorld.GetAdjacentCells(x, z);
        }


        public static Vector3 GridCoordToWorld(Vector3 cell)
        {
            return Vector3.Scale(cell, _cellsizeVec) - _offsetVec;
        }

        public static float GetHeight()
        {
            return m_GridWorld.GetHeight();
        }
        
        public static float GetWidth()
        {
            return m_GridWorld.GetWidth();
        }

        public int UpdateGridWithSensor(Transform agent, float longitude, float range, int value)
        {
            var pos = agent.position;
            var tmp = new GameObject();
            tmp.transform.SetPositionAndRotation(pos, agent.rotation);
            
            tmp.transform.Rotate(Vector3.down, longitude/2);
            var v1 = tmp.transform.forward * range;
            
            tmp.transform.Rotate(Vector3.up, longitude);
            var v2 = tmp.transform.forward * range;
            DestroyImmediate(tmp);

            
            float[] zs = { v1.z+pos.z, v2.z+pos.z, pos.z };
            float[] xs = { v1.x+pos.x, v2.x+pos.x, pos.x };
            
            var z_min = (int) Math.Clamp(zs.Min() + m_GridWorld.Offset(), 0f, 99f)/cellSize;
            var z_max = (int) Math.Clamp(zs.Max() + m_GridWorld.Offset(), 0f, 99f)/cellSize;

            var x_min = (int) Math.Clamp(xs.Min() + m_GridWorld.Offset(), 0f, 99f)/cellSize;
            var x_max = (int) Math.Clamp(xs.Max() + m_GridWorld.Offset(), 0f, 99f)/cellSize;
            
            Vector3 coord;
            var dist = 0f;
            var angle1 = 0f;
            var angle2 = 0f;
            
            //Debug.Log($"Z - min {z_min}  max {z_max}");
            //Debug.Log($"X - min {x_min}  max {x_max}");

            int num_discovered_cells = 0;
            for (int z = z_min; z <= z_max; z++)
            {
                for (int x = x_min; x <= x_max; x++)
                {
                    // Check if cell is a stored frontier point. Remove if true
                    coord = GridCoordToWorld(new Vector3(x, 1f, z));
                    if (!IsCellFrontier(coord))
                    {
                        FRONTIERS.Remove(coord);
                    }

                    if(m_GridWorld.GetGridValue(x, z) > 0) continue;
                    
                    // Check if point between is in FOV
                    angle1 = Vector3.Angle(v1, coord-pos);
                    angle2 = Vector3.Angle(v2, coord-pos);
                    dist = Vector3.Distance(pos, coord);
                    
                    
                    /* DEBUGGING */
                    //Debug.Log($"Angle {angle1}  Angle2 {angle2}");
                    //Debug.DrawRay(pos, coord-pos, Color.cyan, .5f);
                    // Debug.DrawRay(coord, new Vector3(0f, 10f, 0f), Color.yellow, 6f); // DRAW Points instead
                    
                    if (angle1 < longitude && angle2 < longitude && dist <= range)
                    {
                        // Debug.Log($"******************************'");
                        num_discovered_cells++;
                        m_GridWorld.SetValue(x, z, value);
                        m_CellState[z, x] = AuctionFrontierUtil.CELL_STATE.MAP_OPEN;
                    }
                }
            }

            return num_discovered_cells;
        }
    }
}