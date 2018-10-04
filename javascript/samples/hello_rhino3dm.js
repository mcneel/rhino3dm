var m_meshes = [];

function brepToMesh(brep) {
  functionArgs = [brep.encode()];
  auth = localStorage["compute_auth"];
  if (auth == null) {
    auth = window.prompt("Rhino Accounts auth token");
    if (auth != null && auth.length>20) {
      auth = "Bearer " + auth;
      localStorage.setItem("compute_auth", auth);
    }
  }

  fetch("http://staging.compute.rhino3d.com/Rhino/Geometry/Mesh/CreateFromBrep", {
    "method":"POST",
    "body": JSON.stringify(functionArgs),
    "headers": {"Authorization":auth}
  }).then(r=>r.json())
  .then(result=>{
    meshes = result.map(r=>Module.CommonObject.decode(r));

    for( i=0; i<meshes.length; i++ ) {
      m_meshes.push(meshes[i])
    }
    drawMesh();
  });
}

function runMeshMaker() {
  m_meshes = [];

  sphere = new Module.Sphere([250, 250, 0], 100);
  brep = sphere.toBrep();
  functionArgs = [brep.encode()];
  // Don't need the sphere and brep anymore
  sphere.delete();
  brep.delete();

  postdata = JSON.stringify(functionArgs);
  req = new XMLHttpRequest();
  url = "http://staging.compute.rhino3d.com/Rhino/Geometry/Mesh/CreateFromBrep";
  req.open("POST", url);

  auth = localStorage["compute_auth"];
  if (auth == null) {
    auth = window.prompt("Rhino Accounts auth token");
    if (auth != null && auth.length>20) {
      auth = "Bearer " + auth;
      localStorage.setItem("compute_auth", auth);
    }
  }
  req.setRequestHeader("Authorization", auth);
  req.addEventListener("loadend", loadEnd);
  req.send(postdata);

  function loadEnd(e) {
    response = JSON.parse(req.responseText);
    mesh = Module.CommonObject.decode(response[0]);
    m_meshes.push(mesh);
    drawMesh();
  }
}

function drawMesh() {
  var ctx=canvas.getContext("2d");
  // Create gradient
  var grd=ctx.createLinearGradient(0,0,0,500);
  grd.addColorStop(0,"slategray");
  grd.addColorStop(1,"black");
  // Fill with gradient
  ctx.fillStyle=grd;
  ctx.fillRect(0,0,500,500);

  ctx.lineWidth = 1;
  ctx.strokeStyle = 'red';

  for(meshindex=0; meshindex<m_meshes.length; meshindex++) {
    mesh = m_meshes[meshindex];
    verts = mesh.vertices();
    faces = mesh.faces();

    for (i = 0; i < faces.count; i++) {
      ctx.beginPath();
      face = faces.get(i);
      pts = [verts.get(face[0]), verts.get(face[1]), verts.get(face[2]), verts.get(face[3])];
      ctx.moveTo(pts[0][0]*10+250, -pts[0][2]*10+250);
      ctx.lineTo(pts[1][0]*10+250, -pts[1][2]*10+250);
      ctx.lineTo(pts[2][0]*10+250, -pts[2][2]*10+250);
      ctx.lineTo(pts[3][0]*10+250, -pts[3][2]*10+250);
      ctx.stroke();
    }
  }
}


function rotateMesh() {
  // get center of all m_meshes

  //mesh.rotate(.1, [1,1,0], [250,250,0]);
  drawMesh();
}

function getRhinoLogoMeshes() {
  m_meshes = [];
  req = new XMLHttpRequest();
  req.open("GET", "https://files.mcneel.com/TEST/Rhino Logo.3dm");
  req.responseType = "arraybuffer";
  req.addEventListener("loadend", loadEnd);
  req.send(null);

  function loadEnd(e) {
    longInt8View = new Uint8Array(req.response);
    model = Module.File3dm.fromByteArray(longInt8View);

    objecttable = model.objects();
    for(i=0; i<objecttable.count; i++) {
      brep = objecttable.get(i);
      brepToMesh(brep);
    }
  }
}
