using System.Drawing;
#pragma warning disable 1591
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Rhino.DocObjects;
using Rhino.Display;
using System.Runtime.InteropServices;

namespace Rhino.Render
{
	/// <summary>
	/// Class information obligatory for registering RealtimeDisplayMode
	/// implementations.
	/// </summary>
	public abstract class RealtimeDisplayModeClassInfo
	{
		/// <summary>
		/// Get human-facing class description for RealtimeDisplayMode
		/// implementation. This string might show up in the Rhino
		/// UI.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Get the RealtimeDisplayMode implementation GUID
		/// </summary>
		public abstract Guid GUID { get; }

		/// <summary>
		/// Return true if the RealtimeDisplayMode draws its result
		/// using OpenGL. RenderWindow usage will then be skipped.
		/// </summary>
		public abstract bool DrawOpenGl { get; }

		/// <summary>
		/// Get the type being registered.
		/// </summary>
		public abstract Type RealtimeDisplayModeType { get; }

		/// <summary>
		/// Override and return true when you don't want your class info
		/// to cause display attributes to be registered with the system.
		/// </summary>
		public virtual bool DontRegisterAttributesOnStart => false;
	}

	/// <summary>
	/// Helper class to wrap a C++ RealtimeDisplayMode implementation.
	/// </summary>
	internal class RealtimeDisplayModePointerWrapper : RealtimeDisplayMode
	{
		static string InvalidOpString => "Call not allowed on RealtimeDisplayModePointerWrapper";
		internal RealtimeDisplayModePointerWrapper(IntPtr rtdmPtr) : base(rtdmPtr)
		{
		}

		private void RealtimeDisplayModePointerWrapper_HudPlayButtonPressed(object sender, EventArgs e)
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		private void RealtimeDisplayModePointerWrapper_HudPauseButtonPressed(object sender, EventArgs e)
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		private void RealtimeDisplayModePointerWrapper_HudUnlockButtonPressed(object sender, EventArgs e)
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		private void RealtimeDisplayModePointerWrapper_HudLockButtonPressed(object sender, EventArgs e)
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		public override void GetRenderSize(out int width, out int height)
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		public override bool IsCompleted()
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		public override bool IsFrameBufferAvailable(ViewInfo view)
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		public override bool IsRendererStarted()
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		public override void ShutdownRenderer()
		{
			throw new InvalidOperationException(InvalidOpString);
		}

		public override bool StartRenderer(int w, int h, RhinoDoc doc, ViewInfo view, ViewportInfo viewportInfo, bool forCapture, RenderWindow renderWindow)
		{
			throw new InvalidOperationException(InvalidOpString);
		}
	}

	/// <summary>
	/// Base class for implementing real-time display modes in .NET.
	/// 
	/// Pay special attention that in StartRenderer the RenderWindow.SetSize()
	/// function is called if the implementation relies on the RenderWindow to
	/// do the drawing to the viewport. If i.e. OpenGL is used to draw render
	/// results to the viewport then SetSize() doesn't have to be called, nor
	/// should the implementation then access channels on the RenderWindow, as
	/// those then don't exist. For OpenGL-based drawing the RenderWindow is
	/// used as a container for ViewInfo management, nothing else.
	/// </summary>
	public abstract class RealtimeDisplayMode
	{
		/// <summary>
		/// Find and register classes that derive from RealtimeDisplayMode
		/// from the given plug-in.
		/// </summary>
		/// <param name="plugin"></param>
		/// <returns></returns>
		public static RealtimeDisplayModeClassInfo[] RegisterDisplayModes(PlugIns.PlugIn plugin)
		{
			return RegisterDisplayModes(plugin.Assembly, plugin.Id);
		}

