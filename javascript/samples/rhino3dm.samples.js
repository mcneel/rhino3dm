class SampleDoc {
  constructor(model) {
    this.model = model;
    this.breps = [];
    var objecttable = model.objects();
    for(var i=0; i<objecttable.count; i++) {
      var modelobject = objecttable.get(i);
      this.breps.push({"geometry":modelobject.geometry(), "meshes":[], "threejs":null});
    }
  }

  computeBrepMeshes(callback) {
    for(var i=0; i<this.breps.length; i++) {
      var brep = this.breps[i]["geometry"];
      var functionArgs = [brep.encode()];
      var auth = getAuthToken();

      const fetchFunc = (m, index, args, auth) => {
        fetch(m_urlbase + "Geometry/Mesh/CreateFromBrep", {
          "method":"POST",
          "body": JSON.stringify(args),
          "headers": {"Authorization":auth}
        })
        .then(r=>r.json())
        .then(result=>{
          var meshes = result.map(r=>Module.CommonObject.decode(r));
          m.breps[index]["meshes"] = meshes;
          callback(this);
        });
      };
      fetchFunc(this, i, functionArgs, auth);
    }
  }
};


var m_meshes = [];
var m_model = null;
const m_urlbase = "https://compute.rhino3d.com/Rhino/";

function addMesh(mesh) {
  m_meshes.push({"mesh":mesh, "path":null, "three_geometry":null})
}

function getAuthToken() {
  auth = localStorage["compute_auth"];
  if (auth == null) {
    auth = window.prompt("Rhino Accounts auth token");
    if (auth != null && auth.length>20) {
      auth = "Bearer " + auth;
      localStorage.setItem("compute_auth", auth);
    }
  }
  return auth;
}

function brepToMesh(brep) {
  functionArgs = [brep.encode()];
  auth = getAuthToken();

  fetch(m_urlbase + "Geometry/Mesh/CreateFromBrep", {
    "method":"POST",
    "body": JSON.stringify(functionArgs),
    "headers": {"Authorization":auth}
  })
  .then(r=>r.json())
  .then(result=>{
    meshes = result.map(r=>Module.CommonObject.decode(r));

    for( i=0; i<meshes.length; i++ ) {
      addMesh(meshes[i]);
    }
    draw3dMeshes();
  });
}

function runMeshMaker() {
  m_meshes = [];

  sphere = new Module.Sphere([250, 250, 0], 100);
  brep = sphere.toBrep();
  brepToMesh(brep);
  // Don't need the sphere and brep anymore
  sphere.delete();
  brep.delete();
}

function draw2dMeshes(green=0) {
  var ctx=canvas.getContext("2d");
  // Create gradient
  var grd=ctx.createLinearGradient(0,0,0,500);
  grd.addColorStop(0,"slategray");
  grd.addColorStop(1,"black");
  // Fill with gradient
  ctx.fillStyle=grd;
  ctx.fillRect(0,0,500,500);

  ctx.lineWidth = 1;
  ctx.strokeStyle = "rgb(255,"+green+",0)";

  for(meshindex=0; meshindex<m_meshes.length; meshindex++) {
    meshitem = m_meshes[meshindex]
    if( meshitem["path"] == null ) {
      path = new Path2D();
      mesh = meshitem["mesh"];
      verts = mesh.vertices();
      faces = mesh.faces();

      for (i = 0; i < faces.count; i++) {
        face = faces.get(i);
        pts = [verts.get(face[0]), verts.get(face[1]), verts.get(face[2]), verts.get(face[3])];
        path.moveTo(pts[0][0]*10+250, -pts[0][2]*10+250);
        path.lineTo(pts[1][0]*10+250, -pts[1][2]*10+250);
        path.lineTo(pts[2][0]*10+250, -pts[2][2]*10+250);
        path.lineTo(pts[3][0]*10+250, -pts[3][2]*10+250);
      }
      meshitem["path"] = path;
    }
    ctx.stroke(meshitem["path"])
  }
}

function draw3dMeshes() {
  doRotate = false;
  if( m_meshes.length==22 ) {
    doRotate = true;
    for(i=0; i<m_meshes.length; i++) {
      if( m_meshes[i]["three_geometry"]==null )
        doRotate = false;
    }
  }

  for(meshindex=0; meshindex<m_meshes.length; meshindex++) {
    meshitem = m_meshes[meshindex]
    if( meshitem["three_geometry"] == null ) {

      mesh = meshitem["mesh"];
      verts = mesh.vertices();
      faces = mesh.faces();

      var geometry = new THREE.Geometry();
      for (i = 0; i < faces.count; i++) {
        face = faces.get(i);
        pts = [verts.get(face[0]), verts.get(face[1]), verts.get(face[2]), verts.get(face[3])];
        geometry.vertices.push(
          toVec3(pts[0]), toVec3(pts[1]),
          toVec3(pts[1]), toVec3(pts[2]),
          toVec3(pts[2]), toVec3(pts[3]),
          toVec3(pts[3]), toVec3(pts[0])
        );
      }
      var lines = new THREE.LineSegments( geometry, material );
      scene.add( lines );
      meshitem["three_geometry"] = lines;
    }

    if(doRotate) {
      meshitem["three_geometry"].rotation.x += 0.006;
      meshitem["three_geometry"].rotation.y += 0.006;
    }
  }
  renderer.render( scene, camera );
}

function toVec3(pt) {
  return new THREE.Vector3(pt[0], pt[1], pt[2]);
}

function rotateMesh(val) {
  // get center of all m_meshes

  //mesh.rotate(.1, [1,1,0], [250,250,0]);
  draw2dMeshes(val);
}

function getRhinoLogoMeshes(partialResultsCallback=null) {
  m_meshes = [];
  req = new XMLHttpRequest();
  req.open("GET", "https://files.mcneel.com/TEST/Rhino Logo.3dm");
  req.responseType = "arraybuffer";
  req.addEventListener("loadend", loadEnd);
  req.send(null);

  function loadEnd(e) {
    longInt8View = new Uint8Array(req.response);
    m_model = Module.File3dm.fromByteArray(longInt8View);

    var doc = new SampleDoc(m_model);
    doc.computeBrepMeshes(partialResultsCallback);
  }
}

function getapoint() {
  functionArgs = [1,2,3];
  auth = getAuthToken();

  fetch(m_urlbase + "Geometry/Point3d/New", {
    "method":"POST",
    "body": JSON.stringify(functionArgs),
    "headers": {"Authorization":auth}
  })
  .then(r=>r.json())
  .then(result=>{
     alert("wow");
  });
}
