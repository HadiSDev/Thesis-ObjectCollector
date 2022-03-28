using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    // Implementation by Code Monkey
    public class GridWorld
    {
        private int m_Width;
        private int m_Height;
        private float m_CellSize;
        private int[,] m_GridArray;

        // Methods to help visualize grid 
        public static TextMesh CreateWorldText(string text, Transform parent = null,
            Vector3 localPosition = default(Vector3),
            int fontSize = 40,
            Color color = default(Color),
            TextAnchor textAnchor = TextAnchor.MiddleCenter,
            TextAlignment textAlignment = TextAlignment.Center,
            int sortingOrder = 0)
        {
            if (color == null) color = Color.yellow;
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

            for (int x = 0; x < m_GridArray.GetLength(0); x++)
            {
                for (int y = 0; y < m_GridArray.GetLength(1); y++)
                {
                    CreateWorldText(m_GridArray[x, y].ToString(),
                        null, GetWorldPosition(x, y),
                        30,
                        Color.white,
                        TextAnchor.MiddleCenter);
                }
            }
        }

        private Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x, y) * m_CellSize;
        }
    }
}