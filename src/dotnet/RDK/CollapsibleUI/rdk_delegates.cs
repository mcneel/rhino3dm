using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rhino.RDK
{
    class Delegates
    {
        public delegate void   VOIDPROC(int sn);

        public delegate void   SETBOOLPROC(int serial, int b);
        public delegate int    GETBOOLPROC(int serial);

        public delegate int    GETINTPROC(int serial);
        public delegate void   SETINTPROC(int serial, int i);

        public delegate void   SETINTPTRPROC(int serial, IntPtr intptr);
        public delegate bool   SETINTPTRGETBOOLPROC(int serial, IntPtr intptr);
        public delegate IntPtr GETINTPTRPROC(int serial);

        public delegate IntPtr FACTORYPROC(int serial, Guid typeId);
        public delegate IntPtr ATINDEXPROC(int serial, int index);

        public delegate Guid   GETGUIDPROC(int serial);
        public delegate Guid   GETGUIDINTPROC(int serial, int i);
        public delegate Guid   GETGUIDINTPTRINTINTPROC(int serial, IntPtr intptr, int i, int j);
        public delegate void   SETGUIDPROC(int serial, Guid uuid);
        public delegate bool   SETGUIDBOOLPROC(int serial, Guid uuid);
        public delegate void   SETGUIDEVENTINFOPROC(int serial, Guid uuid, IntPtr pEventInfo);

        public delegate void   SETBOOLUINT(int serial, bool b, uint ui);
        public delegate void   GETSTRINGPROC(int serial, IntPtr pON_wString);
        public delegate bool   SETSELECTIONPROC(int sn, IntPtr pContentArray);
        public delegate void   VOIDSETSELECTIONPROC(int sn, IntPtr pContentArray);
        public delegate void   SETMODEPROC(int sn, int modevalue, bool notify);
        public delegate bool   SETINTPTRREFINTPTRPROC(int serial, IntPtr intptr, ref IntPtr ref_intptr);
        public delegate bool   SETINTPTRINTPTRBOOLPROC(int serial, IntPtr intptr1, IntPtr intptr2);
        public delegate bool   SETREFINTREFINTREFBOOLBOOLPROC(int serial, ref int value1, ref int value2, ref bool bValue);
        public delegate bool   SETINTPTRINTPTRUINTUINTBOOLBOOLPROC(int serial, IntPtr intptr1, IntPtr intptr2, uint uValue1, uint uValue2, bool bValue);
        public delegate bool   SETUINTGUIDBOOLPROC(int serial, uint uValue, Guid uuid);
        public delegate bool   SETGUIDINTINTBOOLPROC(int serial, Guid uuidPreviewWindow, int width, int height);

        public delegate bool BOOL_INTINPTRINTINT_PROC(int serial, IntPtr ptr, int i, int j);
        public delegate bool BOOL_INT_PROC(int serial);
        public delegate void VOID_INTBOOL_PROC(int serial, bool b);
        public delegate void VOID_GETINTINTINT_PROC(int serial, ref int i, ref int j);
        public delegate void VOID_SETINTINTINT_PROC(int serial, int i, int j);

        public delegate IntPtr NEWRENDERFRAMEPROC(int serial, IntPtr pSdkRender, IntPtr pBackEnd, IntPtr wszRenderer, bool bCreateWindow, bool bIsClone);

        public delegate bool SHOWPEPOPTIONSPROC(int serial, IntPtr pController, IntPtr pParentWnd);

        public delegate bool SHOWRENDERINGOPENFILEDLGPROC(int serial, IntPtr sFilenameInOut);
        public delegate bool SHOWRENDERINGSAVEFILEDLGPROC(int serial, IntPtr sFilenameInOut, ref int iAlphaOut, Guid uuidRenderEngine);
        public delegate bool SHOWCONTENTTYPEBROWSERPROC(int serial, uint doc_serial, Guid uuidDefaultType, Guid uuidDefaultInstance, IntPtr aKinds, uint ccbu_flags, IntPtr aInstanceOut, ref int resOut);
        public delegate bool PROMPTFORIMAGEFILEPARAMSPROC(int serial, ref int depthOut, ref int widthOut, ref int heightOut);

        public delegate bool SHOWNAMEDITEMEDITDLGPROC(int serial, IntPtr wszCaption, IntPtr wszPrompt, IntPtr sFilenameInOut);
        public delegate bool SAVERENDERIMAGEASPROC(int serial, IntPtr pON_wString, bool bUseAlpha);
        public delegate void SHOWSMARTMERGENAMECOLLISIONDLGPROC(int serial, ref int choice, ref int option, IntPtr pName);
        public delegate bool SHOWPREVIEWPROPERTIESDLGPROC(int serial, IntPtr intptr1);
        public delegate bool CHOOSECONTENTPROC(int serial, uint doc_serial, ref Guid instanceId, IntPtr kinds, uint cceFlags);
        public delegate bool PEPPICKPOINTONIMAGEPROC(int serial, Guid session, ref int x, ref int y);
        public delegate bool SHOWLAYERMATERIALDIALOGPROC(int serial, IntPtr l);
        public delegate bool PROMPTFORIMAGEDRAGOPTIONSDLGPROC(int serial, ref int choice, bool bMultipleFiles, bool bbIsLDRBitmap);
				public delegate bool PROMPTFOROPENACTIONSDLGPROC(int serial, ref int choice);
        public delegate int DISPLAYMISSINGTEXTURESDIALOG(int serial, uint doc_serial, int initialFilter, bool bAllowAbort, bool bForceDisplayIfEmpty, int goodReturn);

        public delegate IntPtr ADDSUBMENUPROC(int serial, IntPtr wszCaption);
        public delegate bool ADDITEMPROC(int serial, IntPtr wszCaption, ushort cmd, bool bEnabled);
        public delegate void ADDSEPARATORPROC(int serial);
        public delegate void SUPPORTEDUUIDDATAPROC(int serial, IntPtr aSimpleUuidOut);
        public delegate Rhino.Display.Color4f GETCOLORPROC(int serial);
        public delegate bool SETCOLORPROC(int serial, Rhino.Display.Color4f col);
        public delegate bool USESALPHAPROC(int serial);
        public delegate bool SHOWCONTENTCTRLPROPDLGPROC(int serial, bool bShowOn, bool bShowAmount, bool bShowChannel, IntPtr vOn, IntPtr vAmount, IntPtr vMode, IntPtr vCustom, IntPtr vStrings);

        public delegate bool OPENNAMEDVIEWANIMATIONSETTINGSDLG(int serial, [MarshalAs(UnmanagedType.U1)]ref bool bAnimate, [MarshalAs(UnmanagedType.U1)]ref bool bConstantSpeed, ref int frames, ref double unitsPerFrame, ref int delay);

        public delegate bool PEPPICKRECTANGLEONIMAGEPROC(int serial, Guid session, ref int left, ref int top, ref int right, ref int bottom);

        public delegate void CREATEINPLACERENDERVIEWPROC(int serial, IntPtr pBackEnd, Guid session_id, uint doc_serial, int x, int y, int w, int h, IntPtr parent_wnd);

        public static bool ToBool(int iBool)
        {
            return 0 != iBool;
        }

        public static int ToInt(bool bBool)
        {
            return bBool ? 1 : 0;
        }
    }
}
