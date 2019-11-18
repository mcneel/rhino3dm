# Samples

## rhino3dm

### [simple viewer](https://mcneel.github.io/rhino3dm/javascript/samples/viewer/01_basic/)

[![Screenshot_2019-11-15 Hello Mesh ](https://user-images.githubusercontent.com/1014562/68938619-05feca80-079f-11ea-8d15-354c3e82a261.png)](https://mcneel.github.io/rhino3dm/javascript/samples/viewer/01_basic/)

Loads a .3dm file and renders the geometry with [Three.js](https://threejs.org)

[source](viewer/01_basic)

### [advanced viewer](https://mcneel.github.io/rhino3dm/javascript/samples/viewer/02_advanced/)

[![Screenshot_2019-11-15 PBR Material Example](https://user-images.githubusercontent.com/1014562/68938708-30e91e80-079f-11ea-9ad8-0de304d87327.jpg)](https://mcneel.github.io/rhino3dm/javascript/samples/viewer/02_advanced/)

Loads a .3dm file and renders the geometry and material with [Three.js](https://threejs.org)

[source](viewer/02_advanced)

### [sketch 2D](https://mcneel.github.io/rhino3dm/javascript/samples/sketch2d/)

[![Screenshot_2019-11-15 2D NURBS curves sketcher](https://user-images.githubusercontent.com/1014562/68938820-6ee64280-079f-11ea-84ef-0c102bc54a70.png)](https://mcneel.github.io/rhino3dm/javascript/samples/sketch2d/)

Sketch NURBS curves on a canvas. Also shows how to download the resulting 3dm file.

[source](sketch2d)

## rhino3dm + Compute

### [Use compute to get isocurves and meshes](https://mcneel.github.io/rhino3dm/javascript/samples/compute/brep_isocurves/)

[![image](https://user-images.githubusercontent.com/1014562/68939044-f2a02f00-079f-11ea-8e39-d582fa67e409.png)](https://mcneel.github.io/rhino3dm/javascript/samples/compute/brep_isocurves/)

Loads a 3dm file containing the Rhino logo as a brep, fetches the render mesh and wireframe and loads them into a [three.js](https://threejs.org) scene.

[source](compute/brep_isocurves)

### [boolean.html](https://mcneel.github.io/rhino3dm/javascript/samples/compute/brep_boolean/)

[![image](https://user-images.githubusercontent.com/1014562/68939173-3b57e800-07a0-11ea-9e3a-46a9e4a82f40.png)](https://mcneel.github.io/rhino3dm/javascript/samples/compute/brep_boolean/)

As rhinologo.html but also loads another set of breps and calculates the boolean difference between the Rhino logo and these.

[source](compute/brep_boolean)

### [ar.html](https://mcneel.github.io/rhino3dm/javascript/samples/viewer/03_ar/)

Print the [hiro marker](https://jeromeetienne.github.io/AR.js/data/images/HIRO.jpg) (or simply open it on your computer screen) then open this sample on a mobile phone and point the camera at the marker.

[source](viewer/03_ar)

### [clash_detection](https://mcneel.github.io/rhino3dm/javascript/samples/compute/clash_detection/)

[![image](https://user-images.githubusercontent.com/1014562/68939429-e7013800-07a0-11ea-91cb-7b58a6e97c3b.png)](https://mcneel.github.io/rhino3dm/javascript/samples/compute/clash_detection/)

Performs clash detection between a bunch of randomly positioned spheres and one main sphere.

[source](compute/clash_detection)

## Solving Grasshopper definitions on Compute

### [DelaunayMesh](https://mcneel.github.io/rhino3dm/javascript/samples/compute/RESTHopper/DelaunayMesh/)

[![image](https://user-images.githubusercontent.com/1014562/68939360-bc16e400-07a0-11ea-80c1-f88aa7c5c0ec.png)](https://mcneel.github.io/rhino3dm/javascript/samples/compute/RESTHopper/DelaunayMesh/)

Generates random points and meshes them with the Delaunay component in Grasshopper. 

[source](compute/RESTHopper/DelaunayMesh)

### [Extrusions](https://mcneel.github.io/rhino3dm/javascript/samples/compute/RESTHopper/Extrusions/)

[![image](https://user-images.githubusercontent.com/1014562/68939312-912c9000-07a0-11ea-9261-ed2e025bfa45.png)](https://mcneel.github.io/rhino3dm/javascript/samples/compute/RESTHopper/Extrusions/)

Passes the input from three sliders to a Grasshopper defintion which offsets faces from a mesh and smooths it with SubD components.

[source](compute/RESTHopper/Extrusions)
