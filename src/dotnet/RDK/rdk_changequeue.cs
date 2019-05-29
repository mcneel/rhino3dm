#if RHINO_SDK

using System;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Render.ChangeQueue
{
	/// <summary>
	/// ChangeQueue DisplayRenderSettings
	/// </summary>
	public class DisplayRenderSettings
	{
		private readonly IntPtr m_ptr;

		/// <summary>
		/// Construct a ChangeQueue DisplayRenderSettings
		/// </summary>
		/// <param name="pSettings"></param>
		internal DisplayRenderSettings(IntPtr pSettings)
		{
			m_ptr = pSettings;
		}

		/// <summary>
		/// True if backfaces should be culled
		/// </summary>
		public bool CullBackFaces
		{
			get
			{
				//[Giulio, 2015 07 27]
				//This section is commented out in \src4\DotNetSDK\rhinocommon\c_rdk\rdk_changequeue.cpp.
				//return UnsafeNativeMethods.ChangeQueue_DisplayRenderSettings_CullBackFaces(m_ptr);

				throw new NotImplementedException("This property is not implemented.");
			}
		}

		/// <summary>
		/// True if flat shading is forced
		/// </summary>
		public bool ForceFlatShading
		{
			get
			{
				//[Giulio, 2015 07 27]
				//This section is commented out in \src4\DotNetSDK\rhinocommon\c_rdk\rdk_changequeue.cpp.
				//return UnsafeNativeMethods.ChangeQueue_DisplayRenderSettings_ForceFlatShading(m_ptr);

				throw new NotImplementedException("This property is not implemented.");
			}
		}

		/// <summary>
		/// True if scene lighting is enabled
		/// </summary>
		public bool SceneLightingOn
		{
			get
			{
				//[Giulio, 2015 07 27]
				//This section is commented out in \src4\DotNetSDK\rhinocommon\c_rdk\rdk_changequeue.cpp.
				//return UnsafeNativeMethods.ChangeQueue_DisplayRenderSettings_SceneLightingOn(m_ptr);

				throw new NotImplementedException("This property is not implemented.");
			}
		}
	}
	/// <summary>
	/// ChangeQueue environment
	/// </summary>
	public class Environment
	{
		/// <summary>
		/// Fillmode for background
		/// </summary>
		[CLSCompliant(false)]
		public enum FrameBufferFillMode : uint
		{
			/// <summary>
			/// None set
			/// </summary>
			None = 0,
			/// <summary>
			/// Use default color
			/// </summary>
			DefaultColor = 1,
			/// <summary>
			/// Use specified solid color
			/// </summary>
			SolidColor,
			/// <summary>
			/// Use 2-color gradient
			/// </summary>
			Gradient2Color,
			/// <summary>
			/// Use 4-color gradient (colors are specified by corners)
			/// </summary>
			Gradient4Color,
			/// <summary>
			/// Use bitmap
			/// </summary>
			Bitmap,
			/// <summary>
			/// Use whatever renderer chooses
			/// </summary>
			Renderer,
			/// <summary>
			/// Transparent background
			/// </summary>
			Transparent,
			/// <summary>
			/// Use 32bit color @todo verify what this means
			/// </summary>
			Force32Bit = 0xffffffff
		};

		private readonly IntPtr m_ptr;
		/// <summary>
		/// Construct a ChangeQueue Environment
		/// </summary>
		/// <param name="pEnv"></param>
		internal Environment(IntPtr pEnv)
		{
			m_ptr = pEnv;
		}
#if env_stuff

		/// <summary>
		/// Get <c>FrameBufferFillMode</c> for environment background
		/// </summary>
		[CLSCompliant(false)]
		public FrameBufferFillMode FillMode
		{
			get
			{
				var rc = UnsafeNativeMethods.ChangeQueue_Environment_Fillmode(m_ptr);
				var fm = (FrameBufferFillMode)rc;
				return fm;
			}
		}

		/// <summary>
		/// True if environment uses custom environment for reflection
		/// </summary>
		public bool UsesCustomReflectionEnvironment
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Environment_GetReflectionCustomEnvironmentOn(m_ptr);
			}
		}

		/// <summary>
		/// True if environment uses custom environment for skylight
		/// </summary>
		public bool UsesCustomSkylightEnvironment
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Environment_GetSkyCustomEnvironmentOn(m_ptr);
			}
		}

		/// <summary>
		/// True if document sun is active
		/// </summary>
		public bool SunActive
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Environment_GetDocSunActive(m_ptr);
			}
		}

		/// <summary>
		/// Get the sun for this environment
		/// </summary>
		public Geometry.Light Sun
		{
			get
			{
				var sunptr = UnsafeNativeMethods.ChangeQueue_Environment_GetDocSun(m_ptr);
				Geometry.Light sun = null;
				if (sunptr != IntPtr.Zero)
				{
					sun = new Geometry.Light(sunptr, null);
					sun.DoNotDestructOnDispose();
				}

				return sun;
			}
		}

		/// <summary>
		/// Get the top color
		/// </summary>
		public Color4f Color1
		{
			get
			{
				var c = new Color4f();

				if (!UnsafeNativeMethods.ChangeQueue_Environment_GetColor(m_ptr, UnsafeNativeMethods.CqEnvironmentColor.One, ref c))
				{
					c = Color4f.Empty;
				}
				return c;
			}
		}

		/// <summary>
		/// Get the second color for gradient background
		/// </summary>
		public Color4f Color2
		{
			get
			{
				var c = new Color4f();

				if (!UnsafeNativeMethods.ChangeQueue_Environment_GetColor(m_ptr, UnsafeNativeMethods.CqEnvironmentColor.Two, ref c))
				{
					c = Color4f.Empty;
				}
				return c;
			}
		}

		/// <summary>
		/// Get the third color for gradient background
		/// </summary>
		public Color4f Color3
		{
			get
			{
				var c = new Color4f();

				if (!UnsafeNativeMethods.ChangeQueue_Environment_GetColor(m_ptr, UnsafeNativeMethods.CqEnvironmentColor.Three, ref c))
				{
					c = Color4f.Empty;
				}
				return c;
			}
		}

		/// <summary>
		/// Get the third color for gradient background
		/// </summary>
		public Color4f Color4
		{
			get
			{
				var c = new Color4f();

				if (!UnsafeNativeMethods.ChangeQueue_Environment_GetColor(m_ptr, UnsafeNativeMethods.CqEnvironmentColor.Four, ref c))
				{
					c = Color4f.Empty;
				}
				return c;
			}
		}

		/// <summary>
		/// Get texture used as wallpaper
		/// </summary>
		public RenderTexture DisplayBackground
		{
			get
			{
				var texptr = UnsafeNativeMethods.ChangeQueue_Environment_DisplayBackground(m_ptr);
				RenderTexture display_background = null;
				if (texptr != IntPtr.Zero)
				{
					display_background = new NativeRenderTexture(texptr);
				}

				return display_background;
			}
		}

		/// <summary>
		/// Get current render environment.
		/// </summary>
		public RenderEnvironment CurrentEnvironment
		{
			get
			{
				var p_render_content = UnsafeNativeMethods.ChangeQueue_Environment_CurrentEnvironment(m_ptr);
				RenderEnvironment current_environment = null;
				if (p_render_content != IntPtr.Zero)
				{
					current_environment = new NativeRenderEnvironment(p_render_content);
				}

				return current_environment;
			}
		}

		/// <summary>
		/// Get current render settings.
		/// </summary>
		public RenderSettings RenderSettings
		{
			get
			{
				var settings_ptr = UnsafeNativeMethods.ChangeQueue_Environment_RenderSettings(m_ptr);
				RenderSettings render_settings = null;
				if (settings_ptr != IntPtr.Zero)
				{
					render_settings = new RenderSettings(settings_ptr);
				}

				return render_settings;
			}
		}

		/// <summary>
		/// Textual representation of environment
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("ChangeQueue.Environment: {0},{1},{2},{3} {4},{5},{6},{7} {8},{9},{10},{11} {12},{13},{14},{15}, mode: {16}, custom skylight: {17}, custom reflection: {18}", Color1.R, Color1.G, Color1.B, Color1.A,
				Color1.R, Color1.G, Color1.B, Color1.A,
				Color1.R, Color1.G, Color1.B, Color1.A,
				Color1.R, Color1.G, Color1.B, Color1.A, FillMode, UsesCustomSkylightEnvironment, UsesCustomReflectionEnvironment);
		}
