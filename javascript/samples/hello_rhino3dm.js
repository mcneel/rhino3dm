var mesh;

function runMeshMaker() {
  // First test of rhinocommon.js
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

  verts = mesh.vertices();
  faces = mesh.faces();

  ctx.beginPath();
  for (i = 0; i < faces.count; i++) {
    face = faces.get(i);
    pts = [verts.get(face[0]), verts.get(face[1]), verts.get(face[2]), verts.get(face[3])];
    ctx.moveTo(pts[0][0], pts[0][1]);
    ctx.lineTo(pts[1][0], pts[1][1]);
    ctx.lineTo(pts[2][0], pts[2][1]);
    ctx.lineTo(pts[3][0], pts[3][1]);
  }
  ctx.stroke();
}


function rotateMesh() {
  mesh.rotate(.1, [1,1,0], [250,250,0]);
  drawMesh();
}
