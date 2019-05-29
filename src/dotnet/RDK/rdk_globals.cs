#pragma warning disable 1591
#if RHINO_SDK
using System;
using Rhino.Runtime.InteropWrappers;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.ComponentModel;

namespace Rhino.Render
{
  public static class Utilities
  {
    /// <summary>
    /// Set default render application
    /// </summary>
    /// <param name="pluginId">ID of render plug-in</param>
    /// <returns>
    /// True if plug-in found and loaded successfully.  False if pluginId is
    ///  invalid or was unable to load plug-in
    /// </returns>
    public static bool SetDefaultRenderPlugIn(Guid pluginId)
    {
      return UnsafeNativeMethods.CRhinoApp_SetDefaultRenderApp(pluginId);
    }

    /// <summary>
    /// Get the plug-in Id for the default render plug-in
    /// </summary>
    public static Guid DefaultRenderPlugInId
    {
      get
      {
        return UnsafeNativeMethods.CRhinoApp_GetDefaultRenderApp();
      }
    }


    internal static bool ShowIncompatibleContent(RenderContentKind kind) { return 1 == UnsafeNativeMethods.Rdk_Globals_ShowIncompatibleContent(RenderContent.KindString(kind)); }
    internal static void SetShowIncompatibleContent(RenderContentKind kind, bool bShow) { UnsafeNativeMethods.Rdk_Globals_SetShowIncompatbileContent(RenderContent.KindString(kind), bShow); }

    /// <summary>
    /// Specifies whether incompatible content should be shown in the corresponding editor.
    /// </summary>
    public static bool ShowIncompatibleMaterials
    {
      get { return ShowIncompatibleContent(RenderContentKind.Material); }
      set { SetShowIncompatibleContent(RenderContentKind.Material, value); }
    }

    /// <summary>
    /// Specifies whether incompatible content should be shown in the corresponding editor.
    /// </summary>
    public static bool ShowIncompatibleEnvironments
    {
      get { return ShowIncompatibleContent(RenderContentKind.Environment); }
      set { SetShowIncompatibleContent(RenderContentKind.Environment, value); }
    }

    /// <summary>
    /// Specifies whether incompatible content should be shown in the corresponding editor.
    /// </summary>
    public static bool ShowIncompatibleTextures
    {
      get { return ShowIncompatibleContent(RenderContentKind.Texture); }
      set { SetShowIncompatibleContent(RenderContentKind.Texture, value); }
    }

    /// <summary>
    /// Queries whether or not the Safe Frame is visible.
    /// </summary>
    public static bool SafeFrameEnabled(RhinoDoc doc)
    {
      if (doc == null)
        throw new ArgumentNullException("doc");
      return 1 == UnsafeNativeMethods.Rdk_Globals_IsSafeFrameVisible((int)doc.RuntimeSerialNumber);
    }

    /*
    public enum ChooseContentFlags : int
    {
      /// <summary>
      /// Dialog will have [New...] button.
      /// </summary>
	    NewButton  = 1,
      /// <summary>
      /// Dialog will have [Edit...] button.
      /// </summary>
	    EditButton = 2,
    };
    
    /// <summary>
    /// Allows the user to choose a content by displaying the Content Chooser dialog.
	  /// The dialog will have OK, Cancel and Help buttons, and optional New and Edit buttons.
    /// </summary>
    /// <param name="instanceId">Sets the initially selected content and receives the instance id of the chosen content.</param>
    /// <param name="kinds">Specifies the kind(s) of content that should be displayed in the chooser.</param>
    /// <param name="flags">Specifies flags controlling the browser.</param>
    /// <param name="doc">A Rhino document.</param>
    /// <returns>true if the operation succeeded.</returns>
    public static bool ChooseContent(ref Guid instanceId, RenderContentKind kinds, ChooseContentFlags flags, RhinoDoc doc)
    {
      return 1 == UnsafeNativeMethods.Rdk_Globals_ChooseContentEx(ref instanceId, RenderContent.KindString(kinds), (int)flags, doc.RuntimeSerialNumber);
    }
    */

       

