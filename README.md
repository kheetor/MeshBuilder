# MeshBuilder
Generating meshes by code in Unity can be a bit tedious when you have to track the vertice index, uvs and normals ever time. MeshBuilder aims to help with that, making generation code more efficient and easier to read.

Features:
- One line add for triangles, quads and n-gons
- Submesh support
- Auto normals (overridable for flat / smoothshading)
- Auto UV projection updateable with Matrix4x4
- Outputs a native mesh

Planned features:
- grid array add mode
- automerging vertices if position/uv/normal matches
- custom lightmap uv generation
