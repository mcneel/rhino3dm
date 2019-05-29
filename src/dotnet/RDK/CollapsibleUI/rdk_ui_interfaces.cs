#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Drawing;



namespace Rhino.UI.Controls
{
  namespace DataSource
  {
    public class ProviderIds
    {
      //Returned object is Rhino.Render.Sun
      public static Guid Sun { get { return Guid.Parse("CCF5B291-C754-40C3-9C1F-CC387BBE2CF1"); } }
      //Returned object is Rhino.Render.ICurrentEnvironment
      public static Guid CurrentEnvironment { get { return Guid.Parse("B568FD6C-40BC-42F0-9F9C-ACA6551BB437"); } }
      //Returned object is Rhino.Render.RhinoSettings
      public static Guid RhinoSettings { get { return Guid.Parse("808D9A08-99BE-4DFD-AECC-D0F552826844"); } }
      //Returned object is Rhino.Render.Dithering
      public static Guid Dithering { get { return Guid.Parse("74228A33-47A4-4C75-8B06-B80C9BB71BFC"); } }
      //Returned object is Rhino.Render.LinearWorkflow
      public static Guid LinearWorkflow { get { return Guid.Parse("05A8ECD2-1C99-4F9E-95EB-DA4EA19E3AE2"); } }
      //Returned object is Rhino.Render.Skylight
      public static Guid Skylight { get { return Guid.Parse("BF885063-6FE2-4676-B96D-BF0357D6E337"); } }
      //Returned object is Rhino.Render.GroundPlane
      public static Guid GroundPlane { get { return Guid.Parse("2E258DDB-05B7-44BA-BE40-5EEA1DC50527"); } }
      //Returned object is RdkUndoRecord
      public static Guid UndoRecord { get { return Guid.Parse("8FA69F0E-7B1C-4186-86C9-003822903820"); } }
      //Returned object is RdkUndo
      public static Guid Undo { get { return Guid.Parse("cd715747-30f3-4cef-a486-a48a799178e5"); } }
      //Returned object is RenderContent
      public static Guid ContentLookup { get { return Guid.Parse("E9AD1AFB-AC51-4801-AEEF-A03823CE3FD9"); } }
      //Returned object is RenderContentCollection
      public static Guid ContentDisplayCollection { get { return Guid.Parse("542e4e4f-14cc-4249-a179-15c02c5201de"); } }
      //Returned object is RenderContentCollection
      public static Guid ContentDatabase { get { return Guid.Parse("E186831C-D5F0-419B-BD0E-70080A7B07D4"); } }
      //Returned object is RenderContentCollection
      public static Guid ContentSelection { get { return Guid.Parse("0FA74E68-3833-4EBE-A444-9C4EB4B04DB8"); } }
      //Returned object is ContentPreviewRenderedEventInfo
      public static Guid ContentPreviewRendered { get { return Guid.Parse("ebda6cae-369e-49b5-998f-b48f338ae1f5"); } }
      //Returned object is ImageFileInfo
      public static Guid ImageFileInfo { get { return Guid.Parse("d46972bb-ea3a-4e8d-9ed3-f19dffeb9bae"); } }
      public static Guid ContentParam { get { return Guid.Parse("0eaa388c-3f2c-47c6-84cb-3b55c5e20ace"); } }
      public static Guid NamedItem { get { return Guid.Parse("39fe6489-f61d-4d73-a895-00b14acb2a7f"); } }
      public static Guid PreviewSettings { get { return Guid.Parse("e6085e6b-ecaa-4540-80ef-d72ea6445ed9"); } }
      public static Guid ContentEditorSettings { get { return Guid.Parse("ffa65102-88c9-4e22-a54d-a5d297bf1864"); } }
      public static Guid ContentUpdatePreviewMarkersEventInfo { get { return Guid.Parse("2137b1e2-df2f-4650-90d9-fbbf175ed288"); } }
      public static Guid NewContentControlAssignBy { get { return Guid.Parse("a6a43ea0-28ed-400b-a8b2-7de171420404"); } }
      public static Guid ContentUIs { get { return Guid.Parse("13f716cd-2cae-4c1a-9279-c7e9210dc6ae"); } }
      public static Guid SelectionNavigator { get { return Guid.Parse("025c3c50-316e-454c-833c-a2b1c00dd239"); } }
      // returned object is RdkContentChildSlot
      public static Guid ContentChildSlot { get { return Guid.Parse("b5efce95-6fd1-4d2d-be6f-6b6da3a01036"); } }
      // return object is DecalDataSource
      public static Guid Decals { get { return Guid.Parse("de0eedbc-2878-46f8-a7d2-f9cee9d9e908"); } }
      // return object is RdkEdit
      public static Guid RdkEdit { get { return Guid.Parse("5ab6b74a-efe5-4e2b-aba4-3174452b30eb"); } }
      public static Guid NullGuid { get { return Guid.Parse("00000000-0000-0000-0000-000000000000"); } }

