﻿using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Driver
{
    public class RoadChunk : Poolable
    {
        // Nominal length (no curvature).
        private const int c_Length = 12;
        // Distances outward from road center.
        private const float c_RoadExtent = 3.4f;
        private const float c_MeshExtent = 6f;
        private const float c_PoleDistance = 3.8f;
        // Applicable to cones and barrels.
        private static readonly float s_ObstacleXSpacing = c_RoadExtent / (c_Length + 1f);

        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private MeshCollider m_MeshCollider;
        private Vector3[] m_Vertices;
        private Vector3[] m_Normals;
        private ObstaclePool m_Pool;
        private Stack<Obstacle> m_Obstacles;

        private enum ObstacleType
        {
            None, Roadblock, Barrel, Cone
        }
        private ObstacleType m_ObstacleType;

        private void Awake()
        {
            m_Pool = FindObjectOfType<ObstaclePool>();
            m_Obstacles = new Stack<Obstacle>();

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshCollider = GetComponent<MeshCollider>();
            m_Mesh = GenerateMesh();
            m_Mesh.MarkDynamic();

            m_Vertices = m_Mesh.vertices;
            m_Normals = m_Mesh.normals;
        }

        protected override void OnDiscard()
        {
            while (m_Obstacles.Count > 0)
            {
                m_Obstacles.Pop().Discard();
            }

            base.OnDiscard();
        }

        public void UpdateChunk(ReferenceFrame frame, bool isFirstChunk)
        {
            // TBD probabilities
            bool hasObstacle = !isFirstChunk && Util.RandomBool(0.1f);
            m_ObstacleType = hasObstacle
                ? (Util.RandomBool(0.2f)
                    ? ObstacleType.Roadblock
                    : (Util.RandomBool(0.5f)
                        ? ObstacleType.Barrel
                        : ObstacleType.Cone))
                : m_ObstacleType = ObstacleType.None;
            int side = Util.RandomBool(0.5f) ? -1 : 1;

            var tf = frame.transform;
            transform.rotation = tf.rotation;
            UpdateMesh(tf, 0);

            for (int z = 1, x; z <= c_Length; z++)
            {
                tf = frame.MoveFrame();
                UpdateMesh(tf, z);

                if (z % 2 == 0)
                {
                    // Poles.
                    m_Obstacles.Push(m_Pool.Spawn(3, tf.position - tf.right * c_PoleDistance, tf.rotation));
                    m_Obstacles.Push(m_Pool.Spawn(3, tf.position + tf.right * c_PoleDistance, tf.rotation));
                }

                switch (m_ObstacleType)
                {
                    case ObstacleType.Roadblock:
                        x = c_Length / 2;
                        if (x == z)
                        {
                            // Single roadblock.
                            m_Obstacles.Push(m_Pool.Spawn(0, ObstacleSpawnPos(tf, x * side), tf.rotation));
                        }
                        break;
                    case ObstacleType.Barrel:
                        x = (c_Length - z) * side;
                        m_Obstacles.Push(m_Pool.Spawn(1, ObstacleSpawnPos(tf, x), tf.rotation));
                        break;
                    case ObstacleType.Cone:
                        x = (c_Length - z) * side;
                        m_Obstacles.Push(m_Pool.Spawn(2, ObstacleSpawnPos(tf, x), tf.rotation));
                        break;
                }
            }
 
            m_Mesh.vertices = m_Vertices;
            m_Mesh.normals = m_Normals;
            m_Mesh.RecalculateBounds();
            m_Mesh.RecalculateBounds();
            m_MeshFilter.sharedMesh = m_Mesh;
            m_MeshCollider.sharedMesh = m_Mesh;
        }

        private Vector3 ObstacleSpawnPos(Transform tf, int x)
        {
            return tf.position + tf.right * x * s_ObstacleXSpacing;
        }

        private void UpdateMesh(Transform tf, int z)
        {
            m_Vertices[z * 2]     = transform.InverseTransformPoint(tf.position - tf.right * c_MeshExtent);
            m_Vertices[z * 2 + 1] = transform.InverseTransformPoint(tf.position + tf.right * c_MeshExtent);

            m_Normals[z * 2]      = tf.up;
            m_Normals[z * 2 + 1]  = tf.up;
        }

        private static Mesh GenerateMesh()
        {
            int n = (c_Length + 1) * 2;
            Vector3[] vertices = new Vector3[n];
            Vector3[] normals = new Vector3[n];
            Vector2[] uvs = new Vector2[n];

            int[] triangles = new int[c_Length * 6];

            for (int i = 0; i <= c_Length; i++)
            {
                int iL = i * 2;
                int iR = iL + 1;

                vertices[iL] = new Vector3(-c_MeshExtent, 0, i);
                vertices[iR] = new Vector3(c_MeshExtent, 0, i);

                normals[iL] = Vector3.up;
                normals[iR] = Vector3.up;

                float y = i / (float)c_Length;
                uvs[iL] = new Vector2(0, y);
                uvs[iR] = new Vector2(1, y);
            }

            for (int i = 0; i < c_Length; i++)
            {
                int iL0 = i * 2;
                int iR0 = iL0 + 1;
                int iL1 = iL0 + 2;
                int iR1 = iR0 + 2;

                int t = i * 6;
                triangles[t] = iL0;
                triangles[t + 1] = iL1;
                triangles[t + 2] = iR1;
                triangles[t + 3] = iL0;
                triangles[t + 4] = iR1;
                triangles[t + 5] = iR0;
            }

            return new Mesh
            {
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
        }
    }
}