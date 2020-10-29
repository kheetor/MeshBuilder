using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MeshBuilder {

    public enum MeshUVMode {
        uvTransform
    };

    private class SubMesh {
        public List<int> tris;

        public SubMesh() {
            tris = new List<int>();
        }
    }

    private struct EarVert {
        public int i;
        public float l;
        public float angle;

        public EarVert(int i, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5) {
            this.i = i;
            this.l = (p2 - p4).magnitude;
            this.angle = Vector3.Angle(p3 - p2, p3 - p4);
            //this.angle -= Vector3.Angle(p2 - p3, p2 - p1);
            //this.angle -= Vector3.Angle(p4 - p3, p4 - p5);
        }
    }

    public int activeSubmesh = 0;
    public float uvScale = 1f;
    public MeshUVMode uvMode = MeshUVMode.uvTransform;
    public Matrix4x4 uvTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

    private List<SubMesh> subMeshes = new List<SubMesh>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Vector3> verts = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();

    public MeshBuilder() {
    }

    public void Clear() {
        verts.Clear();
        uvs.Clear();
        normals.Clear();
        subMeshes.Clear();
        activeSubmesh = 0;
    }

    public void WriteToMesh(Mesh mesh) {
        mesh.Clear();
        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = subMeshes.Count;
        for (int i = 0; i < subMeshes.Count; i++) {
            mesh.SetTriangles(subMeshes[i].tris.ToArray(), i);
        }

        //mesh.normals = normals.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        /*
        UnwrapParam unw = new UnwrapParam();
        unw.angleError = 0.5f;
        unw.areaError = 0.1f;
        unw.hardAngle = 45f;
        unw.packMargin = 0.02f;
        Unwrapping.GenerateSecondaryUVSet(mesh, unw);

        EditorUtility.SetDirty(mesh);
        */
    }

    private int FindEar(List<Vector2> hole) {
        if (hole.Count == 3)
            return 0;
        List<Vector2> ear = new List<Vector2>();

        List<EarVert> earVerts = new List<EarVert>();
        for (int i = 0; i < hole.Count; i++) {
            earVerts.Add(new EarVert(
                i,
                hole[(i + 2) % hole.Count],
                hole[(i + 1) % hole.Count],
                hole[i],
                hole[(hole.Count + i - 1) % hole.Count],
                hole[(hole.Count + i - 2) % hole.Count]
                ));
        }

        earVerts.Sort((x, y) => x.l.CompareTo(y.l));

        //Debug.Log("best earvert at " + earVerts[0].i.ToString() + " : " + hole[earVerts[0].i].ToString());

        for (int index = 0; index < hole.Count; index++) {

            int i = earVerts[index].i;
            ear.Clear();
            bool valid = true;
            int ip = (i + 1) % hole.Count;
            int im = (hole.Count + i - 1) % hole.Count;

            // angle check
            if (earVerts[index].angle > 179 || earVerts[index].angle < 1)
                continue;

            // Winding check
            float sum = 0f;
            sum += (hole[i].x - hole[im].x) * (hole[i].y + hole[im].y);
            sum += (hole[ip].x - hole[i].x) * (hole[ip].y + hole[i].y);
            sum += (hole[im].x - hole[ip].x) * (hole[im].y + hole[ip].y);
            //Debug.Log(sum.ToString() + " sum for ear " + i.ToString() + ", remaining " + hole.Count.ToString());
            if (sum < 0)
                continue;

            // self-intersect check
            ear.Add(hole[im]);
            ear.Add(hole[i]);
            ear.Add(hole[ip]);

            for (int j = 0; j < hole.Count; j++) {
                if (j != i && j != im && j != ip && Utility.PointInPolyXY(ear, hole[j])) {
                    //Debug.Log(ear[0].ToString() + "-" + ear[1].ToString() + "-" + ear[2].ToString() + " contains " + hole[j]);
                    //Debug.Log("self intersects");
                    valid = false;
                }
            }

            // resulting hole check
            if (Vector3.Angle(hole[ip] - hole[im], hole[ip] - hole[(ip + 1) % hole.Count]) < 5f ||
                Vector3.Angle(hole[im] - hole[ip], hole[im] - hole[(hole.Count + im - 1) % hole.Count]) < 5f) {
                //Debug.Log("resulting hole check fail");
                valid = false;
            }


            if (valid) {
                //Debug.Log("valid " + i.ToString() + " at " + hole[i].ToString() + ", hole count " + hole.Count.ToString());
                ear.Clear();
                return i;
            }
        }

        Debug.LogWarning("Unoptimal poly");
        return 0;
    }

    private SubMesh GetSubMesh(int slot) {
        while (subMeshes.Count < slot + 1) {
            subMeshes.Add(new SubMesh());
        }
        return subMeshes[slot];
    }

    public int NewSubMesh() {
        subMeshes.Add(new SubMesh());
        return subMeshes.Count - 1;
    }

    private void AddVerts(List<Vector3> points) {
        Quaternion rot = Quaternion.AngleAxis(90f, points[2] - points[0]);
        AddVerts(points, rot * (points[1] - points[0]));
    }

    private void AddVerts(List<Vector3> points, Vector3 normal) {
        verts.AddRange(points);
        if (uvMode == MeshUVMode.uvTransform && uvTransform != null) {
            for (int i = 0; i < points.Count; i++) {
                uvs.Add(uvTransform * points[i]);
                normals.Add(normal);
            }
        }
    }

    private void AddVert(Vector3 point, Vector3 normal) {
        verts.Add(point);
        uvs.Add(uvTransform * point);
        normals.Add(normal);
    }

    public void AddQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 n1, Vector3 n2, Vector3 n3, Vector3 n4) {
        normals.Add(n1);
        normals.Add(n2);
        normals.Add(n3);
        normals.Add(n4);
        AddQuad(p1, p2, p3, p4);
    }

    public void AddQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 normal) {
        for (int i = 0; i < 4; i++) {
            normals.Add(normal);
        }
        AddQuad(p1, p2, p3, p4);
    }

    public void AddQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
        int vOffset = verts.Count;
        verts.Add(p1);
        verts.Add(p2);
        verts.Add(p3);
        verts.Add(p4);

        uvs.Add(uvTransform * p1);
        uvs.Add(uvTransform * p2);
        uvs.Add(uvTransform * p3);
        uvs.Add(uvTransform * p4);

        SubMesh sm = GetSubMesh(activeSubmesh);
        sm.tris.Add(vOffset + 0);
        sm.tris.Add(vOffset + 1);
        sm.tris.Add(vOffset + 2);

        sm.tris.Add(vOffset + 0);
        sm.tris.Add(vOffset + 2);
        sm.tris.Add(vOffset + 3);
    }

    public void AddTri(Vector3 p1, Vector3 p2, Vector3 p3) {
        int vOffset = verts.Count;
        verts.Add(p1);
        verts.Add(p2);
        verts.Add(p3);

        uvs.Add(uvTransform * p1);
        uvs.Add(uvTransform * p2);
        uvs.Add(uvTransform * p3);

        SubMesh sm = GetSubMesh(activeSubmesh);
        sm.tris.Add(vOffset + 0);
        sm.tris.Add(vOffset + 1);
        sm.tris.Add(vOffset + 2);
    }

    /// <summary>
    /// Move poly from one submesh to another based on bounds
    /// </summary>
    /// <param name="submeshTo"></param>
    /// <param name="bounds">Use empty list to move all</param>
    /// <param name="submeshFrom">Use empty list to move from all submeshes</param>
    public void PolySubmeshChange(int submeshTo, List<Bounds> bounds, int[] submeshFrom) {
        SubMesh smt = GetSubMesh(submeshTo);

        for (int sm = 0; sm < subMeshes.Count; sm++) {
            if (sm == submeshTo || (submeshFrom.Length >= 0 && !Array.Exists(submeshFrom, element => element == sm))) {
                continue;
            }
            SubMesh smf = GetSubMesh(sm);
            for (int i = 0; i < smf.tris.Count; i += 3) {
                if (bounds.Count > 0) {
                    for (int b = 0; b < bounds.Count; b++) {
                        if (bounds[b].Contains(verts[smf.tris[i]]) && bounds[b].Contains(verts[smf.tris[i + 1]]) && bounds[b].Contains(verts[smf.tris[i + 2]])) {
                            smt.tris.Add(smf.tris[i]);
                            smt.tris.Add(smf.tris[i + 1]);
                            smt.tris.Add(smf.tris[i + 2]);
                            smf.tris.RemoveAt(i + 2);
                            smf.tris.RemoveAt(i + 1);
                            smf.tris.RemoveAt(i);
                            i -= 3;
                        }
                    }
                }
                else {
                    smt.tris.AddRange(smf.tris);
                    smf.tris.Clear();
                }
            }
        }
    }

    public bool TryFindContainsTri(Vector3 point, Matrix4x4 ps, out int index) {
        List<Vector2> tri = new List<Vector2>();
        point = ps * point;
        //Debug.Log("Checking point " + point);
        for (int i = 0; i < subMeshes[activeSubmesh].tris.Count; i += 3) {
            tri.Clear();
            tri.Add(ps*verts[subMeshes[activeSubmesh].tris[i]]);
            tri.Add(ps*verts[subMeshes[activeSubmesh].tris[i + 1]]);
            tri.Add(ps*verts[subMeshes[activeSubmesh].tris[i + 2]]);
            //Debug.Log("Checking tri");
            //Debug.Log(tri[0]);
            //Debug.Log(tri[1]);
            //Debug.Log(tri[2]);
            if (Utility.PointInPolyXY(tri, point)) {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    public bool TriFind(int[] include, int[] exclude, out int index) {
        List<int> tri = new List<int>();
        for(int t = 0; t < subMeshes[activeSubmesh].tris.Count; t += 3) {
            bool all = true;
            tri.Clear();
            tri.Add(subMeshes[activeSubmesh].tris[t]);
            tri.Add(subMeshes[activeSubmesh].tris[t+1]);
            tri.Add(subMeshes[activeSubmesh].tris[t+2]);
            for(int i = 0; i < include.Length; i++) {
                if (!tri.Contains(i)) {
                    all = false;
                    break;
                }
            }
            if (all) {
                for(int i = 0; i < exclude.Length; i++) {
                    if (tri.Contains(exclude[i])) {
                        all = false;
                        break;
                    }
                }
                if (all) {
                    index = t;
                    return true;
                }
            }
        }
        index = -1;
        return false;
    }

    public bool VertFindOppositeAB(int a, int b, out int[] indexInfo) {
        for (int t = 0; t < subMeshes[activeSubmesh].tris.Count; t += 3) {
            if (subMeshes[activeSubmesh].tris[t] == a && subMeshes[activeSubmesh].tris[t + 2] == b) {
                indexInfo = new int[] { subMeshes[activeSubmesh].tris[t + 1], t, 1};
                return true;
            }
            else if (subMeshes[activeSubmesh].tris[t + 1] == b && subMeshes[activeSubmesh].tris[t + 2] == a) {
                indexInfo = new int[] { subMeshes[activeSubmesh].tris[t], t, 0};
                return true;
            }
            else if (subMeshes[activeSubmesh].tris[t] == b && subMeshes[activeSubmesh].tris[t + 1] == a) {
                indexInfo = new int[] { subMeshes[activeSubmesh].tris[t + 2], t, 2};
                return true;
            }
        }
        indexInfo = new int[0];
        return false;
    }

    public void TriSplitByPoint(Vector3 point, Vector3 normal) {
        // Find triangle that contains point
        int ti;
        Matrix4x4 ps = Matrix4x4.TRS(point, Quaternion.identity, Vector3.one);
        if (TryFindContainsTri(point, ps, out ti)){
            //indices of the new triangles
            int a = subMeshes[activeSubmesh].tris[ti];
            int b = subMeshes[activeSubmesh].tris[ti+1];
            int c = subMeshes[activeSubmesh].tris[ti+2];

            //add the point to mesh verts
            AddVert(point, normal);
            int d = verts.Count - 1;

            //remove the poly that contains the point
            subMeshes[activeSubmesh].tris.RemoveRange(ti, 3);

            //add 3 new triangles
            int[] newTris = new int[]{
                a, b, d,
                b, c, d,
                c, a, d
                };
            subMeshes[activeSubmesh].tris.AddRange(newTris);
        }
        else {
        }
    }

    public void RotUntilDelaulay() {
        SubMesh sm = GetSubMesh(activeSubmesh);
        bool unoptimal = true;
        int iter = 0;
        while (unoptimal) {
            unoptimal = false;
            for(int i = 0; i < sm.tris.Count; i+=3) {
                int[] d;
                if(VertFindOppositeAB(sm.tris[i], sm.tris[i+1], out d)) {
                    if (FlipQuadTriangulation(i, d[1], sm.tris[i], sm.tris[i + 1], sm.tris[i + 2], d[0])) {
                        unoptimal = true;
                        break;
                    }
                }

            }
            iter++;
            if(iter > 10000) {
                Debug.LogError("Delaunay fail");
                break;
            }
        }
        Debug.Log("Delaunay done in " + iter + " cycles");
    }

    public bool FlipQuadTriangulation(int abcIndex, int dbaIndex, int a, int b, int c, int d) {
        float curAngle = Mathf.Max(Vector3.Angle(verts[a] - verts[c], verts[b] - verts[c]), Vector3.Angle(verts[b] - verts[d], verts[a] - verts[d]));
        float newAngle = Mathf.Max(Vector3.Angle(verts[d] - verts[a], verts[c] - verts[a]), Vector3.Angle(verts[d] - verts[b], verts[c] - verts[b]));
        if (curAngle > newAngle) {
            subMeshes[activeSubmesh].tris.RemoveRange(abcIndex, 3);
            subMeshes[activeSubmesh].tris.RemoveRange(dbaIndex, 3);
            int[] newTris = new int[] {
                a,d,c,
                b,c,d
            };
            subMeshes[activeSubmesh].tris.AddRange(newTris);
            return true;
        }
        return false;
    }

    public void PolyAdd(List<Vector3> points, Vector3 normal, bool flip = false) {
        // for finding Ngon ears and using point in poly formula we are
        // constructing matrix which translates points from mesh space to poly space
        Matrix4x4 ps = Matrix4x4.TRS(points[0], Quaternion.LookRotation(normal, points[1] - points[0]), Vector3.one);
        //Debug.Log("adding poly ");
        for (int i = 0; i < points.Count; i++) {
            //Debug.Log(points[i]);
        }

        // add points to mesh verts
        int vOffset = verts.Count;
        AddVerts(points, normal);
        SubMesh sm = GetSubMesh(activeSubmesh);

        // hole is being analyzed in 2D polygon space
        // this doesn't affect 'points' the actual vertex positions
        List<Vector2> hole = new List<Vector2>();
        for(int i = 0; i < points.Count; i++) {
            hole.Add(ps*points[i]);
        }
        List<int> holeIndices = new List<int>();
        for (int i = 0; i < hole.Count; i++) {
            holeIndices.Add(i);
        }

        while (hole.Count > 2) {
            int i = FindEar(hole);

            if (flip) {
                sm.tris.Add(vOffset + holeIndices[(i + 1) % holeIndices.Count]);
                sm.tris.Add(vOffset + holeIndices[(i) % holeIndices.Count]);
                sm.tris.Add(vOffset + holeIndices[(holeIndices.Count + i - 1) % holeIndices.Count]);
            }
            else {
                sm.tris.Add(vOffset + holeIndices[(holeIndices.Count + i - 1) % holeIndices.Count]);
                sm.tris.Add(vOffset + holeIndices[(i) % holeIndices.Count]);
                sm.tris.Add(vOffset + holeIndices[(i + 1) % holeIndices.Count]);
            }

            hole.RemoveAt(i);
            holeIndices.RemoveAt(i);
        }
    }
}