		/// <summary>
		/// Find and register classes that derive from RealtimeDisplayMode
		/// from the given plug-in. The plug-in is found in the given assembly
		/// </summary>
		/// <param name="assembly"></param>
		/// <param name="pluginId"></param>
		/// <returns></returns>
		public static RealtimeDisplayModeClassInfo[] RegisterDisplayModes(Assembly assembly, Guid pluginId)
		{
			// Check the Rhino plug-in for a RhinoPlugIn with the specified Id
			var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
			// RhinoPlugIn not found so bail, all content gets associated with a plug-in!
			if (plugin == null) return null;

			// Get the RealtimeDisplayModes and register
			var found_realtime_displaymode_types = GetRealtimeDisplayModeTypesFromAssembly(assembly);
			var registered_realtime_displaymode_types = new List<RealtimeDisplayModeClassInfo>();

			foreach(var rtvpci in found_realtime_displaymode_types)
			{
				// register display mode
				var rc = UnsafeNativeMethods.RhRdk_RealtimeDisplayMode_Register(rtvpci.Name, rtvpci.GUID);
				// only install attributes and keep type if registration was success
				if (rc)
				{
					// register display mode attributes
					UnsafeNativeMethods.Rdk_InstallRhRdkCmnRealtimeDisplayModeAndAttributes(rtvpci.Name, rtvpci.GUID, rtvpci.DrawOpenGl, rtvpci.DontRegisterAttributesOnStart);
					registered_realtime_displaymode_types.Add(rtvpci);
				}
			}

			// If this plug-in does not contain any valid RealtimeDisplayMode derived objects then bail
			if (registered_realtime_displaymode_types.Count == 0) return null;

			// Get the RdkPlugIn associated with this RhinoPlugIn, if it is not in
			// the RdkPlugIn dictionary it will get added if possible.
			var rdk_plug_in = RdkPlugIn.GetRdkPlugIn(plugin);

			// Plug-in not found or there was a problem adding it to the dictionary
			if (rdk_plug_in == null) return null;

			// list to hold types that still need to be appended to rdk plugin type list
			var realtime_displaymode_types_to_add = new List<RealtimeDisplayModeClassInfo>();
			foreach (var content_type in registered_realtime_displaymode_types)
				if (!RdkPlugIn.RealtimeDisplayModeTypeIsRegistered(content_type))
					realtime_displaymode_types_to_add.Add(content_type);

			// Append the RdkPlugIn registered content type list
			rdk_plug_in.AddRegisteredRealtimeDisplayModeTypes(realtime_displaymode_types_to_add);

			// Return an array of the valid content types
			return registered_realtime_displaymode_types.ToArray();
		}

		public static void UnregisterDisplayModes(PlugIns.PlugIn plugin)
		{
			UnregisterDisplayModes(plugin.Assembly, plugin.Id);
		}

		public static void UnregisterDisplayModes(Assembly assembly, Guid pluginId)
		{
			// Check the Rhino plug-in for a RhinoPlugIn with the specified Id
			var plugin = PlugIns.PlugIn.GetLoadedPlugIn(pluginId);
			// RhinoPlugIn not found so bail, all content gets associated with a plug-in!
			if (plugin == null) return;

			// Get the RealtimeDisplayModes and register
			var found_realtime_displaymode_types = GetRealtimeDisplayModeTypesFromAssembly(assembly);

			foreach(var rtvpci in found_realtime_displaymode_types)
			{
				// unregister display mode
				UnsafeNativeMethods.RhRdk_RealtimeDisplayMode_Unregister(rtvpci.GUID);
				// unregister display mode attributes
				UnsafeNativeMethods.Rdk_UninstallRhRdkCmnRealtimeDisplayModeAttributes(rtvpci.GUID);
			}
		}

		/// <summary>
		/// Get a list of types that implement RealtimeDisplayMode and are decorated properly
		/// with RealtimeDisplayModeClassInfoAttribute
		/// </summary>
		/// <param name="assembly">Assembly to load RealtimeDisplayMode types from.</param>
		/// <returns>List of tuples with Type and RealtimeDisplayModeClassInfoAttribute instance</returns>
		internal static List<RealtimeDisplayModeClassInfo> GetRealtimeDisplayModeTypesFromAssembly(Assembly assembly)
		{
			// Get a list of the publicly exported class types from the requested assembly
			var exported_types = assembly.GetExportedTypes();
			// Scan the exported class types for RealtimeDisplayMode derived classes
			var realtime_displaymodetypes = new List<RealtimeDisplayModeClassInfo>();
			var realtime_displaymode_type = typeof(RealtimeDisplayMode);
			var realtime_displaymode_classinfo_type = typeof(RealtimeDisplayModeClassInfo);
			for (var i = 0; i < exported_types.Length; i++)
			{
				var exported_type = exported_types[i];
				// If abstract class or not derived from RealtimeDisplayMode or does not contain a public constructor then skip it
				if (exported_type.IsAbstract || !exported_type.IsSubclassOf(realtime_displaymode_classinfo_type) || exported_type.GetConstructor(new Type[] { }) == null)
					continue;

				var classinfo = Activator.CreateInstance(exported_type) as RealtimeDisplayModeClassInfo;

				if (classinfo == null)
					continue;

				var realtime_displaymode_impl_type = classinfo.RealtimeDisplayModeType;

				if (realtime_displaymode_impl_type.IsAbstract || !realtime_displaymode_impl_type.IsSubclassOf(realtime_displaymode_type) || realtime_displaymode_impl_type.GetConstructor(new Type[] { }) == null)
					continue;

				realtime_displaymodetypes.Add(classinfo);
			}

			return realtime_displaymodetypes;
		}

		IntPtr m_realtimeViewport;
		internal RealtimeDisplayMode(IntPtr realtimeViewport) {
			m_realtimeViewport = realtimeViewport;
		}

		protected RealtimeDisplayMode() { }

