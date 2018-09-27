from rhino_geometry import *
import Tkinter
import json
import urllib2

center = Point3d(250, 250, 0)
sphere = Sphere(center, 100)
brep = sphere.ToBrep()
functionArgs = [brep.Encode()]

auth_token = ""
top = Tkinter.Tk()
def onClickOk():
    global auth_token
    auth_token = auth.get()
    top.destroy()
Tkinter.Label(top, text="Rhino Accounts auth token").grid(row=0, column=0)
auth = Tkinter.Entry(top)
auth.grid(row=0, column=1)
Tkinter.Button(top, text='Ok', command=onClickOk).grid(row=1, column=1)
top.mainloop()

url = "http://staging.compute.rhino3d.com/Rhino/Geometry/Mesh/CreateFromBrep"
req = urllib2.Request(url)
req.add_header('Content-Type', 'application/json')
req.add_header('Authorization', 'Bearer ' + auth_token)

response = urllib2.urlopen(req, json.dumps(functionArgs))
d = json.loads(response.read())[0]
mesh = CommonObject.Decode(d)

top = Tkinter.Tk()

w = Tkinter.Canvas(top, width=500, height=500)
w.pack()

verts = mesh.Vertices
faces = mesh.Faces
for i in range(faces.Count):
    face = faces.Get(i)
    pts = [verts.Get(face[0]), verts.Get(face[1]), verts.Get(face[2]), verts.Get(face[3])]
    w.create_line(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y)
    w.create_line(pts[1].X, pts[1].Y, pts[2].X, pts[2].Y)
    w.create_line(pts[2].X, pts[2].Y, pts[3].X, pts[3].Y)
    w.create_line(pts[3].X, pts[3].Y, pts[0].X, pts[0].Y)

top.mainloop()