    /*
    /// <summary>
    /// Constructs a new content chosen by the user and add it to the persistent content list.
	  /// This function cannot be used to create temporary content that you delete after use.
	  /// Content created by this function is owned by RDK and appears in the content editor.
    /// </summary>
    /// <param name="defaultType">The default content type.</param>
    /// <param name="defaultInstance">The default selected content instance.</param>
    /// <param name="kinds">Determines which content kinds are allowed to be chosen from the dialog.</param>
    /// <param name="flags">Options for the tab.</param>
    /// <param name="doc">The current Rhino document.</param>
    /// <returns>A new persistent render content.</returns>
    public static RenderContent CreateContentByUser(Guid defaultType, Guid defaultInstance, RenderContentKind kinds, ChooseContentFlags flags, RhinoDoc doc)
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_Globals_CreateContentByUser(defaultType, defaultInstance, RenderContent.KindString(kinds), (int)flags, doc.RuntimeSerialNumber);
      return pContent == IntPtr.Zero ? null : RenderContent.FromPointer(pContent);
    }
    */

    /// <summary>
    /// Changes the type of a content. This deletes the content and creates a replacement
	  /// of the specified type allowing the caller to decide about harvesting.
    /// </summary>
    /// <param name="oldContent">oldContent is the old content which is deleted.</param>
    /// <param name="newType">The type of content to replace pOldContent with.</param>
    /// <param name="harvestParameters">Determines whether or not parameter harvesting will be performed.</param>
    /// <returns>A new persistent render content.</returns>
    public static RenderContent ChangeContentType(RenderContent oldContent, Guid newType, bool harvestParameters)
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_Globals_ChangeContentType(oldContent.NonConstPointer(), newType, harvestParameters);
      return IntPtr.Zero==pContent ? null : RenderContent.FromPointer(pContent);
    }

    /// <summary>
    /// Prompts the user for a save file name and the width, height and depth of an image to be saved.
    /// </summary>
    /// <param name="filename">The original file path.</param>
    /// <param name="width">A width.</param>
    /// <param name="height">An height.</param>
    /// <param name="colorDepth">A color depth.</param>
    /// <returns>The new file name.</returns>
    public static string PromptForSaveImageFileParameters(string filename, ref int width, ref int height, ref int colorDepth)
    {
      using (var sh = new StringHolder())
      {
        IntPtr pString = sh.NonConstPointer();
        bool bRet = 1==UnsafeNativeMethods.Rdk_Globals_PromptForSaveImageFileParams(filename, ref width, ref height, ref colorDepth, pString);
        
        if (bRet)
          return sh.ToString();
      }
      return null;      
    }

    /// <summary>
    /// Loads a content from a library file and adds it to the persistent content list of a particular document.
    /// </summary>
    /// <param name="docSerialNumber">identifies the document into which the content should be loaded.</param>
    /// <param name="filename">is the full path to the file to be loaded.</param>
    /// <returns>The loaded content or null if an error occurred.</returns>
    [CLSCompliant(false)]
    public static RenderContent LoadPersistentRenderContentFromFile(uint docSerialNumber, String filename)
    {
      IntPtr pContent = UnsafeNativeMethods.Rdk_RenderContent_LoadPersistentContentFromFile(docSerialNumber, filename);
      return IntPtr.Zero == pContent ? RenderContent.FromPointer(pContent) : null;
    }


    public enum ShowContentChooserResults
    {
      /// <summary>
      /// No choice (user cancelled).
      /// </summary>
	    None,
      /// <summary>
      /// User chose from 'New' tab. uuidOut is the type id.
      /// </summary>
	    New,
      /// <summary>
      /// User chose from 'Existing' tab with 'copy' radio button checked. uuidOut is the instance id.
      /// </summary>
	    Copy,
      /// <summary>
      /// User chose from 'Existing' tab with 'instance' radio button checked. uuidOut is the instance id.
      /// </summary>
	    Instance, 
    };