		/// <summary>
		/// Override PostConstruct if you need to initialize where
		/// the underlying RealtimeDisplayMode is available.
		/// 
		/// The connection is made right after RealtimeDisplayMode
		/// has been instantiated, but just before PostConstruct is called.
		/// 
		/// For instance finding out OpenGL information can be done in
		/// PostConstruct.
		/// </summary>
		public virtual void PostConstruct() { }

		private IntPtr ConstPointer()
		{
			return m_realtimeViewport;
		}

		/// <summary>
		/// Use to signal the underlying pipeline a redraw is wanted. This can be used
		/// for instance when a renderer has completed a pass which should be
		/// updated in the associated viewport.
		/// </summary>
		public void SignalRedraw()
		{
			UnsafeNativeMethods.Rdk_RealtimeDisplayMode_SignalRedraw(ConstPointer());
		}

		/// <summary>
		/// Compute viewport CRC for the given ViewInfo
		/// </summary>
		/// <param name="view"></param>
		/// <returns>the CRC for the given view</returns>
		[CLSCompliant(false)]
		public uint ComputeViewportCrc(ViewInfo view)
		{
			return UnsafeNativeMethods.Rdk_RealtimeDisplayMode_ComputeViewportCRC(view.NonConstViewportPointer());
		}

		/// <summary>
		/// Set ViewInfo for this RealtimeDisplayMode instance.
		/// </summary>
		/// <param name="view">The ViewInfo to set for subsequent tests.</param>
		[CLSCompliant(false)]
		public void SetView(ViewInfo view)
		{
			UnsafeNativeMethods.Rdk_RealtimeDisplayMode_SetView(ConstPointer(), view.ConstPointer());
		}

		/// <summary>
		/// Get ViewInfo that has been registered with this RealtimeDisplayMode instance.
		/// </summary>
		/// <returns></returns>
		[CLSCompliant(false)]
		public ViewInfo GetView()
		{
			var viewpointer = UnsafeNativeMethods.Rdk_RealtimeDisplayMode_GetView(ConstPointer());
			return new ViewInfo(viewpointer, true);
		}

		/// <summary>
		/// Returns the LinearWorkflow data for this realitime display mode.
		/// </summary>
		public LinearWorkflow LinearWorkflow
		{
			get
			{
				var lwfpointer = UnsafeNativeMethods.Rdk_RealtimeDisplayMode_LinearWorkflow(ConstPointer());
				return new LinearWorkflow(lwfpointer);
			}
		}


		static private Dictionary<IntPtr, RealtimeDisplayMode> g_instances = new Dictionary<IntPtr, RealtimeDisplayMode>();

		/// <summary>
		/// Remove RealtimeDisplayMode instance from internal dictionary.
		/// </summary>
		/// <param name="realtimeViewport">IntPtr to the RealtimeDisplayMode instance to remove.</param>
		static public void RemoveRealtimeViewport(IntPtr realtimeViewport)
		{
			if (g_instances.ContainsKey(realtimeViewport)) g_instances.Remove(realtimeViewport);
		}

		/// <summary>
		/// Retrieve RealtimeDisplayMode instance that the IntPtr refers to.
		/// </summary>
		/// <param name="realtimeViewport">IntPtr of the instance searched for. If
		/// the instance doesn't exist, a new one is created.
		/// </param>
		/// <returns></returns>
		static public RealtimeDisplayMode GetRealtimeViewport(IntPtr realtimeViewport)
		{
			return GetRealtimeViewport(realtimeViewport, true);
		}

		/// <summary>Retrieve RealtimeDisplayMode instance. If create is set to true
		/// then a new instance is created if not found, null is returned for false.
		/// </summary>
		/// <param name="realtimeViewport">IntPtr</param>
		/// <param name="create">true to create if not found, false to return null if not found.</param>
		/// <returns></returns>
		static public RealtimeDisplayMode GetRealtimeViewport(IntPtr realtimeViewport, bool create)
		{
			if(g_instances.ContainsKey(realtimeViewport))
			{
				return g_instances[realtimeViewport];
			}

			if (!create)
			{
				RealtimeDisplayMode rtdm = new RealtimeDisplayModePointerWrapper(realtimeViewport);
				return rtdm;
			}

			// no hit found, we need to instantiate a new one.
			var rtvp_id = UnsafeNativeMethods.Rdk_RealtimeDisplayMode_Uuid(realtimeViewport);

			Guid plugin_id;

			var rtvp_type = RdkPlugIn.GetRealtimeDisplayModeType(rtvp_id, out plugin_id);

			var inst = Activator.CreateInstance(rtvp_type) as RealtimeDisplayMode;
			if (inst == null)
				return null;

			inst.m_realtimeViewport = realtimeViewport;
			inst.PostConstruct();

			g_instances[realtimeViewport] = inst;

			return inst;
		}

