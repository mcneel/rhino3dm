using System;
using Rhino.Runtime.InteropWrappers;

#pragma warning disable 1591
#if RHINO_SDK

namespace Rhino.Render
{
    public static class SupportOptions
    {
        private static string _last_navigated_folder = "";

        public static bool AlwaysShowSunPreview()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_AlwaysShowSunPreview();
        }

        public static void SetAlwaysShowSunPreview(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetAlwaysShowSunPreview(b);
        }

        public static bool PreviewCustomRenderMeshes()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_PreviewCustomRenderMeshes();
        }

        public static void SetPreviewCustomRenderMeshes(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetPreviewCustomRenderMeshes(b);
        }

        public static bool MultithreadedTextureEvaluation()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_MultithreadedTextureEvaluation();
        }

        public static void SetMultithreadedTextureEvaluation(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetMultithreadedTextureEvaluation(b);
        }

        public static bool HarvestContentParameters()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_HarvestContentParameters();
        }

        public static void SetHarvestContentParameters(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetHarvestContentParameters(b);
        }

        public static int TextureSize()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_TextureSize();
        }

        public enum RdkTextureSize
        {
            Size1 = UnsafeNativeMethods.RdkTextureSize.Size1,
            Size2 = UnsafeNativeMethods.RdkTextureSize.Size2,
            Size3 = UnsafeNativeMethods.RdkTextureSize.Size3,
            Size4 = UnsafeNativeMethods.RdkTextureSize.Size4,
            Size5 = UnsafeNativeMethods.RdkTextureSize.Size5
        }

        [Obsolete("Support for changing the texture size programatically will disappear in a future version of Rhino")]
        public static void SetTextureSize(RdkTextureSize size, bool bSendEvent)
        {
            if (RdkTextureSize.Size1 == size)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetTextureSize(UnsafeNativeMethods.RdkTextureSize.Size1, bSendEvent);
            }
            else
            if (RdkTextureSize.Size2 == size)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetTextureSize(UnsafeNativeMethods.RdkTextureSize.Size2, bSendEvent);
            }
            else
            if (RdkTextureSize.Size3 == size)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetTextureSize(UnsafeNativeMethods.RdkTextureSize.Size3, bSendEvent);
            }
            else
            if (RdkTextureSize.Size4 == size)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetTextureSize(UnsafeNativeMethods.RdkTextureSize.Size4, bSendEvent);
            }
            else
            if (RdkTextureSize.Size5 == size)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetTextureSize(UnsafeNativeMethods.RdkTextureSize.Size5, bSendEvent);
            }
            else
            {
                throw new Exception("Unknown RDK texture size");
            }
        }

        public static bool SupportSharedUIsNoCache()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_SupportSharedUIsNoCache();
        }

        public static bool SupportSharedUIs()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_SupportSharedUIs();
        }

        public static void SetSupportSharedUIs(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetSupportSharedUIs(b);
        }

        public static bool CombineEditors()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_CombineEditors();
        }

        public static void SetCombineEditors(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetCombineEditors(b);
        }

        public static bool UseDefaultLibraryPath()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_UseDefaultLibraryPath();
        }

        public static void SetUseDefaultLibraryPath(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetUseDefaultLibraryPath(b);
        }

        public static string CustomLibraryPath()
        {
            using (var sh = new StringHolder())
            {
                var ptr_string = sh.NonConstPointer();
                UnsafeNativeMethods.Rdk_SupportOption_CustomLibraryPath(ptr_string);
                return sh.ToString();
            }
        }

        public static void SetCustomLibraryPath(string path)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetCustomLibraryPath(path);
        }

        public static bool ShowRenderContent()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_ShowRenderContent();
        }

        public static bool ShowDocuments()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_ShowDocuments();
        }

        public static bool ShowCustom()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_ShowCustom();
        }

        public static void SetShowRenderContent(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetShowRenderContent(b);
        }

        public static void SetShowDocuments(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetShowDocuments(b);
        }

        public static void SetShowCustom(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetShowCustom(b);
        }

        public static string CustomPaths()
        {
            using (var sh = new StringHolder())
            {
                var ptr_string = sh.NonConstPointer();
                UnsafeNativeMethods.Rdk_SupportOption_CustomPaths(ptr_string);
                return sh.ToString();
            }
        }

        public static void SetCustomPaths(string path)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetCustomPaths(path);
        }

        public enum RdkInitialLocation : int
        {
            RenderContent = UnsafeNativeMethods.RdkInitialLocation.RenderContent,
            LastOpenedFolder = UnsafeNativeMethods.RdkInitialLocation.LastOpenedFolder,
            CustomFolder = UnsafeNativeMethods.RdkInitialLocation.CustomFolder
        };

        public static RdkInitialLocation LibrariesInitialLocation()
        {
            var l = UnsafeNativeMethods.Rdk_SupportOption_LibrariesInitialLocation();
            if (UnsafeNativeMethods.RdkInitialLocation.RenderContent == l)
            {
               return RdkInitialLocation.RenderContent;
            }
            else
            if (UnsafeNativeMethods.RdkInitialLocation.LastOpenedFolder == l)
            {
                return RdkInitialLocation.LastOpenedFolder;
            }
            else
            if (UnsafeNativeMethods.RdkInitialLocation.CustomFolder == l)
            {
                return RdkInitialLocation.CustomFolder;
            }
            else
            {
                throw new Exception("Unknown RDK initial location");
            }
        }

        public static void SetLibrariesInitialLocation(RdkInitialLocation l)
        {
            if (RdkInitialLocation.RenderContent == l)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetLibrariesInitialLocation(UnsafeNativeMethods.RdkInitialLocation.RenderContent);
            }
            else
            if (RdkInitialLocation.LastOpenedFolder == l)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetLibrariesInitialLocation(UnsafeNativeMethods.RdkInitialLocation.LastOpenedFolder);
            }
            else
            if (RdkInitialLocation.CustomFolder == l)
            {
                UnsafeNativeMethods.Rdk_SupportOption_SetLibrariesInitialLocation(UnsafeNativeMethods.RdkInitialLocation.CustomFolder);
            }
            else
            {
                throw new Exception("Unknown RDK initial location");
            }
        }

        public static string LibrariesInitialLocationCustomFolder()
        {
            using (var sh = new StringHolder())
            {
                var ptr_string = sh.NonConstPointer();
                UnsafeNativeMethods.Rdk_SupportOption_LibrariesInitialLocationCustomFolder(ptr_string);
                return sh.ToString();
            }
        }

        public static void SetLibrariesInitialLocationCustomFolder(string path)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetLibrariesInitialLocationCustomFolder(path);
        }

        public static void SetLastNavigatedLocation(string folder)
        {
            _last_navigated_folder = folder;
        }

        public static string LastNavigatedLocation()
        {
            return _last_navigated_folder;
        }

        public static bool PreferNativeRenderer()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_PreferNativeRenderer();
        }

        public static void SetPreferNativeRenderer(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetPreferNativeRenderer(b);
        }

        public static bool UsePreviewCache()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_UsePreviewCache();
        }

        public static void SetUsePreviewCache(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetUsePreviewCache(b);
        }

        public static bool UseQuickInitialPreview()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_UseQuickInitialPreview();
        }

        public static void SetUseQuickInitialPreview(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetUseQuickInitialPreview(b);
        }

        public static bool UsePreview()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_UsePreview();
        }

        public static bool UseRenderedPreview()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_UseRenderedPreview();
        }

        public static bool ShowDetailsPanel()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_ShowDetailsPanel();
        }

        public static void SetShowDetailsPanel(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetShowDetailsPanel(b);
        }

        public static string PreferredUnpackFolder()
        {
            using (var sh = new StringHolder())
            {
                var ptr_string = sh.NonConstPointer();
                UnsafeNativeMethods.Rdk_SupportOption_PreferredUnpackFolder(ptr_string);
                return sh.ToString();
            }
        }

        public static void SetPreferredUnpackFolder(string path)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetPreferredUnpackFolder(path);
        }

        public static bool CheckSupportFilesBeforeRendering()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_CheckSupportFilesBeforeRendering();
        }

        public static void SetCheckSupportFilesBeforeRendering(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetCheckSupportFilesBeforeRendering(b);
        }

        public static bool AutoSaveRenderings()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_AutoSaveRenderings();
        }

        public static void SetAutoSaveRenderings(bool b)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetAutoSaveRenderings(b);
        }

        public static int AutoSaveKeepAmount()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_AutoSaveKeepAmount();
        }

        public static void SetAutoSaveKeepAmount(int value)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetAutoSaveKeepAmount(value);
        }

        public static int MaxPreviewCacheMB()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_MaxPreviewCacheMB();
        }

        public static int MaxPreviewSeconds()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_MaxPreviewSeconds();
        }

        public static bool EnablePreviewJobLog()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_EnablePreviewJobLog();
        }

        public static int DarkPreviewCheckerColor()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_DarkPreviewCheckerColor();
        }

        public static int LightPreviewCheckerColor()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_LightPreviewCheckerColor();
        }

        public static int LabelFormatLoc()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_LabelFormatLoc();
        }

        public static void SetLabelFormatLoc(int value)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetLabelFormatLoc(value);
        }

        public static int LabelFormatUtc()
        {
            return UnsafeNativeMethods.Rdk_SupportOption_LabelFormatUTC();
        }

        public static void SetLabelFormatUtc(int value)
        {
            UnsafeNativeMethods.Rdk_SupportOption_SetLabelFormatUTC(value);
        }
    }
}

#endif
