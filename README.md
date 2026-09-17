# NURBS Sharp Samples

Examples showing how to construct, tessellate, and intersect NURBS geometry with
[NURBS Sharp](https://github.com/nyarurato/NurbsSharp).

## TestViewer

The OpenTK viewer demonstrates the following NURBS Sharp operations:

- Curve-Surface intersection
- Curve-Curve intersection and global interpolation
- Surface-Plane intersection (fast and robust algorithms)
- Surface-Surface intersection
- Ray-Mesh intersection with a BVH

All NURBS construction and query code is collected in
[`TestViewer/NurbsExamples.cs`](TestViewer/NurbsExamples.cs). Input, camera, and
OpenGL rendering code is kept separately under [`TestViewer/Viewer`](TestViewer/Viewer).

Run the viewer with:

```shell
dotnet run --project TestViewer/TestViewer.csproj
```

### Keys

| Key | Action |
| --- | --- |
| `0` | Reset to the Curve-Surface example |
| `1` / `2` / `3` | All / solid / wireframe rendering |
| `4` | Toggle rotation |
| `5` | Toggle intersections |
| `6` | Toggle curves |
| `7` / `8` | Increase / decrease wireframe width |
| `9` | Print the current status |
| `Shift+3` | Surface-Plane example |
| `Shift+4` | Surface-Surface example |
| `Shift+5` | Curve-Curve example |
| `Shift+6` | Toggle interpolation points |
| `Shift+8` | Ray-Mesh example |

![OpenTK viewer](https://github.com/user-attachments/assets/75b8bbd9-ebb5-4639-82af-b3f5637c79e5)

## SurfDemoBlazor

An interactive Blazor WebAssembly surface editor using NURBS Sharp:

https://nyarurato.github.io/NurbsSharpSample/

![Blazor surface editor](https://github.com/user-attachments/assets/c9ae0303-a3c5-4191-bf7e-d0e37cc02998)
