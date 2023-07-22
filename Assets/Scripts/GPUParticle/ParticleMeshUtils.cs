using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EdwinTools.Rendering {
    public static class ParticleMeshUtils {
        public static Mesh GeneratePointMesh(int width, int height) {
            var vertices = new Vector3[width * height];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[vertices.Length];

            var meshIndex = 0;
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    vertices[meshIndex] = Vector3.zero;

                    var u = (float)x / width;
                    var v = (float)y / height;
                    uv[meshIndex] = new Vector2(u, v);

                    triangles[meshIndex] = meshIndex;
                    meshIndex++;
                }
            }

            var mesh = new Mesh();
            mesh.hideFlags = HideFlags.DontSave;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.SetIndices(triangles, MeshTopology.Points, 0);
            mesh.Optimize();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 200f);
            return mesh;
        }
    }
}