using System;
using System.Diagnostics;
using Eto.Forms;
using Rhino.Render;


namespace Rhino.UI.Controls.Thumbnaillist
{
    //This is a native Eto holder without any support for native hosting.
    public abstract class ThumbnailList : Eto.Forms.Panel, IThumbnailList
    {
        public Eto.Forms.StackLayout m_stack;

        private ContentEditingContext m_cec;
        private Eto.Forms.Scrollable m_scrollable;
        private ThumbnailListImpl m_impl;
        private ThumbnailView m_thumbview;
        private RdkThumbnaillistViewModel m_viewmodel;

        public ContentEditingContext CEC
        {
            get
            {
                return m_cec;
            }
            set
            {
                m_cec = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
        
            if (m_impl != null)
            {
                var impl = m_impl;
                m_impl = null;
                impl.Dispose();
            }

            base.Dispose(disposing);
        }

        public virtual void Move(System.Drawing.Rectangle rect, bool bRepaint, bool bRepaintNC)
        {
            System.Diagnostics.Debug.Assert(false);
            //Do nothing when everything is native Eto.
        }

        public virtual IntPtr HolderParent
        {
            set
            {
                Debug.Assert(false);
                //Do nothing when everything is native Eto.
            }
        }
        
        public new virtual bool Shown { get { return base.Visible; } set { base.Visible = value; } }
        public new virtual bool Enabled { get { return base.Enabled; } set { base.Enabled = value; } }
        public virtual bool Created { get { return true; } }
        public virtual bool Hidden { get { return base.Visible; } set { base.Visible = value; } }
        public RdkThumbnaillistViewModel ViewModel { get { return m_impl.ViewModel; } }
        public virtual void SetSettingsPath(string w) { }
        public virtual void SetSearchPattern(string w) { }
        public virtual void SaveMetaDataToDocument() { }
        // No previewmetadata at this stage (TODO)
        // CRhRdkPreviewMetaData* PreviewMetaData(void) const override;
        public virtual Rhino.Render.PreviewAppearance SelectedAppearance() { return null; }
        public virtual bool PropagateSelectedAppearance() { return true; }
        public virtual Rhino.Render.RenderContent ContentFromThumbId(ref Guid uuidThumb) { return null; }
        public virtual void GetGridMetrics(ref int w, ref int h, ref int ox, ref int oy) { }
        public virtual void GetStatisticsHeaderHeight() { }
        public virtual Guid UUID() { Guid g = new Guid(); return g; }
        public virtual void Clear() { }
        public virtual void Add(ref IRhRdkThumbnail t) { }
        public virtual IRhRdkThumbnail Get(ref Guid u) { return null; }
        public virtual IRhRdkThumbnailList_Modes Mode() { return IRhRdkThumbnailList_Modes.List; }
        public virtual void SetMode(IRhRdkThumbnailList_Modes m, bool b) 
        { 
            
        }
        public virtual IRhRdkThumbnailList_Shapes Shape() { return IRhRdkThumbnailList_Shapes.Square; }

        // [Giulio] Max, sorry, I just renamed this GetSize so that the warning would go away. You can also add the keyword new if
        // you need to hide the underlying property (is that a better idea?).
        public virtual IRhRdkContentThumbnailList_Sizes GetSize() { return IRhRdkContentThumbnailList_Sizes.Medium; }
        public virtual void SetClientText(string w) { }
        public virtual void SetCustomBitmapSize(int w, int h) { }
        public virtual bool ShowLabels() { return true; }
        public virtual void SetShowLabels(bool b) { }
        public virtual string EnglishCaption { get; }
        public virtual string LocalCaption { get; }

        System.Drawing.Color IThumbnailList.BackgroundColor
        {
            set 
            {
                base.BackgroundColor = ConvertToEtoColor(value);
            }

            get
            {
                return ConvertToSystemColor(base.BackgroundColor);
            }
        }

        // IWindow
        void IWindow.Move(System.Drawing.Rectangle rect, bool bRepaint, bool bRepaintNC)
        {
            Move(rect, bRepaint, bRepaintNC);
        }

        string IWindow.LocalCaption
        {
            get
            {
                return "Thumbnail List";
            }
        }
        string IWindow.EnglishCaption
        {
            get
            {
                return "Thumbnail List";
            }
        }

        IntPtr IWindow.Parent
        {
            set
            {
                HolderParent = value;
            }
        }

        IntPtr IWindow.WindowPtr
        {
            get
            {
                Debug.Assert(false);
                return IntPtr.Zero;
            }
        }

       /* public event EventHandler ViewModelActivated
        {
            add
            {
                m_impl.ViewModelActivated += value;
            }
            remove
            {
                m_impl.ViewModelActivated -= value;
            }
        }*/
        bool IWindow.Shown { get { return Shown; } set { Shown = value; } }
        bool IWindow.Enabled { get { return Enabled; } set { Enabled = value; } }
        bool IWindow.Created { get { return Created; } }

        IntPtr IHasCppImplementation.CppPointer { get { return m_impl.CppPointer; } }

        public ThumbnailList()
        {
            m_impl = new ThumbnailListImpl(this);

            // Thumbnaillist UI
            m_thumbview = new ThumbnailView();
            m_thumbview.SetContextMenu();

            m_scrollable = new Scrollable();
            m_scrollable.Border = Eto.Forms.BorderType.None;
            Panel t = new Panel() { Width = 1, Content = m_thumbview };
            m_scrollable.Content = t;
            m_scrollable.Padding = 2;
            m_scrollable.ExpandContentHeight = true;
            Content = m_scrollable;

            //This code is here to match the code in the CTOR of CRhRdKHolder_MFC.
            //It shouldn't be here - it should be in the client code.  But this will 
            //make the holder fit more nicely into the existing UI for now.
            m_scrollable.BackgroundColor = Eto.Drawing.Color.FromArgb(0x00F9F7F7);
        }

        public void ViewModelActivated()
        {
            string text = ViewModel.ToString();

            // make databinding to viewmodel

            // Update ui from viewmodel (initial update)
            ViewModel.DisplayData();
        }

        private System.Drawing.Color ConvertToSystemColor(Eto.Drawing.Color c)
        {
            System.Drawing.Color color = System.Drawing.Color.FromArgb(c.Ab, c.Rb, c.Gb, c.Bb);
            return color;
        }

        private Eto.Drawing.Color ConvertToEtoColor(System.Drawing.Color c)
        {
            Eto.Drawing.Color color = new Eto.Drawing.Color();
            color.Rb = c.R;
            color.Gb = c.G;
            color.Bb = c.B;
            color.Ab = c.A;
            return color;
        }

        public new interface IHandler : Panel.IHandler
        {

        }
    }
}