    /*
    /// <summary>
    /// Shows the content chooser to allow the user to select a new or existing content.
    /// </summary>
    /// <param name="defaultType">The content type that will be initially selected in the 'New' tab.</param>
    /// <param name="defaultInstanceId">The content instance that will be initially selected in the 'Existing' tab.</param>
    /// <param name="kinds">Which content kinds will be displayed.</param>
    /// <param name="instanceIdOut">The UUID of the chosen item. Depending on eRhRdkSccResult, this can be the type id of a content type or the instance id of an existing content.</param>
    /// <param name="flags">Tabs specifications.</param>
    /// <param name="doc">A Rhino document.</param>
    /// <returns>The result.</returns>
    public static ShowContentChooserResults ShowContentChooser(Guid defaultType, Guid defaultInstanceId, RenderContentKind kinds, ref Guid instanceIdOut, ShowContentChooserFlags flags, RhinoDoc doc)
    {
      return (ShowContentChooserResults)UnsafeNativeMethods.Rdk_Globals_ShowContentChooser(defaultType, defaultInstanceId, RenderContent.KindString(kinds), ref instanceIdOut, (int)flags, doc.RuntimeSerialNumber);
    }
    */
 
    /// <summary>
    /// Finds a file and also handles network shares.
    /// <remarks>This is a replacement for CRhinoFileUtilities::FindFile().</remarks>
    /// </summary>
    /// <param name="doc">Document to use for locating .3dm file's folder.</param>
    /// <param name="fullPathToFile">The file to be found.</param>
    /// <returns>The found file.</returns>
    [CLSCompliant(false)]
    public static string FindFile(RhinoDoc doc, string fullPathToFile)
    {
      if (doc == null)
        throw new ArgumentNullException("doc");
      using (var sh = new StringHolder())
      {
        var  pointer = sh.NonConstPointer();
        var found = (1 == UnsafeNativeMethods.Rdk_Globals_FindFile(doc.RuntimeSerialNumber, fullPathToFile, true, pointer));

        return found ? sh.ToString() : null;
      }
    }

    /// <summary>
    /// Finds a file and also handles network shares.
    /// <remarks>This is a replacement for CRhinoFileUtilities::FindFile().</remarks>
    /// </summary>
    /// <param name="doc">Document to use for locating .3dm file's folder.</param>
    /// <param name="fullPathToFile">The file to be found.</param>
    /// <param name="unpackFromBitmapTableIfNecessary">True to seasch for the file in the bitmap table and unpack it into the temp folder if not found in the initial search.</param>
    /// <returns>The found file.</returns>
    [CLSCompliant(false)]
    public static string FindFile(RhinoDoc doc, string fullPathToFile, bool unpackFromBitmapTableIfNecessary)
    {
      if (doc == null)
        throw new ArgumentNullException("doc");
      using (var sh = new StringHolder())
      {
        var pointer = sh.NonConstPointer();
        var found = (1 == UnsafeNativeMethods.Rdk_Globals_FindFile(doc.RuntimeSerialNumber, fullPathToFile, unpackFromBitmapTableIfNecessary, pointer));

        return found ? sh.ToString() : null;
      }
    }

    /// <summary>
    /// Determines if any texture in any persistent content list is using the specified file name for caching.
    /// </summary>
    /// <param name="textureFileName">The file name to check for. The extension is ignored.</param>
    /// <returns>true if the texture is present.</returns>
    public static bool IsCachedTextureFileInUse(string textureFileName)
    {
      return 1 == UnsafeNativeMethods.Rdk_Globals_IsCachedTextureFileInUse(textureFileName);
    }

    public static void MoveWindow(IntPtr hwnd, System.Drawing.Rectangle rect, bool bRepaint, bool bRepaintNC)
    {
        UnsafeNativeMethods.Rdk_Globals_MoveWindow(hwnd, rect.Left, rect.Top, rect.Width, rect.Height, bRepaint, bRepaintNC);
    }

    /// <summary>
    /// Display and track the context menu.
    /// </summary>
    /// <param name="hwnd">The window that displays the menu, for example an edit box.</param>
    /// <param name="pt">The position to display the menu at inside the window</param>
    /// <param name="outIOR">Accepts the IOR value of the user's chosen substance</param>
    /// <param name="outString">Accepts the name of the user's chosen substance. Can be null if not required.</param>
    /// <returns>true if the function showed the IOR menu and something was picked.</returns>
    public static bool ShowIORMenu(IntPtr hwnd, System.Drawing.Point pt, ref double outIOR, ref string outString)
    {
        if (outString == null)
        {
            return UnsafeNativeMethods.Rdk_Globals_TrackIORMenu(hwnd, pt.X, pt.Y, ref outIOR, IntPtr.Zero);
        }
        else
        {
            using (var sh = new StringHolder())
            {
                var pointer = sh.NonConstPointer();
                if (UnsafeNativeMethods.Rdk_Globals_TrackIORMenu(hwnd, pt.X, pt.Y, ref outIOR, pointer))
                {
                    outString = sh.ToString();
                    return true;
                }
            }
        }
        return false;
    }