      // *** 
      // Internal DataSources start
      // ***
      internal static Guid ContentManipulator { get { return Guid.Parse("ded2b938-473f-4634-81d5-ff32b4c20f3f"); } }
      internal static Guid ContentUpdatePreview { get { return Guid.Parse("680b115a-e7af-4e63-a34f-22ce46d9d48a"); } }
      internal static Guid DragDropTopLevel { get { return Guid.Parse("87d7be21-07aa-413b-8819-23d9cae83645"); } }
      internal static Guid DragDropSubNode { get { return Guid.Parse("34f61182-7f05-45b0-9962-20830e183265"); } }
      internal static Guid DragDropColor { get { return Guid.Parse("23327b11-a6ff-4a7d-85ef-7e17413e4314"); } }
      internal static Guid DragDropLibraries { get { return Guid.Parse("ad7b70a3-ef07-4ec4-bf22-5cdc124bbb35"); } }
      // return object is TextureMapping
      internal static Guid TextureMapping { get { return Guid.Parse("ad2a2697-417b-44be-b127-0d46b5e98987 "); } }
      // return object is TextureMapping_Channels
      internal static Guid TextureMapping_Channels { get { return Guid.Parse("20b6b75a-9003-41d7-9409-5425b21a5670"); } }
      // return object is TextureMapping_Mappings
      internal static Guid TextureMapping_Mappings { get { return Guid.Parse("436be17a-f14b-4c7f-af6d-937e43beeb03"); } }
      // return object is PreviewProperties
      internal static Guid PreviewProperties { get { return Guid.Parse("e4204d28-4b55-4c16-8220-5fac80939514"); } }
      internal static Guid ContentCtrlInfo { get { return Guid.Parse("56b17d3e-9a5c-4556-b483-73bafe63b271"); } }
      internal static Guid Libraries { get { return Guid.Parse("59e961e5-b965-485f-af1d-0f9b6068b9a1"); } }
      internal static Guid FolderNavigator { get { return Guid.Parse("1e26e62d-73ee-490c-82b0-c24d06aa574c"); } }
      // return object is RenderContentCollection
      internal static Guid ContentSelectionForSetParams { get { return Guid.Parse("e360441e-a52c-4aea-bd17-40e7f59e278d"); } }
      // return object is RenderContentCollection
      internal static Guid ContentSelectionForChangeType { get { return Guid.Parse("7d8ff5df-36f2-4b70-bb39-b073793c5d4d"); } }
      internal static Guid CustomCurve { get { return Guid.Parse("4d982b00-d5af-4e17-93b3-5c766d11af72"); } }
      // return object is ColorData
      internal static Guid ColorData { get { return Guid.Parse("434302ec-a268-4579-a584-d11f8cb8c90c"); } }
      // return object is RdkIORMenuData
      internal static Guid IORMenuData { get { return Guid.Parse("70013684-12d2-4101-aaac-395c24562f86"); } }
      // return obhect is RenderContentCollection
      internal static Guid ContentSelectionOverride { get { return Guid.Parse("affe5c5c-51e1-4a79-b180-d12ee6a82964"); } }
      // *** 
      // Internal DataSources stop
      // ***
      public static Guid RdkRendering { get { return Guid.Parse("00feeac9-62b3-4eaf-b0b7-32c84186abf7"); } }
      public static Guid RdkRenderingProgress { get { return Guid.Parse("7a74dfd0-f699-4692-8320-e3441e413aee"); } }
      public static Guid RdkRenderingGamma { get { return Guid.Parse("e88f0f2d-4bec-4161-831d-5ebb1722e938"); } }
      public static Guid RdkRenderingToneMapping { get { return Guid.Parse("d83d10dc-55ea-4580-a721-d89c45117ae5"); } }
      public static Guid RdkRenderingPostEffects { get { return Guid.Parse("9ad06d76-6712-49c8-8364-1f534797c370"); } }
      public static Guid RdkRenderingPostEffectDOF { get { return Guid.Parse("48cf3ad5-2e39-4f4b-84ce-aa1f2fa6f60d"); } }
      public static Guid RdkRenderingPostEffectFog { get { return Guid.Parse("57daa49d-3856-499e-b480-d76bce053013"); } }
      public static Guid RdkRenderingPostEffectGlare { get { return Guid.Parse("902cea64-8bad-403f-932b-98b08fd50f61"); } }
      public static Guid RdkRenderingPostEffectGlow { get { return Guid.Parse("dd02e1c3-54b3-47a0-ad60-aa5d13794bc2"); } }
    }

