resultcurves = []

function doit() {
  auth = localStorage["compute_auth"];
  if (auth == null) {
    auth = window.prompt("Rhino Accounts auth token");
    if (auth != null && auth.length>20) {
      auth = "Bearer " + auth;
      localStorage.setItem("compute_auth", auth);
    }
  }
  // So dumb. This needs to be fixed right away
  plane = {"Origin":{"X":0,"Y":0,"Z":0},
          "OriginX":0, "OriginY":0, "OriginZ":0,
          "XAxis":{"X":1,"Y":0,"Z":0},
          "YAxis":{"X":0,"Y":1,"Z":0},
          "ZAxis":{"X":0,"Y":0,"Z":1},
          "Normal":{"X":0,"Y":0,"Z":1},
          "IsValid":true};
  args = ["Rhino","Arial",80,0,true, plane, 0, 0.01];
  fetch("http://staging.compute.rhino3d.com/Rhino/Geometry/Curve/CreateTextOutlines", {
    "method":"POST",
    "body": JSON.stringify(args),
    "headers": {"Authorization":auth}
  })
  .then(r=>r.json())
  .then(result=> {
    curves = result.map(r=>Module.CommonObject.decode(r));
    for( i=0; i<curves.length; i++) {
      topl_args = [curves[i].encode(),0.1,0.1,0.1,1000];
      fetch("http://staging.compute.rhino3d.com/Rhino/Geometry/Curve/ToPolyline", {
        "method":"POST",
        "body": JSON.stringify(topl_args),
        "headers": {"Authorization":auth}
      })
      .then(r=>r.json())
      .then(result => {
        polylinecurve = Module.CommonObject.decode(result);
        resultcurves.push(polylinecurve);
        drawPolyline(polylinecurve);
      });
    }
  });
}

function drawPolyline(pl) {
  ctx = Module.canvas.getContext('2d');
  ctx.lineWidth = 1;
  ctx.strokeStyle = 'red';

  ctx.beginPath();

  for (i = 0; i < pl.pointCount; i++) {
    pt = pl.point(i);
    x = pt[0];
    y = -pt[1]+125;
    if(i==0)
      ctx.moveTo(x,y);
    else
      ctx.lineTo(x,y);
  }
  ctx.stroke();
}

doit();