#endif // env_stuff
	}
	/// <summary>
	/// ChangeQueue skylight
	/// </summary>
	public class Skylight
	{
		private readonly IntPtr m_ptr;
		/// <summary>
		/// Construct a ChangeQueue Skylight
		/// </summary>
		/// <param name="pSkyLight"></param>
		internal Skylight(IntPtr pSkyLight)
		{
			m_ptr = pSkyLight;
		}

		/// <summary>
		/// Return true if skylight is enabled
		/// </summary>
		public bool Enabled
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Skylight_GetOn(m_ptr);
			}
		}

		/// <summary>
		/// Return true if skylight uses custom environment
		/// </summary>
		public bool UsesCustomEnvironment
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Skylight_GetCustomEnvironmentOn(m_ptr);
			}
		}

		/*
		/// <summary>
		/// Get the environment used for skylight, or null if none exists.
		/// </summary>
		public RenderEnvironment CustomEnvironment
		{
			get
			{
				var penv = UnsafeNativeMethods.ChangeQueue_Skylight_GetCustomEnvironment(m_ptr);
				NativeRenderEnvironment env = null;
				if (penv != IntPtr.Zero)
				{
					env = new NativeRenderEnvironment(penv);
				}
				return env;
			}
		}
		*/

		/// <summary>
		/// Get shadow intensity for skylight
		/// </summary>
		public double ShadowIntensity
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Skylight_GetShadowIntensity(m_ptr);
			}
		}

		/// <summary>
		/// Textual representation of Skylight
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var envstr = UsesCustomEnvironment ? "yes" /*CustomEnvironment.Name*/ : "no";

			return String.Format("Skylight {0}, shadow intensity {1}, custom environment: {2}", Enabled, ShadowIntensity, envstr);
		}
	}
	/// <summary>
	/// ChangeQueue Light change representation
	/// </summary>
	public class Light
	{
		/// <summary>
		/// Light change type
		/// </summary>
		public enum Event
		{
			/// <summary>
			/// Light was added
			/// </summary>
			Added,
			/// <summary>
			/// Light was deleted
			/// </summary>
			Deleted,
			/// <summary>
			/// Light was undeleted
			/// </summary>
			Undeleted,
			/// <summary>
			/// Light was modified
			/// </summary>
			Modified,
			/// <summary>
			/// Light was sorted in LightTable
			/// </summary>
			Sorted,	 // doc.m_table.Sort() potentially changed sort order
		}
		private readonly IntPtr m_ptr;

		internal Light(IntPtr pLight)
		{
			m_ptr = pLight;
		}

		/// <summary>
		/// Get the light object id
		/// </summary>
		public Guid Id
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Light_GetId(m_ptr);
			}
		}

		/// <summary>
		/// Get CRC computed from Id
		/// </summary>
		[CLSCompliant(false)]
		public uint IdCrc
		{
			get
			{
				return ChangeQueue.CrcFromGuid(Id);
			}
		}

		/// <summary>
		/// Get the actual light data
		/// </summary>
		public Geometry.Light Data
		{
			get
			{
				var ld = UnsafeNativeMethods.ChangeQueue_Light_GetLight(m_ptr);
				var l = new Geometry.Light(ld, null);
				l.DoNotDestructOnDispose();
				return l;
			}
		}

		/// <summary>
		/// Get what type of light change this represents
		/// </summary>
		public Event ChangeType
		{
			get
			{
				var r = UnsafeNativeMethods.ChangeQueue_Light_GetEvent(m_ptr);

				return (Event)r;
			}
		}
	}
	/// <summary>
	/// ChangeQueue DynamicObject
	/// </summary>
	public class DynamicObjectTransform
	{
		private readonly IntPtr m_ptr;

		internal DynamicObjectTransform(IntPtr pDynOb)
		{
			m_ptr = pDynOb;
		}

		/// <summary>
		/// Get the mesh instance id for this dynamic object.
		/// </summary>
		[CLSCompliant(false)]
		public uint MeshInstanceId
		{
			get { return UnsafeNativeMethods.ChangeQueue_DynamicObject_GetMeshInstanceId(m_ptr); }
		}

		/// <summary>
		/// Transform for the DynamicObject
		/// </summary>
		public Transform Transform
		{
			get
			{
				var transform = new Transform();
				UnsafeNativeMethods.ChangeQueue_DynamicObject_GetXform(m_ptr, ref transform);

				return transform;
			}
		}

		/// <summary>
		/// String representation of DynamicObject
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format("{0} - {1}", MeshInstanceId, Transform);
		}
	}

	/// <summary>
	/// ChangeQueue clipping plane
	/// </summary>
	public class ClippingPlane
	{
		private readonly IntPtr m_ptr;

		internal ClippingPlane(IntPtr pClippingPlane)
		{
			m_ptr = pClippingPlane;
		}

		/// <summary>
		/// True if clipping plane is enabled
		/// </summary>
		public bool IsEnabled
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetEnabled(m_ptr);
			}
		}

		/// <summary>
		/// Get the plane that represents this clipping plane
		/// </summary>
		public Plane Plane
		{
			get
			{
				Point3d orig = Point3d.Unset;
				Vector3d normal = Vector3d.Unset;
				UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetPlaneOrigin(m_ptr, ref orig);
				UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetPlaneNormal(m_ptr, ref normal);

				return new Plane(orig, normal);
			}
		}

#if MEH
		/// <summary>
		/// Get the underling ClippingPlaneObject
		/// </summary>
		public ClippingPlaneObject Object
		{
			get
			{
				var p_obj = UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetObject(m_ptr);
				var clobj = p_obj != IntPtr.Zero ? RhinoObject.CreateRhinoObjectHelper(p_obj) as ClippingPlaneObject : null;
				if (clobj != null) clobj.DoNotDestructOnDispose();
				return clobj;
			}
		}