    public class EventInfoArgs : System.EventArgs
    {
      internal EventInfoArgs(Guid uuidData, IntPtr pEventInfo)
      {
        DataType = uuidData;
        EventInfoPtr = pEventInfo;
      }
      public Guid DataType { get; private set; }
      public IntPtr EventInfoPtr { get; private set; }
    }

    public class EventArgs : System.EventArgs
    {
      internal EventArgs(Guid uuidData)
      {
        DataType = uuidData;
      }
      public Guid DataType { get; private set; }
    }
  }

  public interface IHasCppImplementation
  {
    IntPtr CppPointer { get; }
  }

  public interface IWindow
  {
    //System.Drawing.Rectangle Position { set; }
    bool Created { get; }
    bool Shown { get; set; }
    bool Enabled { get; set; }
    LocalizeStringPair Caption { get; }
    IntPtr Parent { get; set; }
    IntPtr Window { get; }
    void Move(System.Drawing.Rectangle pos, bool bRepaint, bool bRepaintBorder);
  }

  //ICollapsibleSectionHolder == IRhinoUiHolder
  public interface ICollapsibleSectionHolder : IWindow, IHasCppImplementation, IDisposable
  {
    void Add(ICollapsibleSection section);
    void Remove(ICollapsibleSection section);

    IEnumerable<ICollapsibleSection> Sections { get; }
    int SectionCount { get; }
    ICollapsibleSection SectionAt(int index);

    bool IsSectionExpanded(ICollapsibleSection section);
    void ExpandSection(ICollapsibleSection section, bool expand, bool ensureVisible);

    System.Drawing.Color BackgroundColor { set; }

    string EmptyText { set; }

    void UpdateAllViews(int flags);

    int TopMargin { get; set; }
    int BottomMargin { get; set; }
    int LeftMargin { get; set; }
    int RightMargin { get; set; }

    int ScrollPosition { get; set; }

    string SettingsPathSubKey { set; }
  }

  public class Delegates
  {
    internal delegate void MOVEPROC(int serial, int x, int y, int w, int h, int bRepaint, int bRepaintBorder);
  }

  //ICollapsibleSection == IRhinoUiSection
  public interface ICollapsibleSection : IWindow, IHasCppImplementation, IDisposable
  {
    int Height { get; }
    bool Hidden { get; }
    bool InitiallyExpanded { get; }
    Guid Id { get; }
    string SettingsTag { get; }
    bool Collapsible { get; }
    System.Drawing.Color BackgroundColor { get; set; }
    IRdkViewModel ViewModel { get; }
    Guid PlugInId { get; }
    LocalizeStringPair CommandOptionName { get; }
    int RunScript(IRdkViewModel vm);
    Guid ViewModelId { get; }
  }

  //IViewModel == IRhinoUiController
  public interface IRdkViewModel : IHasCppImplementation
  {
    //Pass one of the DataSourceProviderIds static Guids
    object GetData(Guid uuidDataType, bool bForWrite, bool bAutoChangeBracket);
    void Commit(Guid uuidDataType);
    void Discard(Guid uuidDataType);
  }
}