		#region Client API

		internal delegate void CreateWorldCallback(IntPtr realtimeViewport, uint doc, IntPtr ViewInfo, IntPtr displayPipelineAttributes);
		internal static CreateWorldCallback m_on_create_world = OnCreateWorld;
		static private void OnCreateWorld(IntPtr realtimeViewport, uint doc, IntPtr viewInfo, IntPtr displayPipelineAttributes)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			rtvp.CreateWorld(RhinoDoc.FromRuntimeSerialNumber(doc), new ViewInfo(viewInfo, true), new DisplayPipelineAttributes(displayPipelineAttributes, true));
		}

		/// <summary>
		/// Implement if you need to handle the initial CreateWorld call initiated by the display pipeline system. Note
		/// that this is not the same as the CreateWorld call in Rhino.Render.ChangeQueue.ChangeQueue, although
		/// related.
		/// </summary>
		/// <param name="doc">Rhino document</param>
		/// <param name="viewInfo">active viewport info</param>
		/// <param name="displayPipelineAttributes">display pipeline attributes</param>
		public virtual void CreateWorld(RhinoDoc doc, ViewInfo viewInfo, DisplayPipelineAttributes displayPipelineAttributes)
		{

		}

		internal delegate void GetRenderSizeCallback(IntPtr realtimeViewport, out int width, out int height);
		internal static GetRenderSizeCallback m_on_get_rendersize = OnGetRenderSize;
		static private void OnGetRenderSize(IntPtr realtimeViewport, out int width, out int height)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			rtvp.GetRenderSize(out width, out height);
		}

		/// <summary>
		/// Get the current render resolution for the running render session.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public abstract void GetRenderSize(out int width, out int height);

		[return: MarshalAs(UnmanagedType.I1)]
		internal delegate bool StartRenderCallback(IntPtr realtimeViewport, uint w, uint h, uint docSerialNumber, IntPtr view, IntPtr viewPort, [MarshalAs(UnmanagedType.I1)]bool forCapture, IntPtr rw);
		internal static StartRenderCallback m_on_start_renderer = OnStartRenderer;
		static private bool OnStartRenderer(IntPtr realtimeViewport, uint w, uint h, uint docSerialNumber, IntPtr view, IntPtr viewPort, bool forCapture, IntPtr rw)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			RhinoDoc doc = RhinoDoc.FromRuntimeSerialNumber(docSerialNumber);
			ViewportInfo viewportInfo = new ViewportInfo(viewPort);
			RenderWindow renderWindow = new RenderWindow(rw);
			ViewInfo viewInfo = new ViewInfo(view, true);

			renderWindow.SetView(viewInfo);

			return rtvp.StartRenderer((int)w, (int)h, doc, viewInfo, viewportInfo, forCapture, renderWindow);
		}

		/// <summary>
		/// Override to start your render engine.
		/// 
		/// Note that before using the RenderWindow you *must* call SetSize
		/// to properly initialize the underlying DIB.
		/// </summary>
		/// <param name="w">Width of resolution</param>
		/// <param name="h">Height of resolution</param>
		/// <param name="doc">Rhino document</param>
		/// <param name="view">active view</param>
		/// <param name="viewportInfo">active viewport info</param>
		/// <param name="forCapture">true if renderer is started for capture purposes (ViewCaptureTo*), false for regular interactive rendering</param>
		/// <param name="renderWindow">RenderWindow to hold render results in.</param>
		/// <returns>Return true when your render engine started correctly, false when that failed</returns>
		public abstract bool StartRenderer(int w, int h, RhinoDoc doc, ViewInfo view, ViewportInfo viewportInfo,
			bool forCapture, RenderWindow renderWindow);

		internal delegate void ShutdownRenderCallback(IntPtr realtimeViewport);
		internal static ShutdownRenderCallback m_on_shutdown_renderer = OnShutdownRenderer;
		static private void OnShutdownRenderer(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			rtvp.ShutdownRenderer();
			RemoveRealtimeViewport(realtimeViewport);
		}

		/// <summary>
		/// Override to shutdown your render engine
		/// </summary>
		public abstract void ShutdownRenderer();

		internal delegate int LastRenderedPassCallback(IntPtr realtimeViewport);
		internal static LastRenderedPassCallback m_on_lastrenderedpass = OnLastRenderedPass;
		static private int OnLastRenderedPass(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport, false);
			if (rtvp == null || rtvp is RealtimeDisplayModePointerWrapper) return int.MinValue;
			return rtvp.LastRenderedPass();
		}

		/// <summary>
		/// Implement to communicate last completed pass to the underlying system.
		/// </summary>
		/// <returns>the last completed pass</returns>
		public virtual int LastRenderedPass()
		{
			return 0;
		}

		internal delegate bool ShowCaptureProgressCallback(IntPtr realtimeViewport);
		internal static ShowCaptureProgressCallback m_on_showcaptureprogress = OnShowCaptureProgress;
		static private bool OnShowCaptureProgress(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.ShowCaptureProgress();
		}

		/// <summary>
		/// Override if you want to i.e. hide the progress dialog for capture progress.
		/// </summary>
		/// <returns>Return true to show, false to hide</returns>
		public virtual bool ShowCaptureProgress()
		{
			return true;
		}

		internal delegate double CaptureProgressCallback(IntPtr realtimeViewport);
		internal static CaptureProgressCallback m_on_captureprogress = OnCaptureProgress;
		static private double OnCaptureProgress(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.CaptureProgress();
		}

		/// <summary>
		/// Override to communicate the progress of a capture.
		/// </summary>
		/// <returns>A number between 0.0 and 1.0 inclusive. 1.0 means 100%.</returns>
		public virtual double CaptureProgress()
		{
			return 0.5;
		}

		internal delegate bool RestartRenderCallback(IntPtr realtimeViewport, int width, int height);
		internal static RestartRenderCallback m_on_rendersize_changed = _OnRenderSizeChanged;
		static private bool _OnRenderSizeChanged(IntPtr realtimeViewport, int width, int height)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			return rtvp.OnRenderSizeChanged(width, height);
		}

		/// <summary>
		/// Override to restart your render engine
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public virtual bool OnRenderSizeChanged(int width, int height)
		{
			return false;
		}

		internal delegate bool IsRendererStartedCallback(IntPtr realtimeViewport);
		internal static IsRendererStartedCallback m_on_renderer_started = OnIsRendererStarted;
		static private bool OnIsRendererStarted(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp?.IsRendererStarted() ?? false;
		}

		/// <summary>
		/// Override to tell the started state of your render engine.
		/// </summary>
		/// <returns>true if render engine is ready and rendering</returns>
		public abstract bool IsRendererStarted();

		internal delegate bool IsRenderframeAvailableCallback(IntPtr realtimeViewport);
		internal static IsRenderframeAvailableCallback m_on_iscompleted = OnIsCompleted;
		static private bool OnIsCompleted(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			return rtvp.IsCompleted();
		}

		internal delegate bool IsFrameBufferAvailableCallback(IntPtr realtimeViewport, IntPtr viewInfo);
		internal static IsFrameBufferAvailableCallback m_on_framebuffer_available = OnIsFrameBufferAvailable;
		static private bool OnIsFrameBufferAvailable(IntPtr realtimeViewport, IntPtr viewInfo)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			return rtvp.IsFrameBufferAvailable(new ViewInfo(viewInfo, true));
		}

		/// <summary>
		/// Implement to tell the render pipeline that a framebuffer is ready
		/// </summary>
		/// <param name="view"></param>
		/// <returns>Return true when a framebuffer is ready. This is generally the
		/// case when StartRenderer as returned successfully.</returns>
		public abstract bool IsFrameBufferAvailable(ViewInfo view);

		/// <summary>
		/// Implement to tell if your render engine has completed a frame for drawing into the viewport
		/// </summary>
		/// <returns></returns>
		public abstract bool IsCompleted();

		internal delegate void OnDisplayPipelineSettingsChangedCallback(IntPtr realtimeViewport, IntPtr displayAttributes);
		internal static OnDisplayPipelineSettingsChangedCallback m_on_displaypipeline_settings_changed = _OnDisplayPipelineSettingsChanged;
		static private void _OnDisplayPipelineSettingsChanged(IntPtr realtimeViewport, IntPtr displayAttributes)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			var dpa = new DisplayPipelineAttributes(displayAttributes, true);


			rtvp.OnDisplayPipelineSettingsChanged?.Invoke(rtvp, new DisplayPipelineSettingsChangedEventArgs(dpa));
		}

		public class DisplayPipelineSettingsChangedEventArgs : EventArgs
		{
			public DisplayPipelineAttributes Attributes { get; private set; }
			public DisplayPipelineSettingsChangedEventArgs(DisplayPipelineAttributes dpa)
			{
				Attributes = dpa;
			}
		}

		public event EventHandler<DisplayPipelineSettingsChangedEventArgs> OnDisplayPipelineSettingsChanged;

		internal delegate void OnInitFramebufferCallback(IntPtr realtimeViewport, IntPtr displayAttributes);
		internal static OnInitFramebufferCallback m_on_initframebuffer = _OnInitFramebuffer;
		static private void _OnInitFramebuffer(IntPtr realtimeViewport, IntPtr displayPipeline)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			var dp = new DisplayPipeline(displayPipeline);


			rtvp.OnInitFramebuffer?.Invoke(rtvp, new InitFramebufferEventArgs(dp));
		}

		public class InitFramebufferEventArgs : EventArgs
		{
			public DisplayPipeline Pipeline { get; private set; }
			public InitFramebufferEventArgs(DisplayPipeline dp)
			{
				Pipeline = dp;
			}
		}

		public event EventHandler<InitFramebufferEventArgs> OnInitFramebuffer;


		internal delegate void OnDrawMiddlegroundCallback(IntPtr realtimeViewport, IntPtr displayAttributes);
		internal static OnDrawMiddlegroundCallback m_on_drawmiddleground = _OnDrawMiddleground;
		static private void _OnDrawMiddleground(IntPtr realtimeViewport, IntPtr displayPipeline)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			var dp = new DisplayPipeline(displayPipeline);


			rtvp.OnDrawMiddleground?.Invoke(rtvp, new DrawMiddlegroundEventArgs(dp));
		}

		public class DrawMiddlegroundEventArgs : EventArgs
		{
			public DisplayPipeline Pipeline { get; private set; }
			public DrawMiddlegroundEventArgs(DisplayPipeline dp)
			{
				Pipeline = dp;
			}
		}

		public event EventHandler<DrawMiddlegroundEventArgs> OnDrawMiddleground;

		public virtual bool DrawOpenGl() { return true; }
		internal delegate bool DrawOpenGlCallback(IntPtr realtimeViewport);
		internal static DrawOpenGlCallback m_drawopengl = _DrawOpenGl;

		static private bool _DrawOpenGl(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.DrawOpenGl();
		}

		/// <summary>
		/// Implement and return true if you want the display pipeline to not
		/// wait for IsFramebufferAvailable during the MiddleGround draw phase.
		/// This will also tell the pipeline to draw a complete middleground pass in OpenGL.
		/// </summary>
		public virtual bool UseFastDraw() { return false; }
		internal delegate bool UseFastDrawCallback(IntPtr realtimeViewport);
		internal static UseFastDrawCallback m_usefastdraw = _UseFastDraw;

		static private bool _UseFastDraw(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.UseFastDraw();
		}

		/// <summary>
		/// During run-time change whether to use OpenGL drawing of results or not. For instance
		/// offline rendering (viewcapture* with different resolution than viewport) could use
		/// RenderWindow instead of direct OpenGL drawing.
		/// </summary>
		/// <param name="use">Set to true if OpenGL drawing is wanted, set to false if RenderWindow
		/// method is needed.</param>
		public virtual void SetUseDrawOpenGl(bool use)
		{
			UnsafeNativeMethods.Rdk_RealtimeDisplayMode_SetUseDrawOpenGl(m_realtimeViewport, use);
		}

		public int OpenGlVersion()
		{
			return UnsafeNativeMethods.Rdk_RealtimeDisplayMode_OpenGlVersion(m_realtimeViewport);
		}

		#region HUD callbacks

		internal delegate void HudProductNameCallback(IntPtr realtimeViewport, IntPtr strIntPtr);
		internal static HudProductNameCallback m_on_hud_productname = OnHudProductName;

		private static void OnHudProductName(IntPtr realtimeViewport, IntPtr strIntPtr)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			var name = rtvp.HudProductName();

			if (!String.IsNullOrEmpty(name))
			{
				UnsafeNativeMethods.ON_wString_Set(strIntPtr, name);
			}
		}

		/// <summary>
		/// Override to return the name of your product. This will be printed in
		/// the HUD.
		/// </summary>
		/// <returns>Name of the product.</returns>
		public virtual string HudProductName()
		{
			return "";
		}

		internal delegate void HudCustomStatusTextCallback(IntPtr realtimeViewport, IntPtr strIntPtr);
		internal static HudCustomStatusTextCallback m_on_hud_statustext = OnHudCustomStatusText;

		private static void OnHudCustomStatusText(IntPtr realtimeViewport, IntPtr strIntPtr)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			var text = rtvp.HudCustomStatusText();
			if (!string.IsNullOrEmpty(text))
			{
				UnsafeNativeMethods.ON_wString_Set(strIntPtr, text);
			}
		}

		/// <summary>
		/// Override to display status of the render engine.
		/// </summary>
		/// <returns>Status text to display</returns>
		public virtual string HudCustomStatusText()
		{
			return "";
		}
		
		internal delegate int HudMaximumPassesCallback(IntPtr realtimeViewport);
		internal static HudMaximumPassesCallback m_on_hud_maximumpasses = OnHudMaximumPasses;

		private static int OnHudMaximumPasses(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudMaximumPasses();
		}

		/// <summary>
		/// Override to communicate the maximum passes count currently in use for the render session.
		/// Can be shown in the HUD
		/// </summary>
		/// <returns>Maximum passes</returns>
		public virtual int HudMaximumPasses()
		{
			return -1;
		}

		internal delegate int HudLastRenderedPassCallback(IntPtr realtimeViewport);
		internal static HudLastRenderedPassCallback m_on_hud_lastrenderedpass = OnHudLastRenderedPass;

		private static int OnHudLastRenderedPass(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudLastRenderedPass();
		}

		/// <summary>
		/// Override to communicate the last completed pass. Can be shown in the HUD
		/// </summary>
		/// <returns>Last completed pass</returns>
		public virtual int HudLastRenderedPass()
		{
			return -1;
		}

		internal delegate bool HudRendererPausedCallback(IntPtr realtimeViewport);
		internal static HudRendererPausedCallback m_on_hud_rendererpaused = OnHudRendererPaused;

		private static bool OnHudRendererPaused(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudRendererPaused();
		}

		/// <summary>
		/// Implement to support pausing and resuming in the viewport
		/// </summary>
		/// <returns>Return true if the render engine is paused.</returns>
		public virtual bool HudRendererPaused()
		{
			return false;
		}

		internal delegate bool HudRendererLockedCallback(IntPtr realtimeViewport);
		internal static HudRendererLockedCallback m_on_hud_rendererlocked = OnHudRendererLocked;

		private static bool OnHudRendererLocked(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudRendererLocked();
		}

    /// <summary>
    /// Implement to support locking in the viewport
    /// </summary>
    /// <returns>Return true if the render engine is locked.</returns>
    public virtual bool HudRendererLocked()
		{
			return false;
		}

		internal delegate bool HudAllowEditMaxPassesCallback(IntPtr realtimeViewport);
		internal static HudAllowEditMaxPassesCallback m_on_hud_allow_edit_maxpasses = OnHudAllowEditMaxPasses;

		private static bool OnHudAllowEditMaxPasses(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudAllowEditMaxPasses();
		}

		/// <summary>
		/// Override to allow maximum pass editing. By default disabled.
		/// </summary>
		/// <returns>Return true to allow users to edit the maximum pass count.</returns>
		public virtual bool HudAllowEditMaxPasses()
		{
			return false;
		}

		internal delegate bool HudShowMaxPassesCallback(IntPtr realtimeViewport);
		internal static HudShowMaxPassesCallback m_on_hud_showmaxpasses = OnHudShowMaxPasses;

		private static bool OnHudShowMaxPasses(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudShowMaxPasses();
		}

		/// <summary>
		/// Override to show maximum passes in HUD. By default disabled.
		/// </summary>
		/// <returns>Return true to show maximum passes.</returns>
		public virtual bool HudShowMaxPasses()
		{
			return false;
		}

		internal delegate bool HudShowPassesCallback(IntPtr realtimeViewport);
		internal static HudShowPassesCallback m_on_hud_showpasses = OnHudShowPasses;

		private static bool OnHudShowPasses(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudShowPasses();
		}

		/// <summary>
		/// Override to show current pass in HUD. By default disabled.
		/// </summary>
		/// <returns>Return true to show current pass in HUD.</returns>
		public virtual bool HudShowPasses()
		{
			return false;
		}

		internal delegate bool HudShowCustomStatusTextCallback(IntPtr realtimeViewport);
		internal static HudShowCustomStatusTextCallback m_on_hud_showstatustext = OnHudShowCustomStatusText;

		private static bool OnHudShowCustomStatusText(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudShowCustomStatusText();
		}

		/// <summary>
		/// Override to show status text in HUD. By default disabled.
		/// </summary>
		/// <returns>Return true to show status text in HUD</returns>
		public virtual bool HudShowCustomStatusText()
		{
			return false;
		}

		internal delegate bool HudShowControlsCallback(IntPtr realtimeViewport);
		internal static HudShowControlsCallback m_on_hud_showcontrols = OnHudShowControls;

		private static bool OnHudShowControls(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudShowControls();
		}

		/// <summary>
		/// Show control buttons on the realtime display HUD.
		/// 
		/// By default these are shown, override this function and
		/// return false if HUD controls aren't needed.
		/// </summary>
		/// <returns></returns>
		public virtual bool HudShowControls()
		{
			return true;
		}

		internal delegate bool HudShowCallback(IntPtr realtimeViewport);
		internal static HudShowCallback m_on_hud_show = OnHudShow;

		private static bool OnHudShow(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			return rtvp.HudShow();
		}

		/// <summary>
		/// Override if you want to hide the HUD. Shown by default
		/// </summary>
		/// <returns>Return false to hide the HUD.</returns>
		public virtual bool HudShow()
		{
			return true;
		}

		internal delegate long HudStartTimeCallback(IntPtr realtimeViewport);
		internal static HudStartTimeCallback m_on_hud_starttime = OnHudStartTime;

		private static long OnHudStartTime(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			var start_time = rtvp.HudStartTime();
			TimeSpan span = (start_time - in_the_beginning);
			return (long) span.TotalSeconds;
		}

		/// <summary>
		/// Begin of epoch for timestamp calculation.
		/// </summary>
		private static DateTime in_the_beginning = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		public virtual DateTime HudStartTime()
		{
			return in_the_beginning;
		}

		internal delegate void HudButtonPressed(IntPtr realtimeViewport);

		internal static HudButtonPressed m_on_hud_playbutton_pressed = _OnHudPlayButtonPressed;

		private static void _OnHudPlayButtonPressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudPlayButtonPressed?.Invoke(rtvp, EventArgs.Empty);
		}

		/// <summary>
		/// Listen to this event if you want to handle the play button control.
		/// </summary>
		public event EventHandler HudPlayButtonPressed;

		internal static HudButtonPressed m_on_hud_pausebutton_pressed = _OnHudPauseButtonPressed;

		private static void _OnHudPauseButtonPressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudPauseButtonPressed?.Invoke(rtvp, EventArgs.Empty);
		}
		/// <summary>
		/// Listen tot his event if you want to handle the pause button control
		/// </summary>
		public event EventHandler HudPauseButtonPressed;

		internal static HudButtonPressed m_on_hud_lockbutton_pressed = _OnHudLockButtonPressed;

		private static void _OnHudLockButtonPressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudLockButtonPressed?.Invoke(rtvp, EventArgs.Empty);
		}

		/// <summary>
		/// Listen tot his event if you want to handle the lock button control
		/// </summary>
		public event EventHandler HudLockButtonPressed;


		/// <summary>
		/// Listen tot his event if you want to handle the unlock button control
		/// </summary>
		public event EventHandler HudUnlockButtonPressed;
		internal static HudButtonPressed m_on_hud_unlockbutton_pressed = _OnHudUnlockButtonPressed;

		private static void _OnHudUnlockButtonPressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudUnlockButtonPressed?.Invoke(rtvp, EventArgs.Empty);
		}
		/// <summary>
		/// Listen tot his event if you want to handle a press on the product name component
		/// </summary>
		public event EventHandler HudProductNamePressed;
		internal static HudButtonPressed m_on_hud_productname_pressed = _OnHudProductNamePressed;

		private static void _OnHudProductNamePressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudProductNamePressed?.Invoke(rtvp, EventArgs.Empty);
		}

		/// <summary>
		/// Listen tot his event if you want to handle a press on the status text component
		/// </summary>
		public event EventHandler HudStatusTextPressed;
		internal static HudButtonPressed m_on_hud_statustext_pressed = _OnHudStatusTextPressed;

		private static void _OnHudStatusTextPressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudStatusTextPressed?.Invoke(rtvp, EventArgs.Empty);
		}

		/// <summary>
		/// Listen tot his event if you want to handle a press press on the time component
		/// </summary>
		public event EventHandler HudTimePressed;
		internal static HudButtonPressed m_on_hud_time_pressed = _OnHudTimePressed;

		private static void _OnHudTimePressed(IntPtr realtimeViewport)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);
			rtvp.HudTimePressed?.Invoke(rtvp, EventArgs.Empty);
		}

		internal delegate void HudMaxPassesChanged(IntPtr realtimeViewport, int mp);
		internal static HudMaxPassesChanged m_on_hud_max_passes_changed = _OnHudMaxPassesChanged;

		private static void _OnHudMaxPassesChanged(IntPtr realtimeViewport, int mp)
		{
			var rtvp = GetRealtimeViewport(realtimeViewport);

			rtvp.MaxPassesChanged?.Invoke(rtvp, new HudMaxPassesChangedEventArgs(mp));
		}

		public class HudMaxPassesChangedEventArgs : EventArgs
		{
			public int MaxPasses { get; private set; }
			public HudMaxPassesChangedEventArgs(int mp)
			{
				MaxPasses = mp;
			}
		}

    public bool Paused
    {
      get
      {
        return HudRendererPaused();
      }
      set 
      {
        UnsafeNativeMethods.Rdk_RealtimeDisplayMode_SetPaused(m_realtimeViewport, value);
      }
    }

    public bool Locked
    {
      get
      {
        return HudRendererLocked();
      }
      set 
      {
        UnsafeNativeMethods.Rdk_RealtimeDisplayMode_SetLocked(m_realtimeViewport, value);
      }
    }

    public int MaxPasses
    {
      get
      {
        return HudMaximumPasses();
      }
      set 
      {
        UnsafeNativeMethods.Rdk_RealtimeDisplayMode_SetMaxPasses(m_realtimeViewport, value);
      }
    }

    /// <summary>
    /// Listen to this if you want to handle changes in maximum pass count through
    /// the HUD.
    /// </summary>
    public event EventHandler<HudMaxPassesChangedEventArgs> MaxPassesChanged;
		#endregion

		#endregion
	}
}