    //TODO
    /* \return A reference to RDK's collection of registered content I/O plug-ins. */
    //RHRDK_SDK const IRhRdkContentIOPlugIns& RhRdkContentIOPlugIns(void);

    //TODO
    //RHRDK_SDK IRhRdkRegisteredPropertyManager& RhRdkRegisteredPropertiesManager(void);

    //TODO
    //RHRDK_SDK CRhRdkRenderPlugIn* FindCurrentRenderPlugIn(void);

    //TODO
    //RHRDK_SDK void RhRdkCopySun(IRhRdkSun& dest, const IRhRdkSun& srce);

    //TODO
    //RHRDK_SDK ON_BoundingBox RhRdkGetCRMBoundingBox(const IRhRdkCustomRenderMeshes& meshes);

    /* \return A reference to RDK's custom render mesh manager. */
    //RHRDK_SDK IRhRdkCustomRenderMeshManager& RhRdkCustomRenderMeshManager(void);

    /* Create a new texture from a HBITMAP (which should be a DIBSECTION).
	\param hBitmap is the bitmap to create the texture from.
	\param bAllowSimulation determines whether simulation of the texture into a temporary bitmap is allowed.
	\param bShared determines whether ownership is passed to RDK. If bShared is \e false, you must call
	       ::DeleteObject() on hBitmap at some convenient time. If bShared is \e true, RDK will delete it
	       when the texture is deleted. You can use this parameter to share bitmaps between textures.
	\return A pointer to the texture. Never NULL. */
    //RHRDK_SDK CRhRdkTexture* RhRdkNewDibTexture(HBITMAP hBitmap, bool bShared=false, bool bAllowSimulation=true);
    //RHRDK_SDK CRhRdkTexture* RhRdkNewDibTexture(CRhinoDib* pDib, bool bShared=false, bool bAllowSimulation=true);

