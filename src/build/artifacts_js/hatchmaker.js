function getWires(nurbsCurve, drawLinesCallback) {
  auth = localStorage["compute_auth"];
  if (auth == null) {
    auth = window.prompt("Rhino Accounts auth token");
    if (auth != null && auth.length>20) {
      auth = "Bearer " + auth;
      localStorage.setItem("compute_auth", auth);
    }
  }

  functionArgs = [nurbsCurve.encode()];

  urlbase = "http://staging.compute.rhino3d.com/Rhino/";

  fetch(urlbase + "Geometry/Brep/CreatePlanarBreps", {
    "method":"POST",
    "body": JSON.stringify(functionArgs),
    "headers": {"Authorization":auth}
  }).then(r=>r.json())
  .then(result=>{
    breps = result.map(r=>Module.CommonObject.decode(r));

    for( i=0; i<breps.length; i++ ) {
      bbox = breps[i].getBoundingBox();
      bmin = {"X":bbox.min[0], "Y":bbox.min[1], "Z":bbox.min[2]};
      bmax = {"X":bbox.max[0], "Y":bbox.max[1], "Z":bbox.max[2]};
      args = [breps[i].encode(), bmin, bmax, 4]
      breps[i].delete();
      fetch(urlbase + "Geometry/Brep/CreateContourCurves", {
        "method":"POST",
        "body": JSON.stringify(args),
        "headers": {"Authorization":auth}
      }).then(r=>r.json())
      .then(result=>{
        curves = result.map(r=>Module.CommonObject.decode(r));
        l = curves.map(c=>[c.pointAtStart, c.pointAtEnd]);
        curves.map(c=>c.delete());
        drawLinesCallback(l);
      });
    }
  });
}

function drawLines(lines) {
  ctx = Module.canvas.getContext('2d');
  ctx.lineWidth = 1;
  ctx.strokeStyle = 'red';

  ctx.beginPath();
  for (i = 0; i < lines.length; i++) {
    from = lines[i][0];
    to = lines[i][1];
    ctx.moveTo(from[0], from[1]);
    ctx.lineTo(to[0], to[1]);
  }
  ctx.stroke();
}

circle = new Module.Circle([50, 50, 0], 100);
ctx = Module.canvas.getContext('2d');
ctx.lineWidth = 1;
ctx.strokeStyle = 'red';
ctx.beginPath();
ctx.arc(circle.plane.origin[0],circle.plane.origin[1],circle.radius, 0, 2*Math.PI);
ctx.stroke();
nc = circle.toNurbsCurve();
getWires(nc, drawLines);
circle.delete();
nc.delete();