#endif

		/// <summary>
		/// Get the ClippingPlaneObject for this clipping plane
		/// </summary>
		public ObjectAttributes Attributes
		{
			get
			{
				var p_obj = UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetObjectAttributes(m_ptr);
				//return RhinoObject.CreateRhinoObjectHelper(p_obj) as ClippingPlaneObject;
				ObjectAttributes attr = p_obj != IntPtr.Zero ? new ObjectAttributes(p_obj) : null;
				if (attr != null) attr.DoNotDestructOnDispose();
				return attr;
			}
		}

		/// <summary>
		/// Get Guid for this clipping plane
		/// </summary>
		public Guid Id
		{
			get
			{
				var id = UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetPlaneUuid(m_ptr);

				return id;
			}
		}

		/// <summary>
		/// Get list of View IDs this clipping plane is supposed to clip.
		/// </summary>
		public List<Guid> ViewIds
		{
			get
			{
				var c = UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetViewCount(m_ptr);
				var viewids = new List<Guid>(c);
				for(int i = 0; i < c; i++)
				{
					var l = UnsafeNativeMethods.ChangeQueue_ClippingPlane_GetPlaneViewUuidAt(m_ptr, i);
					viewids.Add(l);
				}

				return viewids;
			}
		}

	}
	/// <summary>
	/// ChangeQueue ground plane
	/// </summary>
	public class GroundPlane
	{
		private readonly IntPtr m_ptr;

		internal GroundPlane(IntPtr pGroundPlane)
		{
			m_ptr = pGroundPlane;
		}

		/// <summary>
		/// Get true if ground plane should be shadow-only
		/// </summary>
		public bool IsShadowOnly
		{
			get
			{
				var b = UnsafeNativeMethods.ChangeQueue_Groundplane_GetShadowOnly(m_ptr);
				return b;
			}
		}

		/// <summary>
		/// Get the altitude for ground plane
		/// </summary>
		public double Altitude
		{
			get
			{
				var alt = UnsafeNativeMethods.ChangeQueue_Groundplane_GetAltitude(m_ptr);
				return alt;
			}
		}

		/// <summary>
		/// The CRC / RenderHash of the material on this ground plane
		/// </summary>
		[CLSCompliant(false)]
		public uint MaterialId
		{
			get
			{
				var mid = UnsafeNativeMethods.ChangeQueue_Groundplane_GetMaterialId(m_ptr);
				return mid;
			}
		}

		/// <summary>
		/// Get texture scale on the ground plane
		/// </summary>
		public Vector2d TextureScale
		{
			get
			{
				var scale = new Vector2d(0.0, 0.0);
				UnsafeNativeMethods.ChangeQueue_Groundplane_GetTextureScale(m_ptr, ref scale);
				return scale;
			}
		}

		/// <summary>
		/// Get texture offset on the ground plane
		/// </summary>
		public Vector2d TextureOffset
		{
			get
			{
				var scale = new Vector2d(0.0, 0.0);
				UnsafeNativeMethods.ChangeQueue_Groundplane_GetTextureOffset(m_ptr, ref scale);
				return scale;
			}
		}

		/// <summary>
		/// Get texture rotation on the ground plane
		/// </summary>
		public double TextureRotation
		{
			get
			{
				double rot = UnsafeNativeMethods.ChangeQueue_Groundplane_GetTextureRotation(m_ptr);
				return rot;
			}
		}

		/// <summary>
		/// Return true if ground plane is enabled
		/// </summary>
		public bool Enabled
		{
			get
			{
				var enabled = UnsafeNativeMethods.ChangeQueue_Groundplane_GetEnabled(m_ptr);
				return enabled;
			}
		}

		/// <summary>
		/// Get the checksum of this groundplane object
		/// </summary>
		[CLSCompliant(false)]
		public uint Crc
		{
			get
			{
				var crc = UnsafeNativeMethods.ChangeQueue_Groundplane_GetCRC(m_ptr);
				return crc;
			}
		}
	}

	/// <summary>
	/// Mapping Channel for a ChangeQueue Mesh
	/// </summary>
	public class MappingChannel
	{
		private readonly IntPtr m_ptr;
		internal MappingChannel(IntPtr pMappingChannel)
		{
			m_ptr = pMappingChannel;
		}
		/// <summary>
		/// Local transform for the mapping
		/// </summary>
		public Transform Local
		{
			get
			{
				var transform = new Transform();
				UnsafeNativeMethods.CRhRdkCmnChangeQueueMappingChannel_GetXformLocal(m_ptr, ref transform);

				return transform;
			}
		}

		/// <summary>
		/// Return TexturMapping for this MappingChannel
		/// </summary>
		public TextureMapping Mapping
		{
			get
			{
				var m = UnsafeNativeMethods.CRhRdkCmnChangeQueueMappingChannel_GetTextureMapping(m_ptr);
				var tm = new TextureMapping(m);
				tm.DoNotDestructOnDispose();
				return tm;
			}
		}
	}

	/// <summary>
	/// MappingChannels for a Mesh
	/// </summary>
	public class MappingChannelCollection
	{
		private readonly IntPtr m_parent;
		internal MappingChannelCollection(IntPtr mesh)
		{
			m_parent = mesh;
		}

		/// <summary>
		/// Get count of MappingChannels in this collection
		/// </summary>
		public int Count
		{
			get
			{
				return UnsafeNativeMethods.CRhRdkCmnChangeQueueMappingChannels_Count(m_parent);
			}
		}

		/// <summary>
		/// Get MappingChannel on index
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public MappingChannel this[int i]
		{
			get
			{
				if (i < 0 && i >= Count) throw new ArgumentOutOfRangeException();

				var mc = UnsafeNativeMethods.CRhRdkCmnChangeQueueMappingChannels_Get(m_parent, i);
				return new MappingChannel(mc);
			}
		}

		/// <summary>
		/// Enumerate all available channels in this mapping
		/// </summary>
		public IEnumerable<MappingChannel> Channels
		{
			get
			{
				for (var i = 0; i < Count; i++)
				{
					var mc = UnsafeNativeMethods.CRhRdkCmnChangeQueueMappingChannels_Get(m_parent, i);
					yield return new MappingChannel(mc);
				}
			}
		}
	}
	/// <summary>
	/// Representation of ChangeQueue Mesh
	/// </summary>
	public class Mesh
	{
		private readonly IntPtr m_ptr;
		internal Mesh(IntPtr pMesh)
		{
			m_ptr = pMesh;
		}

		internal IntPtr Ptr { get { return m_ptr; } }
		/// <summary>
		/// Get a SimpleArrayMeshPointer containing all meshes for the related Mesh
		/// </summary>
		/// <returns></returns>
		public Geometry.Mesh[] GetMeshes()
		{
			var samp = new SimpleArrayMeshPointer(UnsafeNativeMethods.CRhRdkCmnChangeQueueMesh_meshes(m_ptr));
			var meshes = samp.ToNonConstArray();
			foreach (var m in meshes)
			{
				m.DoNotDestructOnDispose();
			}

			return meshes;
		}

		/// <summary>
		/// Get the Object Guid this mesh is for.
		/// </summary>
		/// <returns>Guid of parent object.</returns>
		public Guid Id()
		{
			return UnsafeNativeMethods.CRhRdkCmnChangeQueueMesh_GetUuidId(m_ptr);
		}

		/// <summary>
		/// Get texture mapping info as single mapping
		/// </summary>
		public MappingChannel SingleMapping
		{
			get
			{
				var ptr = UnsafeNativeMethods.CRhRdkCmnChangeQueueMappingChannels_GetSingleMapping(m_ptr);
				return new MappingChannel(ptr);
			}
		}

		/// <summary>
		/// Get the mapping for this mesh.
		/// </summary>
		public MappingChannelCollection Mapping
		{
			get
			{
				return new MappingChannelCollection(m_ptr);
			}
		}

		/// <summary>
		/// Get object attributes of object associated to this mesh. This will be possible only 
		/// after returning true from ChangeQueue.ProvideOriginalObject()
		/// </summary>
		public ObjectAttributes Attributes
		{
			get
			{
				var robject = UnsafeNativeMethods.CRhRdkCmnChangeQueueMesh_GetObjectAttributes(m_ptr);
				if (robject == IntPtr.Zero) return null;
				var noa = new ObjectAttributes(robject);
				noa.DoNotDestructOnDispose();
				return noa;
			}
		}

		/// <summary>
		/// Get a copy of the original RhinoObject this Mesh is created from. Possible only after
		/// return true from ChangeQueue.ProvideOriginalObject().
		/// 
		/// Access this only with a using(var o = mesh.Object) {} construct.
		/// 
		/// Note that the object is free floating, i.e. not part of a document.
		/// </summary>
		public RhinoObject Object 
		{
			get
			{
				var robject = UnsafeNativeMethods.ChangeQueue_Mesh_GetObject(m_ptr);
				var noa = robject != IntPtr.Zero ? RhinoObject.CreateRhinoObjectHelper(robject): null;
				if(noa!=null) noa.DoNotDestructOnDispose();
				return noa;
			}
		}
	}

	/// <summary>
	/// Representation of ChangeQueue MeshInstance
	/// </summary>
	public class MeshInstance
	{

		private readonly IntPtr m_ptr;
		private readonly ChangeQueue m_changeQueue;

		internal MeshInstance(IntPtr pMeshInstance, ChangeQueue changequeue)
		{
			m_ptr = pMeshInstance;
			m_changeQueue = changequeue;
		}

		/// <summary>
		/// Return true if mesh instance should receive shadows
		/// </summary>
		public bool ReceiveShadows
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetReceiveShadows(m_ptr) == 1; }
		}

		/// <summary>
		/// Return true if mesh instance should cast shadows
		/// </summary>
		public bool CastShadows
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetCastShadows(m_ptr) == 1; }
		}

		/// <summary>
		/// Get the instance identifier for this mesh instance.
		/// </summary>
		[CLSCompliant(false)]
		public uint InstanceId
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetInstanceId(m_ptr); }
		}

		/// <summary>
		/// Get the mesh identifier for this mesh instance.
		/// </summary>
		public Guid MeshId
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetMeshId(m_ptr); }
		}

		/// <summary>
		/// Get identifier that specifies the grouping of these mesh instances - usually based on the object that this originally comprised.
		/// </summary>
		[CLSCompliant(false)]
		public uint GroupId
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetGroupId(m_ptr); }
		}

		/// <summary>
		/// Get the mesh index for this mesh instance.
		/// </summary>
		public int MeshIndex
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetMeshIndex(m_ptr); }
		}

		/// <summary>
		/// The Material CRC / RenderHash
		/// </summary>
		[CLSCompliant(false)]
		public uint MaterialId
		{
			get { return UnsafeNativeMethods.ChangeQueue_MeshInstance_GetMaterialId(m_ptr); }
		}

		/// <summary>
		/// RenderMaterial associated with mesh instance
		/// </summary>
		public RenderMaterial RenderMaterial => m_changeQueue.MaterialFromId(MaterialId);

		/// <summary>
		/// Transform for the MeshInstance
		/// </summary>
		public Transform Transform
		{
			get
			{
				var transform = new Transform();
				UnsafeNativeMethods.ChangeQueue_MeshInstance_GetXform(m_ptr, ref transform);

				return transform;
			}
		}

	}

	/// <summary>
	/// Representation of a Material through the change queue
	/// </summary>
	public class Material
	{
		private readonly IntPtr m_ptr;
		private readonly uint m_mesh_instance_id;

		internal Material(IntPtr ptr)
		{
			m_ptr = ptr;
			m_mesh_instance_id = UnsafeNativeMethods.ChangeQueue_Material_GetMeshInstanceId(m_ptr);
		}

		/// <summary>
		/// Get the material InstanceAncestry
		/// </summary>
		/// <value></value>
		[CLSCompliant(false)]
		public uint MeshInstanceId
		{
			get { return m_mesh_instance_id; }
		}

		/// <summary>
		/// Get the material ID (crc)
		/// </summary>
		[CLSCompliant(false)]
		public uint Id
		{
			get { return UnsafeNativeMethods.ChangeQueue_Material_GetId(m_ptr); }
		}

		/// <summary>
		/// Get mesh index
		/// </summary>
		public int MeshIndex
		{
			get
			{
				return UnsafeNativeMethods.ChangeQueue_Material_GetMeshIndex(m_ptr);
			}
		}
	}

	/// <summary>
	/// Base class for ChangeQueue.
	/// 
	/// Generally used by render plugins to build interactive updating of scenes that are being rendered.
	/// </summary>
	public abstract class ChangeQueue : IDisposable
	{
		private IntPtr m_p_changequeue;

		private int m_serial_number;
		private static int g_current_serial_number = 1;
		private static readonly Dictionary<int, ChangeQueue> g_all_changequeues = new Dictionary<int, ChangeQueue>();
		private ViewInfo m_view;

		internal static ChangeQueue FromSerialNumber(int serial)
		{
			g_all_changequeues.TryGetValue(serial, out ChangeQueue rc);
			return rc;
		}

		/// <summary>
		/// Helper function to get a CRC from a Guid.
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public static uint CrcFromGuid(Guid guid)
		{
			uint crc = 0xBABECAFE;

			crc = RhinoMath.CRC32(crc, guid.ToByteArray());

			return crc;
		}

		/// <summary>
		/// Return true if this ChangeQueue is created for preview rendering.
		/// 
		/// No view was set for preview rendering.
		/// </summary>
		public bool IsPreview { get { return m_view == null; } }

		/// <summary>
		/// Return view ID for a RhinoDoc based ChangeQueue.
		/// 
		/// Returns Guid.Empty if no view was associated with the changequeue,
		/// i.e. preview rendering.
		/// </summary>
		public Guid ViewId
		{
			get { return !IsPreview ? m_view.Viewport.Id : Guid.Empty; }
		}

		/// <summary>
		/// Id of plugin instantiating change queue
		/// </summary>
		private Guid m_plugin_id;

        /// <summary>
        /// Construct a new ChangeQueue from the given document
        /// </summary>
        /// <param name="pluginId">Id of plugin instantiating ChangeQueue</param>
        /// <param name="docRuntimeSerialNumber"></param>
        /// <param name="viewinfo"></param>
        /// <param name="bRespectDisplayPipelineAttributes">Determines whether the display pipeline attributes for this viewport should be taken into account when supplying data.	If you are
        /// using this queue to populate a scene for the "Render" command, set this to false.	If you are using it to draw a realtime preview, you will probably want to set it to true.</param>
        [CLSCompliant(false)]
        [Obsolete]
		protected ChangeQueue(Guid pluginId, uint docRuntimeSerialNumber, ViewInfo viewinfo, bool bRespectDisplayPipelineAttributes)
		{
			m_plugin_id = pluginId;
			m_serial_number = g_current_serial_number++;
			m_view = viewinfo;
			g_all_changequeues.Add(m_serial_number, this);
			m_p_changequeue = UnsafeNativeMethods.CRhRdkCmnChangeQueue_NewSingleViewAndAttributes(pluginId, m_serial_number, docRuntimeSerialNumber, viewinfo == null ? IntPtr.Zero : viewinfo.ConstPointer(), IntPtr.Zero, bRespectDisplayPipelineAttributes, true);
		}

        /// <summary>
        /// Construct a new ChangeQueue from the given document, using given display pipeline attributes.
        /// </summary>
        /// <param name="pluginId">Id of plugin instantiating ChangeQueue</param>
        /// <param name="docRuntimeSerialNumber"></param>
        /// <param name="viewinfo"></param>
        /// <param name="attributes">Display pipeline attributes to use.</param>
        /// <param name="bRespectDisplayPipelineAttributes">Determines whether the display pipeline attributes for this viewport should be taken into account when supplying data.	If you are
        /// using this queue to populate a scene for the "Render" command, set this to false.	If you are using it to draw a realtime preview, you will probably want to set it to true.</param>
        /// <param name="bNotifyChanges">Determines whether you want to receive updates when the model changes.  If you are rendering for a view capture, print or something similar, you will probably want to set this to false.</param>
        [CLSCompliant(false)]
		protected ChangeQueue(Guid pluginId, uint docRuntimeSerialNumber, ViewInfo viewinfo, Display.DisplayPipelineAttributes attributes, bool bRespectDisplayPipelineAttributes, bool bNotifyChanges)
		{
			m_plugin_id = pluginId;
			m_serial_number = g_current_serial_number++;
			m_view = viewinfo;
			g_all_changequeues.Add(m_serial_number, this);
			m_p_changequeue = UnsafeNativeMethods.CRhRdkCmnChangeQueue_NewSingleViewAndAttributes(pluginId, m_serial_number, docRuntimeSerialNumber, viewinfo == null ? IntPtr.Zero : viewinfo.ConstPointer(), (attributes==null) ? IntPtr.Zero : attributes.ConstPointer(), bRespectDisplayPipelineAttributes, bNotifyChanges);
		}


		/// <summary>
		/// Construct a new ChangeQueue using the given CreatePreviewEventArgs.
		/// 
		/// The preview scene for these args will be used to populate the world.
		/// </summary>
		/// <param name="pluginId">Id of plugin instantiating ChangeQueue</param>
		/// <param name="createPreviewEventArgs"></param>
		protected ChangeQueue(Guid pluginId, CreatePreviewEventArgs createPreviewEventArgs)
		{
			m_plugin_id = pluginId;
			m_serial_number = g_current_serial_number++;
			m_view = null;
			g_all_changequeues.Add(m_serial_number, this);
			m_p_changequeue = UnsafeNativeMethods.CRhRdkCmnChangeQueueFromPreviewScene_New(pluginId, m_serial_number,
				createPreviewEventArgs.SceneServerPtr);
		}

		/// <summary>
		/// Call Flush() once, after that automatically dispose the queue.
		/// </summary>
		public void OneShot()
		{
			Flush();
			Dispose();
		}

		/// <summary>
		/// Calls CreateWorld with true passed.
		/// </summary>
		public void CreateWorld()
		{
			CreateWorld(true);
		}

		/// <summary>
		/// Signal the queue to do the initialisation of the queue, seeding it with the content currently available.
		/// </summary>
		/// <param name="bFlushWhenReady">Set to true CreateWorld should automatically flush at the end.
		/// Note that the Flush called when true is passed doesn't apply changes.
		/// </param>
		public void CreateWorld(bool bFlushWhenReady)
		{
			UnsafeNativeMethods.CRhRdkCmnChangeQueue_CreateWorld(NonConstPtr, bFlushWhenReady);
		}


		/// <summary>
		/// Tell the ChangeQueue to flush all collected events.
		///
		/// This can trigger a host of Apply* calls.
		/// 
		/// The following is the order in which those calls get
		/// made if there are changes for that specific data type:
		/// 
		/// ApplyViewChange
		/// ApplyLinearWorkflowChanges
		/// ApplyDynamicObjectTransforms
		/// ApplyDynamicLightChanges
		/// ApplyRenderSettingsChanges
		/// ApplyEnvironmentChanges (background)
		/// ApplyEnvironmentChanges (refl)
		/// ApplyEnvironmentChanges (sky)
		/// ApplySkylightChanges
		/// ApplySunChanges
		/// ApplyLightChanges
		/// ApplyMaterialChanges
		/// ApplyMeshChanges
		/// ApplyMeshInstanceChanges
		/// ApplyGroundPlaneChanges
		/// ApplyClippingPlaneChanges
		/// </summary>
		public void Flush()
		{
			UnsafeNativeMethods.CRhRdkCmnChangeQueue_Flush(NonConstPtr);
		}

		/// <summary>
		/// Get skylight known to the queue at the time of the Flush
		/// </summary>
		/// <returns></returns>
		public Skylight GetQueueSkylight()
		{
			var p_cq_skylight = UnsafeNativeMethods.CRhRdkCmnChangeQueue_Skylight(NonConstPtr);
			Skylight skylight = null;
			if (p_cq_skylight != IntPtr.Zero)
			{
				skylight = new Skylight(p_cq_skylight);
			}
			return skylight;
		}

		/// <summary>
		/// Get groundplane known to the queue at the time of the Flush
		/// </summary>
		/// <returns></returns>
		public GroundPlane GetQueueGroundPlane()
		{
			var p_cq_groundplane = UnsafeNativeMethods.CRhRdkCmnChangeQueue_GroundPlane(NonConstPtr);
			GroundPlane gp = null;
			if (p_cq_groundplane != IntPtr.Zero)
			{
				gp = new GroundPlane(p_cq_groundplane);
			}
			return gp;
		}

		/// <summary>
		/// Get render settings known to the queue at the time of the Flush
		/// </summary>
		/// <returns></returns>
		public RenderSettings GetQueueRenderSettings()
		{
			var p_cq_rendersettings = UnsafeNativeMethods.CRhRdkCmnChangeQueue_RenderSettings(NonConstPtr);
			RenderSettings render_settings = null;
			if (p_cq_rendersettings != IntPtr.Zero)
			{
				render_settings = new RenderSettings(p_cq_rendersettings);
			}

			return render_settings;
		}

		/// <summary>
		/// Get sun known to the queue at the time of the Flush
		/// </summary>
		/// <returns></returns>
		public Geometry.Light GetQueueSun()
		{
			var p_cq_sun = UnsafeNativeMethods.CRhRdkCmnChangeQueue_Sun(NonConstPtr);
			Geometry.Light sun = null;
			if (p_cq_sun != IntPtr.Zero)
			{
				sun = new Geometry.Light(p_cq_sun, null);
			}

			return sun;
		}

		/// <summary>
		/// Get view known to the queue at the time of the Flush
		/// </summary>
		/// <returns>ViewInfo</returns>
		public ViewInfo GetQueueView()
		{
			var p_cq_view = UnsafeNativeMethods.CRhRdkCmnChangeQueue_View(NonConstPtr);
			ViewInfo view = null;
			if (p_cq_view != IntPtr.Zero)
			{
				view = new ViewInfo(p_cq_view, true);
			}

			return view;
		}

		/// <summary>
		/// Compare to ViewInfo instances and decide whether they are equal or not.
		/// 
		/// If you need to change the way the comparison is done you can override
		/// this function and implement your custom comparison.
		/// </summary>
		/// <param name="aView">First ViewInfo to compare</param>
		/// <param name="bView">Second ViewInfo to compare</param>
		/// <returns>true if the views are considered equal</returns>
		public virtual bool AreViewsEqual(ViewInfo aView, ViewInfo bView)
		{
			return UnsafeNativeMethods.CRhRdkCmnChangeQueue_AreViewsEqual(NonConstPtr, aView.ConstPointer(), bView.ConstPointer());
		}

		/// <summary>
		/// Get the scene bounding box
		/// </summary>
		/// <returns>Scene bounding box</returns>
		public BoundingBox GetQueueSceneBoundingBox()
		{
			double minx = 0, miny = 0, minz = 0, maxx = 0, maxy = 0, maxz = 0;
			UnsafeNativeMethods.CRhRdkCmnChangeQueue_SceneBoundingBox(NonConstPtr, ref minx, ref miny, ref minz, ref maxx, ref maxy, ref maxz);

			return new BoundingBox(minx, miny, minz, maxx, maxy, maxz);
		}

		internal IntPtr NonConstPtr { get { return m_p_changequeue; } }

		/// <summary>
		/// Dispose our ChangeQueue
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}

		private bool _disposed;
		/// <summary>
		/// Dispose our ChangeQueue. Disposes unmanaged memory.
		/// </summary>
		/// <param name="isDisposing"></param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (!_disposed)
			{
				if (g_all_changequeues.ContainsKey(m_serial_number))
				{
					g_all_changequeues.Remove(m_serial_number);
				}
				if (isDisposing && IntPtr.Zero != m_p_changequeue)
				{
					UnsafeNativeMethods.CRhRdkCmnChangeQueue_Delete(NonConstPtr);
				}
				m_p_changequeue = IntPtr.Zero;
				GC.Collect();
				_disposed = true;
			}
		}

		/// <summary>
		/// Get the RenderMaterial from the ChangeQueue material cache based on RenderHash
		/// </summary>
		/// <param name="crc">The RenderHash</param>
		/// <returns>RenderMaterial</returns>
		[CLSCompliant(false)]
		public RenderMaterial MaterialFromId(uint crc)
		{
			var mat_ptr = UnsafeNativeMethods.ChangeQueue_RdkMaterial(m_p_changequeue, crc);
			var native = new NativeRenderMaterial(mat_ptr);
			return native;
		}

		/// <summary>
		/// Get RenderEnvironment RenderHash for given usage.
		/// </summary>
		/// <param name="usage"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public uint EnvironmentIdForUsage(RenderEnvironment.Usage usage)
		{
			return UnsafeNativeMethods.ChangeQueue_EnvironmentIdForUsage(m_p_changequeue, (int)usage);
		}

		/// <summary>
		/// Get RenderEnvironment for given RenderHash
		/// </summary>
		/// <param name="crc"></param>
		/// <returns></returns>
		[CLSCompliant(false)]
		public RenderEnvironment EnvironmentForid(uint crc)
		{
			var p_env = UnsafeNativeMethods.ChangeQueue_EnvironmentFromId(m_p_changequeue, crc);
			RenderEnvironment env = null;
			if (p_env != IntPtr.Zero)
			{
				env = new NativeRenderEnvironment(p_env);
			}

			return env;
		}

		/// <summary>
		/// Get the DisplayPipelineAttributes if available, null otherwise
		/// </summary>
		public Display.DisplayPipelineAttributes DisplayPipelineAttributes
		{
			get
			{
				var pdpa = UnsafeNativeMethods.CRhRdkCmnChangeQueue_GetDisplayPipelineAttributes(m_p_changequeue);
				if (pdpa != IntPtr.Zero)
				{
					Display.DisplayPipelineAttributes dpa = new Display.DisplayPipelineAttributes(pdpa, true);

					return dpa;
				}
				return null;
			}
		}

		/// <summary>
		/// Convert given (camera-based) light to a world-based light (in-place)
		/// </summary>
		/// <param name="changequeue"></param>
		/// <param name="light"></param>
		/// <param name="vp"></param>
		public static void ConvertCameraBasedLightToWorld(ChangeQueue changequeue, Light light, ViewInfo vp)
		{
			if (vp == null || light == null) return;
			UnsafeNativeMethods.ChangeQueue_ConvertCameraBasedLightToWorld(changequeue.m_p_changequeue, light.Data.NonConstPointer(), vp.NonConstViewportPointer());
		}