    /* Get an interface to an automatic UI. The caller shall delete the interface
      when it is no longer required.
      \param pParent is the parent window which <b>must not be NULL</b>.
      \param style specifies the visual style of the UI.
      \return Interface to automatic UI. This will be NULL only if RDK or plug-in is
      not correctly initialized. */
    //RHRDK_SDK IRhRdkAutomaticUI* RhRdkNewAutomaticUI(CWnd* pParent, IRhRdkAutomaticUI::eStyle style);
  }

  internal static class InternalUtilities
  {
    enum RCPResult
    {
      Rendering, // A preview job was issued to do the rendering using the current renderer.
      CacheOK,   // cacheOut contains a cached preview.
      CacheFail, // There was a preview in the cache but it couldn't be loaded.
      Nothing,   // The function did nothing because a new preview was not required or impossible to create.
    };


    /// <summary>
    /// Generate a render content preview
    /// </summary>
    /// <param name="lwf">Linear Workflow</param>
    /// <param name="c">Render Content</param>
    /// <param name="width">Image width</param>
    /// <param name="heigth">Image heigth</param>
    /// <param name="bSuppressLocalMapping">Suppress Local Mapping</param>
    /// <param name="pjs">Preivew Job Signature</param>
    /// <param name="pa">Preivew Appearance</param>
    /// <param name="rValue">Reference to RCPResult value</param>
    /// <returns>The Bitmap of the render content preview</returns>
    internal static System.Drawing.Bitmap GenerateRendercontentPreview(LinearWorkflow lwf, RenderContent c, int width, int heigth, bool bSuppressLocalMapping, PreviewJobSignature pjs, PreviewAppearance pa, ref uint rValue)
    {
      IntPtr pDib = UnsafeNativeMethods.Rdk_Globals_GenerateRenderedContentPreview(lwf.CppPointer, c.CppPointer, width, heigth, bSuppressLocalMapping, pjs.CppPointer, pa.CppPointer, ref rValue);

      if (pDib == IntPtr.Zero)
        return null;

      if (rValue == (uint)RCPResult.Rendering || rValue == (uint)RCPResult.CacheFail || rValue == (uint)RCPResult.Nothing)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    /// <summary>
    /// Generate a quick render content preview
    /// </summary>
    /// <param name="c">Render Content</param>
    /// <param name="width">Image width</param>
    /// <param name="heigth">Image heigth</param>
    /// <param name="psc">PreviewSceneServer</param>
    /// <param name="bSuppressLocalMapping">SuppressLocalMapping</param>
    /// <param name="reason"> ContentChanged = 0, ViewChanged = 1, RefreshDisplay = 2, Other = 99</param>
    /// <param name="rValue">bool value for successfull quick image creation</param>
    /// <returns>The Bitmap of the quick render content preview</returns>
    internal static System.Drawing.Bitmap GenerateQuickContentPreview(RenderContent c, int width, int heigth, PreviewSceneServer psc, bool bSuppressLocalMapping, int reason, ref uint rValue)
    {
      if (c == null)
        return null;

      IntPtr pPreviewSceneServer = IntPtr.Zero;
      if (psc != null)
        pPreviewSceneServer = psc.CppPointer;

      IntPtr pDib = UnsafeNativeMethods.Rdk_Globals_GenerateQuickContentPreview(c.CppPointer, width, heigth, pPreviewSceneServer, bSuppressLocalMapping, reason, ref rValue);

      if (pDib == IntPtr.Zero)
        return null;

      if (rValue == 0)
        return null;

      var bitmap = Rhino.Runtime.InteropWrappers.RhinoDib.ToBitmap(pDib, true);
      return bitmap;
    }

    /// <summary>
    /// Get the In Use Marker state of content. Used by thumbnaillist to draw the corners and set selected color in UI
    /// </summary>
    /// <param name="c">Render Content</param>
    /// <param name="colors">Selected color</param>
    /// <param name="bIsMarkedOut">True if content is in use, otherwise false</param>
    internal static void GetInUseMarkerState(RenderContent c, ref List<Display.Color4f> colors, ref bool bIsMarkedOut)
    {
      Display.Color4f color1 = new Display.Color4f();
      Display.Color4f color2 = new Display.Color4f();
      Display.Color4f color3 = new Display.Color4f();
      Display.Color4f color4 = new Display.Color4f();

      UnsafeNativeMethods.Rdk_Globals_GetInUseMarkerState(c.CppPointer, ref color1, ref color2, ref color3, ref color4, ref bIsMarkedOut);

      colors.Clear();
      colors.Add(color1);
      colors.Add(color2);
      colors.Add(color3);
      colors.Add(color4);
    }

    internal delegate int RhCsDownloadFileProc(IntPtr pUrl, IntPtr pPath);
    internal static RhCsDownloadFileProc OnDownloadFileProc = OnDownloadFile;
    static int OnDownloadFile(IntPtr pUrl, IntPtr pPath)
    {
      var url  = StringWrapper.GetStringFromPointer (pUrl);
      var path = StringWrapper.GetStringFromPointer (pPath);

      using (var webClient = new WebClient())
      {
        try
        {
          webClient.DownloadFile(new Uri(url), path);
          return 1;
        }
        catch (WebException)
        {
          return 0;
        }
      }
    }

    internal delegate int RhCsUrlResponseProc(IntPtr pUrl, IntPtr response);
    internal static RhCsUrlResponseProc OnUrlResponseProc = OnUrlResponse;
    static int OnUrlResponse(IntPtr pUrl, IntPtr response)
    {
        if (response == IntPtr.Zero)
          return 0;

        var url = StringWrapper.GetStringFromPointer (pUrl);

        using (var webClient = new WebClient())
        {
            try
            {
                var res = webClient.DownloadString(url);
                StringWrapper.SetStringOnPointer(response, res);
                return 1;
            }
            catch (WebException)
            {
                return 0;
            }
        }
    }


    /// <summary>
    /// Checks if material is non rdk material
    /// </summary>
    /// <param name="mat">Material</param>
    /// <returns>Returns true if material is non rdk material, otherwise false</returns>
    internal static bool RdkIsNonRDKPlugInMaterial(Rhino.DocObjects.Material mat)
    {
      return UnsafeNativeMethods.RdkIsNonRDKPlugInMaterial(mat.ConstPointer());
    }

    internal static class RdkPlugin
    {
      internal static Guid Uuid => new Guid("16592D58-4A2F-401D-BF5E-3B87741C1B1B");
    }
  }
}

#endif