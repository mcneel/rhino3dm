rhino3dm.py
===========

.. important::

   **Coming from rhino3dm 8.x?** 9.2 is generated from RhinoCommon, and much of the API changed.
   Read :doc:`migrating` first, and look up any 8.x name in :doc:`migrating-members`.

rhino3dm is a Python library for reading, writing and working with Rhino's .3dm files and the
geometry in them, without Rhino. Its API is RhinoCommon's: the classes, members and names are
generated from RhinoCommon, and the documentation here is RhinoCommon's own.

.. code-block:: python

   import rhino3dm

   model = rhino3dm.File3dm()
   circle = rhino3dm.Circle(rhino3dm.Point3d(0, 0, 0), 5.0)
   model.Objects.AddCircle(circle)
   model.Write("circle.3dm", 8)

Install with ``pip install rhino3dm``.

.. toctree::
   :maxdepth: 1

   migrating
   migrating-members
   reference/index
