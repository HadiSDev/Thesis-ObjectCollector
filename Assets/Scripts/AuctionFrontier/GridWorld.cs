using System;
using System.Collections.Generic;
using MBaske.Sensors.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    // Implementation by Code Monkey
    public class GridWorld
    {
        private float m_Offset = 50f;
        private int m_Width;
        private int m_Height;
        private float m_CellSize;
        private int[,] m_GridArray;
        private TextMesh[,] m_TextArray; // Only for debug
        
        // Methods to help visualize grid 
        public static TextMesh CreateWorldText(string text, Transform parent = null,
            Vector3 localPosition = default(Vector3),
            int fontSize = 40,
            Color color = default(Color),
            TextAnchor textAnchor = TextAnchor.MiddleCenter,
            TextAlignment textAlignment = TextAlignment.Center,
            int sortingOrder = 0)
        {
            return CreateWorldText(parent, text, localPosition, fontSize, color, textAnchor, textAlignment,
                sortingOrder);
        }

        public static TextMesh CreateWorldText(Transform parent,
            string text,
            Vector3 localPosition, 
            int fontSize,
            Color color,
            TextAnchor textAnchor,
            TextAlignment textAlignment,
            int sortingOrder
            )
        {
            GameObject gameObject = new GameObject("World_text", typeof(TextMesh));
            Transform transform = gameObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            TextMesh textMesh = gameObject.GetComponent<TextMesh>();
            textMesh.anchor = textAnchor;
            textMesh.alignment = textAlignment;
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = color;
            textMesh.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
            
            return textMesh;
        }

        public GridWorld(int width, int height, float cellSize)
        {
            m_Width = width;
            m_Height = height;
            m_CellSize = cellSize;
                m_GridArray = new int[m_Width, m_Height];
                m_TextArray = new TextMesh[m_Width, m_Height];

            for (int z = 0; z < m_GridArray.GetLength(0); z++)
            {
                for (int x = 0; x < m_GridArray.GetLength(1);x++)
                {
                    m_TextArray[z, x] = CreateWorldText(m_GridArray[z, x].ToString(),
                        null,
                        GetWorldPosition(x, z) + new Vector3(m_CellSize,  m_CellSize) * .5f, // Shift by half a cell size
                        30,
                        Color.white);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);   // HorizontalLine
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f); // VerticalLine
                }
            }
            Debug.DrawLine(GetWorldPosition(0, m_Height), GetWorldPosition(m_Width, m_Height), Color.white, 100f);   // Horizontal Lines
            Debug.DrawLine(GetWorldPosition(m_Width, 0), GetWorldPosition(m_Width, m_Height), Color.white, 100f);   // Vertical Lines

        }

        public int GetGridValue(int x, int z)
        {
            return m_GridArray[z, x];
        }

        public void SetValue(int x, int z, int value)
        {
            if (x >= 0 && z >= 0 && x < m_Width && z < m_Height)
            {
                m_GridArray[z, x] = value;
                m_TextArray[z, x].text = value.ToString();
            }
        }

        public void SetValue(Vector3 worldPosition, int value)
        {
            int x, z;
            GetXZ(worldPosition, out x, out z);
            SetValue(x, z, value);
        }
        
        public int GetValue(Vector3 worldPosition)
        {
            int x, z;
            GetXZ(worldPosition, out x, out z);
            return GetGridValue(x, z);
        }
        
        private Vector3 GetWorldPosition(int x, int z)
        {
            //return new Vector3(x,10f,   z) * m_CellSize;
            return new Vector3(x,  z) * m_CellSize;

        }
        
        
        private void GetXY(Vector3 worldPosition, out int x, out int y)
        {
            x = Mathf.FloorToInt(worldPosition.x / m_CellSize);
            y = Mathf.FloorToInt(worldPosition.y / m_CellSize);
        }
        
        
        private void GetXZ(Vector3 worldPosition, out int x, out int z)
        {
            x = Mathf.FloorToInt(worldPosition.x / m_CellSize);
            z = Mathf.FloorToInt(worldPosition.z / m_CellSize);
        }
        
        public void GetXZOffset(Vector3 worldPosition, float offset, out int x, out int z)
        {
            x = Mathf.FloorToInt((worldPosition.x + offset) / m_CellSize);
            z = Mathf.FloorToInt((worldPosition.z + offset) / m_CellSize);
        }
        
        public Vector3 GetXZOffset(Vector3 worldPosition, float offset)
        {
            var x = Mathf.FloorToInt((worldPosition.x + offset) / m_CellSize);
            var z = Mathf.FloorToInt((worldPosition.z + offset) / m_CellSize);
            
            return new Vector3(50, 0, 50);
        }

        public float GetHeight()
        {
            return m_Height;
        }
        
        public float GetWidth()
        {
            return m_Width;
        }

        public IEnumerable<Vector3> GetAdjacentCells(float x, float z)
        {
            
            var z_lower = Math.Clamp(z-1, 0f, m_Height-1);
            var z_upper = Math.Clamp(z+1, 0f, m_Height-1);
            
            var x_lower = Math.Clamp(x-1, 0f, m_Width-1);
            var x_upper = Math.Clamp(x+1, 0f, m_Width-1);

            var neightbours = new List<Vector3>();
            
            for (float zs = z_lower; zs <= z_upper; zs++)
            {
                for (float xs = x_lower; xs <= x_upper; xs++)
                {
                    if (zs == z && xs == x) continue;
                    neightbours.Add(new Vector3(xs, 1f, zs));
                }
            }

            return neightbours;
        }

        public void ResetGrid()
        {
            m_GridArray = new int[m_Height, m_Width];
            m_TextArray = new TextMesh[m_Height, m_Width];
        }

        public float Offset()
        {
            return m_Offset;
        }
    }
}