#region ChangeQueue API that client can implement

		internal delegate void VoidCallback(int serial);

		internal delegate void ViewCallback(int serial, IntPtr viewInfo);
		internal static ViewCallback m_on_apply_viewchange = OnApplyViewChange;

		/// <summary>
		/// Handler for view changes
		/// </summary>
		/// <param name="serial">runtime serial of the ChangeQueue instance.</param>
		/// <param name="viewInfo">Reference to ON_3dmView.m_vp</param>
		static private void OnApplyViewChange(int serial, IntPtr viewInfo)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				changequeue.ApplyViewChange(new ViewInfo(viewInfo, true));
			}
		}

		/// <summary>
		/// Override ApplyViewChange when you want to receive changes to the view/camera
		/// </summary>
		/// <param name="viewInfo">The ViewInfo with the changes</param>
		protected virtual void ApplyViewChange(ViewInfo viewInfo)
		{
		}

		internal delegate void DynamicObjectChangesCallback(int serial, IntPtr pSimpleArrayDynamicObjects);
		internal static DynamicObjectChangesCallback m_on_apply_dynamicobject_transforms = OnApplyDynamicObjectChanges;

		static private void OnApplyDynamicObjectChanges(int serial, IntPtr pSimpleArrayObjectTransforms)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var transforms_count = UnsafeNativeMethods.ChangeQueue_DynamicObjectArray_Count(pSimpleArrayObjectTransforms);
				var transforms = new List<DynamicObjectTransform>(transforms_count);
				for (i = 0; i < transforms_count; i++)
				{
					var dynobptr = UnsafeNativeMethods.ChangeQueue_DynamicObjectArray_Get(pSimpleArrayObjectTransforms, i);
					var dynob = new DynamicObjectTransform(dynobptr);
					transforms.Add(dynob);
				}
				changequeue.ApplyDynamicObjectTransforms(transforms);
			}
		}
		/// <summary>
		/// Handle dynamic object transforms
		/// </summary>
		protected virtual void ApplyDynamicObjectTransforms(List<DynamicObjectTransform> dynamicObjectTransforms)
		{
		}

		internal delegate void DynamicLightChangesCallback(int serial, IntPtr pSimpleArrayLights);
		internal static DynamicLightChangesCallback m_on_apply_dynamic_lightchanges = OnApplyDynamicLightChanges;

		private static void OnApplyDynamicLightChanges(int serial, IntPtr pSimpleArrayLight)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var count = UnsafeNativeMethods.ChangeQueue_OnLightArray_Count(pSimpleArrayLight);
				var lights = new List<Geometry.Light>(count);

				for (i = 0; i < count; i++)
				{
					var lpr = UnsafeNativeMethods.ChangeQueue_OnLightArray_Get(pSimpleArrayLight, i);
					var l = new Geometry.Light(lpr, null);
					l.DoNotDestructOnDispose();
					lights.Add(l);
				}
				changequeue.ApplyDynamicLightChanges(lights);
			}
		}

		/// <summary>
		/// Handle dynamic light changes
		/// </summary>
		protected virtual void ApplyDynamicLightChanges(List<Geometry.Light> dynamicLightChanges)
		{
		}

		internal delegate void MeshChangesCallback(int serial, IntPtr pSimpleArrayUuid, IntPtr pSimpleArrayMesh);
		internal static MeshChangesCallback m_on_apply_meshchanges = OnApplyMeshChanges;

		/// <summary>
		/// Handler for receiving mesh changes
		/// </summary>
		/// <param name="serial">Runtime serial of the ChangeQueue instance.</param>
		/// <param name="pDeletedUuids"></param>
		/// <param name="pAddedMeshes"></param>
		static private void OnApplyMeshChanges(int serial, IntPtr pDeletedUuids, IntPtr pAddedMeshes)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var added_count = UnsafeNativeMethods.ChangeQueue_MeshArray_Count(pAddedMeshes);
				var deleted_orig = new SimpleArrayGuidPointer(pDeletedUuids);
				var deleted = deleted_orig.ToArray();
				var added = new List<Mesh>(added_count);
				for (i = 0; i < added_count; i++)
				{
					added.Add(new Mesh(UnsafeNativeMethods.ChangeQueue_MeshArray_Get(pAddedMeshes, i)));
				}
				changequeue.ApplyMeshChanges(deleted, added);
			}
		}

		//virtual void ApplyMeshChanges(const ON_SimpleArray<const UUID*>& deleted, const ON_SimpleArray<const Mesh*>& added) const {}
		/// <summary>
		/// Override this when you want to handle mesh changes.
		/// </summary>
		/// <param name="deleted">List of Guids to meshes that have been deleted</param>
		/// <param name="added">List of ChangeQueueMeshes that have been added or changed</param>
		protected virtual void ApplyMeshChanges(Guid[] deleted, List<Mesh> added)//List<Guid> deleted, List<Mesh> addedOrChanged)
		{
		}
		internal delegate void MeshInstanceChangesCallback(int serial, IntPtr pSimpleArrayUuid, IntPtr pSimpleArrayMeshInstance);
		internal static MeshInstanceChangesCallback m_on_apply_meshinstancechanges = OnApplyMeshInstanceChanges;

		private static void OnApplyMeshInstanceChanges(int serial, IntPtr pMeshIdList, IntPtr pAddedMeshInstancesList)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var deleted_count = UnsafeNativeMethods.ChangeQueue_MeshInstanceIdArray_Count(pMeshIdList);
				var added_count = UnsafeNativeMethods.ChangeQueue_MeshInstanceArray_Count(pAddedMeshInstancesList);
				var deleted = new List<uint>(deleted_count);
				var added = new List<MeshInstance>(added_count);
				for (i = 0; i < deleted_count; i++)
				{
					deleted.Add((UnsafeNativeMethods.ChangeQueue_MeshInstanceIdArray_Get(pMeshIdList, i)));
				}
				for (i = 0; i < added_count; i++)
				{
					added.Add(new MeshInstance(UnsafeNativeMethods.ChangeQueue_MeshInstanceArray_Get(pAddedMeshInstancesList, i), changequeue));
				}

				changequeue.ApplyMeshInstanceChanges(deleted, added);
			}
		}

		/// <summary>
		/// Override this when you want to handle mesh instance changes (block instances and first-time added new meshes)
		/// </summary>
		/// <param name="deleted"></param>
		/// <param name="addedOrChanged"></param>
		[CLSCompliant(false)]
		protected virtual void ApplyMeshInstanceChanges(List<uint> deleted, List<MeshInstance> addedOrChanged)
		{
		}

		internal delegate void SunChangesCallback(int serial, IntPtr light);
		internal static SunChangesCallback m_on_apply_sunchanges = OnApplySunChanges;

		private static void OnApplySunChanges(int serial, IntPtr light)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				var sun = new Geometry.Light(light, null);

				sun.DoNotDestructOnDispose();
				changequeue.ApplySunChanges(sun);
			}

		}

		/// <summary>
		/// Override this when you want to handle sun changes
		/// </summary>
		protected virtual void ApplySunChanges(Geometry.Light sun)
		{
		}

		internal delegate void SkylightChangesCallback(int serial, IntPtr pCqSkylight);
		internal static SkylightChangesCallback m_on_apply_skylightchanges = OnApplySkylightChanges;

		private static void OnApplySkylightChanges(int serial, IntPtr pCqSkylight)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				var skylight = new Skylight(pCqSkylight);
				changequeue.ApplySkylightChanges(skylight);
			}
		}

		/// <summary>
		/// Override this when you want to handle skylight changes
		/// </summary>
		/// <param name="skylight"></param>
		protected virtual void ApplySkylightChanges(Skylight skylight)
		{
		}


		internal delegate void LightChangesCallback(int serial, IntPtr pSimpleArrayLights);
		internal static LightChangesCallback m_on_apply_lightchanges = OnApplyLightChanges;

		private static void OnApplyLightChanges(int serial, IntPtr pSimpleArrayLight)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var count = UnsafeNativeMethods.ChangeQueue_LightArray_Count(pSimpleArrayLight);
				var lights = new List<Light>(count);

				for (i = 0; i < count; i++)
				{
					var lpr = UnsafeNativeMethods.ChangeQueue_LightArray_Get(pSimpleArrayLight, i);
					var l = new Light(lpr);
					lights.Add(l);
				}
				changequeue.ApplyLightChanges(lights);
			}
		}

		/// <summary>
		/// Override this when you want to handle light changes
		/// </summary>
		protected virtual void ApplyLightChanges(List<Light> lightChanges)
		{
		}

		internal delegate void MaterialChangesCallback(int serial, IntPtr pSimpleArrayMaterial);
		internal static MaterialChangesCallback m_on_apply_materialchanges = OnApplyMaterialChanges;

		static private void OnApplyMaterialChanges(int serial, IntPtr pChangedMaterials)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var changed_count = UnsafeNativeMethods.ChangeQueue_MaterialArray_Count(pChangedMaterials);
				var changed = new List<Material>(changed_count);
				for (i = 0; i < changed_count; i++)
				{
					changed.Add(new Material(UnsafeNativeMethods.ChangeQueue_MaterialArray_Get(pChangedMaterials, i)));
				}

				changequeue.ApplyMaterialChanges(changed);
			}

		}

		/// <summary>
		/// Override when you want to handle material changes
		/// </summary>
		/// <param name="mats">List of ChangeQueue::Material that have changed</param>
		protected virtual void ApplyMaterialChanges(List<Material> mats)
		{
		}

		internal delegate void RenderSettingsChangesCallback(int serial, IntPtr renderSettingsIntPtr);
		internal static RenderSettingsChangesCallback m_on_apply_backgroundchanges = OnApplyRenderSettingsChanges;

		private static void OnApplyRenderSettingsChanges(int serial, IntPtr renderSettingsIntPtr)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				var rs = new RenderSettings(renderSettingsIntPtr);
				changequeue.ApplyRenderSettingsChanges(rs);
			}
		}

		/// <summary>
		/// Override this when you need to handle background changes (through RenderSettings)
		/// </summary>
		/// <param name="rs"></param>
		protected virtual void ApplyRenderSettingsChanges(RenderSettings rs)
		{

		}


		internal delegate void EnvironmentChangesCallback(int serial, RenderEnvironment.Usage usage);
		internal static EnvironmentChangesCallback m_on_apply_environmentchanges = OnApplyEnvironmentChanges;

		private static void OnApplyEnvironmentChanges(int serial, RenderEnvironment.Usage usage)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				changequeue.ApplyEnvironmentChanges(usage);
			}
		}
		/// <summary>
		/// Override this when you want to handle environment changes
		/// </summary>
		protected virtual void ApplyEnvironmentChanges(RenderEnvironment.Usage usage)
		{
		}

		internal delegate void GroundplaneChangesCallback(int serial, IntPtr groundPlanePtr);
		internal static GroundplaneChangesCallback m_on_apply_groundplanechanges = OnApplyGroundplaneChanges;

		private static void OnApplyGroundplaneChanges(int serial, IntPtr groundPlanePtr)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				var gp = new GroundPlane(groundPlanePtr);
				changequeue.ApplyGroundPlaneChanges(gp);
			}
		}

		/// <summary>
		/// Override this when you want to handle groundplane changes
		/// </summary>
		/// <param name="gp"></param>
		protected virtual void ApplyGroundPlaneChanges(GroundPlane gp)
		{
		}

		internal delegate void ClippingPlaneChangesCallback(int serial, IntPtr deletedCpUuidArray, IntPtr addedOrModifiedCpArray);
		internal static ClippingPlaneChangesCallback m_on_apply_clippingplanechanges = OnApplyClippingPlaneChanges;

		private static void OnApplyClippingPlaneChanges(int serial, IntPtr deletedCpUuidArray, IntPtr addedOrModifiedCpArray)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var deleted_orig = new SimpleArrayGuidPointer(deletedCpUuidArray);
				var deleted = deleted_orig.ToArray();
				var added_count = UnsafeNativeMethods.ChangeQueue_ClippingPlaneArray_Count(addedOrModifiedCpArray);
				var added = new List<ClippingPlane>(added_count);
				for (i = 0; i < added_count; i++)
				{
					added.Add(new ClippingPlane(UnsafeNativeMethods.ChangeQueue_ClippingPlaneArray_Get(addedOrModifiedCpArray, i)));
				}

				changequeue.ApplyClippingPlaneChanges(deleted, added);
			}
		}

		/// <summary>
		/// Override this when you want to handle clippingplane changes
		/// </summary>
		/// <param name="deleted"></param>
		/// <param name="addedOrModified"></param>
		protected virtual void ApplyClippingPlaneChanges(Guid[] deleted, List<ClippingPlane> addedOrModified)
		{
		}

		internal delegate void DynamicClippingPlaneChangesCallback(int serial,	IntPtr changedArray);
		internal static DynamicClippingPlaneChangesCallback m_on_apply_dynamicclippingplanechanges = OnApplyDynamicClippingPlaneChanges;

		private static void OnApplyDynamicClippingPlaneChanges(int serial, IntPtr changedArray)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				int i;
				var added_count = UnsafeNativeMethods.ChangeQueue_ClippingPlaneArray_Count(changedArray);
				var added = new List<ClippingPlane>(added_count);
				for (i = 0; i < added_count; i++)
				{
					added.Add(new ClippingPlane(UnsafeNativeMethods.ChangeQueue_ClippingPlaneArray_Get(changedArray, i)));
				}

				changequeue.ApplyDynamicClippingPlaneChanges(added);
			}
		}

		/// <summary>
		/// Override this when you want to handle dynamicclippingplane changes
		/// </summary>
		/// <param name="changed"></param>
		protected virtual void ApplyDynamicClippingPlaneChanges(List<ClippingPlane> changed)
		{
		}

		/*		internal delegate void RenderSettingsChangesCallback(int serial, IntPtr pCqRenderSettings);
				internal static RenderSettingsChangesCallback m_on_apply_rendersettingschanges = OnApplyRenderSettingsChanges;

				private static void OnApplyRenderSettingsChanges(int serial, IntPtr pCqRenderSettings)
				{
					var changequeue = FromSerialNumber(serial);

					if (changequeue != null)
					{
						var rendersettings = new DisplayRenderSettings(pCqRenderSettings);
						changequeue.ApplyRenderSettingsChanges(rendersettings);
					}
				}*/

		/// <summary>
		/// Override this when you want to handle render setting changes. These are for the viewport settings.
		/// </summary>
		protected virtual void ApplyRenderSettingsChanges(DisplayRenderSettings settings)
		{
		}



		internal delegate bool ProvideOriginalObjectCallback(int serial);
		internal static ProvideOriginalObjectCallback m_on_provideoriginalobject = OnProvideOriginalObject;
		internal static bool OnProvideOriginalObject(int serial)
		{

			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				return changequeue.ProvideOriginalObject();
			}

			return false;
		}

		/// <summary>
		/// Override ProvideOriginalObject and return true if you want original objects supplied with
		/// ChangeQueue.Mesh. This will then also allow access to the Attributes.UserData of the original
		/// object from which the Mesh was generated.
		/// </summary>
		/// <returns></returns>
		protected virtual bool ProvideOriginalObject()
		{
			return false;
		}


		internal delegate void LinearWorkflowChangesCallback(int serial, IntPtr pCqLinearWorkflow);
		internal static LinearWorkflowChangesCallback m_on_apply_linearworkflowchanges = OnApplyLinearWorkflowChanges;

		private static void OnApplyLinearWorkflowChanges(int serial, IntPtr pCqLinearWorkflow)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				var linearworkflow = new LinearWorkflow(pCqLinearWorkflow);
				changequeue.ApplyLinearWorkflowChanges(linearworkflow);
			}
		}

		/// <summary>
		/// Override this when you want to handle linear workflow changes
		/// </summary>
		protected virtual void ApplyLinearWorkflowChanges(LinearWorkflow lw)
		{
		}

		internal delegate void BeginUpdatesCallback(int serial);
		internal static BeginUpdatesCallback m_on_notify_beginupdates = OnNotifyBeginUpdates;

		static private void OnNotifyBeginUpdates(int serial)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				changequeue.NotifyBeginUpdates();
			}
		}

		/// <summary>
		/// Override this when you want to be notified of begin of changes
		/// </summary>
		protected virtual void NotifyBeginUpdates()
		{
		}

		internal delegate void EndUpdatesCallback(int serial);
		internal static EndUpdatesCallback m_on_notify_endupdates = OnNotifyEndUpdates;

		static private void OnNotifyEndUpdates(int serial)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				changequeue.NotifyEndUpdates();
			}
		}

		/// <summary>
		/// Override this when you want to be notified when change notifications have ended.
		/// </summary>
		protected virtual void NotifyEndUpdates()
		{
		}

		internal delegate void DynamicUpdatesAreAvailableCallback(int serial);
		internal static DynamicUpdatesAreAvailableCallback m_on_notify_dynamicupdatesareavailable = OnNotifyDynamicUpdatesAreAvailable;

		static private void OnNotifyDynamicUpdatesAreAvailable(int serial)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				changequeue.NotifyDynamicUpdatesAreAvailable();
			}
		}

		/// <summary>
		/// Override this when you want to be notified dynamic updates are available
		/// </summary>
		protected virtual void NotifyDynamicUpdatesAreAvailable()
		{
		}

		/// <summary>
		/// Enumeration of functions for baking to conduct.
		/// </summary>
		[Flags]
		[CLSCompliant(false)]
		public enum BakingFunctions : uint
		{
			/// <summary>
			/// No baking
			/// </summary>
			None = 0x00,
			/// <summary>
			/// Bake decals
			/// </summary>
			Decals = 0x01,
			/// <summary>
			/// Bake procedural textures
			/// </summary>
			ProceduralTextures = 0x02,
			/// <summary>
			/// Bake custom object mappings
			/// </summary>
			CustomObjectMappings = 0x04,
			/// <summary>
			/// Bake WCS-based mappings
			/// </summary>
			WcsBasedMappings = 0x08,
			/// <summary>
			/// Bake multiple mapping channels
			/// </summary>
			MultipleMappingChannels = 0x10,
			/// <summary>
			/// Bake no-repeat textures
			/// </summary>
			NoRepeatTextures = 0x20,
			/// <summary>
			/// Bake everything
			/// </summary>
			All = 0xFFFFFFFF
		}

		internal delegate uint BakeForCallback(int serial);
		internal static BakeForCallback m_on_bakefor = OnBakeFor;

		static private uint OnBakeFor(int serial)
		{
			var changequeue = FromSerialNumber(serial);

			if (changequeue != null)
			{
				return (uint)changequeue.BakeFor();
			}

			return (uint)BakingFunctions.None;
		}

		internal delegate int BakingSizeCallback(int serial, uint rhino_object, IntPtr material, int type);
		internal static BakingSizeCallback m_on_bakingsize = OnBakingSize;

		static private int OnBakingSize(int serial, uint object_serial, IntPtr pMaterial, int type)
		{
			var changequeue = FromSerialNumber(serial);
			var rhino_object = RhinoObject.FromRuntimeSerialNumber(object_serial);
			var material = RenderMaterial.FromPointer(pMaterial) as RenderMaterial;

			if (changequeue != null)
			{
				return changequeue.BakingSize(rhino_object, material, (Rhino.DocObjects.TextureType)type);
			}

			return 2048;
		}

		/// <summary>
		/// Override this if you need to control baking behavior for textures. By
		/// default bake everything.
		/// </summary>
		/// <returns></returns>
		[CLSCompliant(false)]
		protected virtual BakingFunctions BakeFor()
		{
			return BakingFunctions.All;
		}

		/// <summary>
		/// Override this if you need to control the size of the baked bitmaps for textures. By
		/// default the value returned is 2048.
		/// </summary>
		/// <returns></returns>
		protected virtual int BakingSize(RhinoObject ro, RenderMaterial material, TextureType type)
		{
			return 2048;
		}

		internal delegate void ApplyDisplayPipelineAttributesChangesCallback(int serial, IntPtr displayPipelineAttributes);
		internal static ApplyDisplayPipelineAttributesChangesCallback m_on_displaypipelineattributeschanges = OnApplyDisplayPipelineAttributesChanges;

		static private void OnApplyDisplayPipelineAttributesChanges(int serial, IntPtr displayPipelineAttributes)
		{
			var changequeue = FromSerialNumber(serial);
			Display.DisplayPipelineAttributes dpa = new Display.DisplayPipelineAttributes(displayPipelineAttributes, true);

			if (changequeue != null)
			{
				changequeue.ApplyDisplayPipelineAttributesChanges(dpa);
			}
		}

		/// <summary>
		/// Override if you need to react to display attribute changes.
		/// 
		/// This function is needed to be able to react to different sample settings for i.e.
		/// capture preview rendering.
		/// </summary>
		/// <param name="displayPipelineAttributes">The changed DisplayPipelineAttributes.</param>
		protected virtual void ApplyDisplayPipelineAttributesChanges(Display.DisplayPipelineAttributes displayPipelineAttributes)
		{
		}
#endregion
	}
}
#endif