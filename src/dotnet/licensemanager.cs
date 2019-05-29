using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Rhino.UI;
#if RHINO_SDK
using Rhino.PlugIns;
using Rhino.Runtime.InteropWrappers;

namespace Rhino.Runtime
{
  static class LicenseManager
  {

    /// <summary>
    /// Delegate passed to rhcommon_c as a call back function pointer
    /// </summary>
    /// <returns>
    /// Returns LicenseUtils.Initialize()
    /// </returns>
    internal delegate int InitializeCallback();
    private static readonly InitializeCallback InitializeProc = InitializeHelper;
    /// <summary>
    /// Delegate passed to rhcommon_c as a call back function pointer
    /// </summary>
    /// <param name="message"></param>
    /// <param name="resultString"></param>
    internal delegate void EchoCallback([MarshalAs(UnmanagedType.LPWStr)]string message, IntPtr resultString);
    private static readonly EchoCallback EchoProc = EchoHelper;
    /// <summary>
    /// Delegate passed to rhcommon_c as a call back function pointer
    /// </summary>
    /// <param name="cdKey"></param>
    /// <returns></returns>
    internal delegate bool ShowValidationUiCallback([MarshalAs(UnmanagedType.LPWStr)]string cdKey);
    private static readonly ShowValidationUiCallback ShowValidationUiProc = ShowValidationUiHelper;
    /// <summary>
    /// Delegate passed to rhcommon_c as a call back function pointer
    /// </summary>
    /// <param name="mode">
    /// Constants defined in rh_license.cpp which define which LicenseUtils
    /// method to call.
    /// </param>
    /// <param name="id">
    /// License type id
    /// </param>
    /// <returns></returns>
    internal delegate int UuidCallback(int mode, Guid id);
    static readonly UuidCallback UuidProc = UuidHelper;
    /// <summary>
    /// Delegate passed to rhcommon_c as a call back function pointer
    /// </summary>
    /// <param name="title">
    /// License description string
    /// </param>
    /// <param name="pluginId">
    /// Plugin ID requesting a license
    /// </param>
    /// <param name="licenseId">
    /// ID of license for this version of the plug-in. May be different from pluginId
    /// </param>
    /// <param name="productBuildType"></param>
    /// <param name="path">
    /// Full path to module which we are getting a license for, will check
    /// module for the required digital signatures.
    /// </param>
    /// <param name="validator">
    /// A pointer to the CRhinoLicenseValidator associated with this call, this
    /// object contains the ValidateProductKey method used by the Zoo Client to
    /// validate key values.
    /// </param>
    /// <returns>
    /// Returns a value of 1 on success or 0 on failure.
    /// </returns>
    internal delegate int GetLicenseCallback([MarshalAs(UnmanagedType.LPWStr)]string title, Guid pluginId, Guid licenseId, int productBuildType, [MarshalAs(UnmanagedType.LPWStr)]string path, IntPtr validator);
    private static readonly GetLicenseCallback GetLicenseProc = GetLicenseHelper;
    /// <summary>
    /// Delegate passed to rhcommon_c as a call back function pointer
    /// </summary>
    /// <param name="pluginId">
    /// ID of plug-in requesting a license
    /// </param>
    /// <param name="licenseId">
    /// ID of license for this version of the plug-in. May be different from pluginId.
    /// </param>
    /// <param name="title">
    /// License description string
    /// </param>
    /// <param name="capabilities">
    /// Bitwise flag containing a list of buttons to be displayed on the custom
    /// get license dialog.
    /// </param>
    /// <param name="textMask">
    /// Optional text mask to be applied to the license key input control
    /// </param>
    /// <param name="path">
    /// Full path to module which we are getting a license for, will check
    /// module for the required digital signatures.
    /// </param>
    /// <param name="validator">
    /// A pointer to the CRhinoLicenseValidator associated with this call, this
    /// object contains the ValidateProductKey method used by the Zoo Client to
    /// validate key values.
    /// </param>
    /// <returns>
    /// Returns a value of 1 on success or 0 on failure.
    /// </returns>
    internal delegate int GetCustomLicenseCallback(Guid pluginId, Guid licenseId, [MarshalAs(UnmanagedType.LPWStr)]string title, uint capabilities, [MarshalAs(UnmanagedType.LPWStr)]string textMask, [MarshalAs(UnmanagedType.LPWStr)]string path, IntPtr validator);
    private static readonly GetCustomLicenseCallback GetCustomLicenseProc = CustomGetLicenseHelper;

    internal delegate bool AskUserForLicenseCallback([MarshalAs(UnmanagedType.LPWStr)]string productTitle, bool standAlone, IntPtr parent, Guid pluginId, Guid licenseId, int productBuildType, [MarshalAs(UnmanagedType.LPWStr)]string texMask, [MarshalAs(UnmanagedType.LPWStr)]string path, IntPtr validator, uint capabilities);
    private static readonly AskUserForLicenseCallback AskUserForLicenseProc = AskUserForLicenseHelper;

    internal delegate bool GetRegisteredOwnerInfoCallback(Guid productId, IntPtr ownerWStringPointer, IntPtr companyWStringPointer);
    private static readonly GetRegisteredOwnerInfoCallback g_get_registered_owner_info_proc = GetRegisteredOwnerInfoHelper;

    internal delegate bool ShowExpiredMessageCallback(Rhino.Runtime.Mode mode, ref int result);
    private static readonly ShowExpiredMessageCallback g_show_expired_message_proc = ShowExpiredMessageHelper;

    /// <summary>
    /// Gets set to true after initial call to
    /// UnsafeNativeMethods.RHC_SetLicenseManagerCallbacks and is checked to
    /// make sure it only gets called one time.
    /// </summary>
    private static bool _setCallbacksWasRun;
    /// <summary>
    /// Only needs to be called once, will call into rhcommon_c and set call
    /// back function pointers
    /// </summary>
    static public void SetCallbacks()
    {
      if (_setCallbacksWasRun) return;
      _setCallbacksWasRun = true;
      UnsafeNativeMethods.RHC_SetLicenseManagerCallbacks(
        InitializeProc,
        EchoProc,
        ShowValidationUiProc,
        UuidProc,
        GetLicenseProc,
        GetCustomLicenseProc,
        AskUserForLicenseProc,
        g_get_registered_owner_info_proc,
        g_show_expired_message_proc,
        GetInternetTimeProc
      );
    }

    #region rhcommon_c call back methods
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    static int InitializeHelper()
    {
      var success = LicenseUtils.Initialize();
      return success ? 1 : 0;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="resultString"></param>
    static void EchoHelper([MarshalAs(UnmanagedType.LPWStr)]string message, IntPtr resultString)
    {
      var echoResult = LicenseUtils.Echo(message);
      UnsafeNativeMethods.ON_wString_Set(resultString, echoResult);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cdKey"></param>
    /// <returns></returns>
    static bool ShowValidationUiHelper([MarshalAs(UnmanagedType.LPWStr)]string cdKey)
    {
      return LicenseUtils.ShowLicenseValidationUi(cdKey);
    }
    /// <summary>
    /// Helper class used by GetLicenseHelper and GetCustomLicenseHelper
    /// methods when calling back into UnsafeNativeMethods.RHC_GetLicense
    /// </summary>
    internal class ValidatorHelper
    {
      /// <summary>
      /// Constructor
      /// </summary>
      /// <param name="validator">CRhinoLicenseValidator unmanaged pointer</param>
      public ValidatorHelper(IntPtr validator)
      {
        _validator = validator;
      }


      private LicenseData LicenseDataFromCppValidator(IntPtr cppValidator)
      {
        var licenseData = new LicenseData();

        double licenseExpirationDate = 0.0;
        bool requiresOnlineValidation = false;
        bool licenseIsUpgrade = false;
        var licenseCount = 0;
        var buildType = 0;
        IntPtr hIcon;
        // String placeholders
        using (var shSerailNumber = new StringHolder())
        using (var shLicenseTitle = new StringHolder())
        using (var shProductLicense = new StringHolder())
        using (var shErrorMessage = new StringHolder())
        {
          // Get ON_wString pointers
          var pSerialNumber = shSerailNumber.NonConstPointer();
          var pLicenseTitle = shLicenseTitle.NonConstPointer();
          var pProductLicense = shProductLicense.NonConstPointer();
          var pErrorMessage = shErrorMessage.NonConstPointer();
          hIcon = UnsafeNativeMethods.RHC_ExtractLicenseData(cppValidator,
                                                             ref licenseExpirationDate,
                                                             pSerialNumber,
                                                             ref licenseCount,
                                                             pLicenseTitle,
                                                             pProductLicense,
                                                             ref buildType,
                                                             ref requiresOnlineValidation,
                                                             ref licenseIsUpgrade,
                                                             pErrorMessage);
          // Copy ON_wString values to C# strings
          licenseData.SerialNumber = shSerailNumber.ToString();
          licenseData.LicenseTitle = shLicenseTitle.ToString();
          licenseData.ProductLicense = shProductLicense.ToString();
          licenseData.RequiresOnlineValidation = requiresOnlineValidation;
          licenseData.IsUpgradeFromPreviousVersion = licenseIsUpgrade;
          licenseData.ErrorMessage = shErrorMessage.ToString();
        }
        // Set the expiration date using the C++ date data
        if (licenseExpirationDate == 0.0)
          licenseData.DateToExpire = null;
        else
          licenseData.DateToExpire = DateTime.FromOADate(licenseExpirationDate);
        licenseData.LicenseCount = licenseCount;
        licenseData.BuildType = (LicenseBuildType)buildType;
        // Icon associated with the specified license type
        if (hIcon != IntPtr.Zero)
        {
          // 19 May 214 - John Morse
          // Added try catch to the icon copy code, an exception here should not cause
          // license validation to fail.
          try
          {
            // Create a new icon from the handle.
            var newIcon = System.Drawing.Icon.FromHandle(hIcon);

            // Set the LicenseData icon. Note, LicenseData creates it's own copy of the icon.
            licenseData.ProductIcon = newIcon;

            //AJ: The code below causes Rhino to crash because hIcon is always the same.
            // When using Icon::FromHandle, you must dispose of the original icon by using the
            // DestroyIcon method in the Win32 API to ensure that the resources are released.
            //if (newIcon != null)
            //DestroyIcon(newIcon.Handle);
          }
          catch { }
        }

        return licenseData;
      }

      /// <summary>
      /// Passed as a delegate to LicenseUtils.GetLicense(), will call the C++
      /// ValidateProductKey() method then and copy CRhinoLicenseValidator C++
      /// data to the LicenseData output object.
      /// </summary>
      /// <param name="productKey"></param>
      /// <param name="licenseData">
      /// Key information is coppied to this object
      /// </param>
      /// <returns></returns>
      public ValidateResult ValidateProductKey(string productKey, out LicenseData licenseData)
      {
        // Create an empty LicenseData
        licenseData = new LicenseData();
        var rc = 0;
        // Call back into the C++ unmanaged function pointer
        if (IntPtr.Zero != _validator)
          rc = UnsafeNativeMethods.RHC_ValidateProductKey(productKey, _validator);
        if (rc != 1)
          return (-1 == rc ? ValidateResult.ErrorHideMessage : ValidateResult.ErrorShowMessage);
        // Copy unmanaged C++ validate data to the managed LicenseData

        licenseData = LicenseDataFromCppValidator(_validator);

        return ValidateResult.Success;
      }

      public ValidateResult VerifyLicenseKey(string licenseKey, string validationCode,
        DateTime validationCodeInstallDate, bool gracePeriodExpired, out LicenseData licenseData)
      {
        licenseData = new LicenseData();
        var rc = 0;
        // Call back into the C++ unmanaged function pointer
        if (IntPtr.Zero != _validator)
          rc = UnsafeNativeMethods.RHC_VerifyLicenseKey(licenseKey, validationCode, validationCodeInstallDate.ToOADate(), gracePeriodExpired, _validator);
        if (rc != 1)
          return (-1 == rc ? ValidateResult.ErrorHideMessage : ValidateResult.ErrorShowMessage);

        licenseData = LicenseDataFromCppValidator(_validator);

        return ValidateResult.Success;
      }

      public bool VerifyPreviousVersionLicenseKey(string licenseKey, string previousVersionLicenseKey, out string errorMessage)
      {
        if (IntPtr.Zero != _validator)
        {
          bool rc = UnsafeNativeMethods.RHC_VerifyPreviousVersionLicenseKey(licenseKey, previousVersionLicenseKey, _validator);
          var licenseData = LicenseDataFromCppValidator(_validator);
          errorMessage = licenseData.ErrorMessage;
          return rc;
        }
        errorMessage = "VerifyPreviousVersionLicenseKey called with null validator";
        return false;
      }

      public void OnLeaseChanged(LicenseLeaseChangedEventArgs args, out System.Drawing.Icon icon)
      {
        icon = null;

        if (IntPtr.Zero != _validator)
        {
          IntPtr hIcon = UnsafeNativeMethods.RHC_ExtractProductIcon(_validator);
          if (hIcon != IntPtr.Zero)
          {
            // 19 May 214 - John Morse
            // Added try catch to the icon copy code, an exception here should not cause
            // license validation to fail.
            try
            {
              // Create a new icon from the handle.
              var newIcon = System.Drawing.Icon.FromHandle(hIcon);
              // Set the icon. Notice we clone it.
              icon = (System.Drawing.Icon)newIcon.Clone();

              //AJ: The code below causes Rhino to crash because hIcon is always the same.
              // When using Icon::FromHandle, you must dispose of the original icon by using the
              // DestroyIcon method in the Win32 API to ensure that the resources are released.
              //if (newIcon != null)
              //DestroyIcon(newIcon.Handle);
            }
            catch { }
          }

          UnsafeNativeMethods.RHC_RhinoLicenseValidator_OnLeaseChanged(_validator, args.m_ptr);
        }
      }

      #region Interop Windows imports
      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      extern static bool DestroyIcon(IntPtr handle);
      #endregion Interop Windows imports

      #region Members
      /// <summary>
      /// CRhinoLicenseValidator pointer passed to the constructor
      /// </summary>
      readonly IntPtr _validator;
      #endregion Members
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="pluginId"></param>
    /// <param name="licenseId"></param>
    /// <param name="productBuildType"></param>
    /// <param name="path"></param>
    /// <param name="validator"></param>
    /// <returns></returns>
    private static int GetLicenseHelper([MarshalAs(UnmanagedType.LPWStr)]string title, Guid pluginId, Guid licenseId, int productBuildType, [MarshalAs(UnmanagedType.LPWStr)]string path, IntPtr validator)
    {
      var helper = new ValidatorHelper(validator);
      int capabilities = 0; // no capabilities
      string text_mask = null; // no mask
      var result = LicenseUtils.GetLicense(helper.ValidateProductKey, null, productBuildType, capabilities, text_mask, path, title, pluginId, licenseId);
      return (result ? 1 : 0);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pluginId"></param>
    /// <param name="licenseId"></param>
    /// <param name="title"></param>
    /// <param name="capabilities"></param>
    /// <param name="textMask"></param>
    /// <param name="path"></param>
    /// <param name="validator"></param>
    /// <returns></returns>
    private static int CustomGetLicenseHelper(Guid pluginId, Guid licenseId, [MarshalAs(UnmanagedType.LPWStr)]string title, uint capabilities, [MarshalAs(UnmanagedType.LPWStr)]string textMask, [MarshalAs(UnmanagedType.LPWStr)]string path, IntPtr validator)
    {
      var helper = new ValidatorHelper(validator);
      int product_type = 0; // Undefined
      var result = LicenseUtils.GetLicense(helper.ValidateProductKey, helper.OnLeaseChanged, helper.VerifyLicenseKey, helper.VerifyPreviousVersionLicenseKey, product_type, (int)capabilities, textMask, path, title, pluginId, licenseId);
      return (result ? 1 : 0);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="productId"></param>
    /// <param name="ownerWStringPointer"></param>
    /// <param name="companyWStringPointer"></param>
    /// <returns></returns>
    static bool GetRegisteredOwnerInfoHelper(Guid productId, IntPtr ownerWStringPointer, IntPtr companyWStringPointer)
    {
      var owner = string.Empty;
      var company = string.Empty;
      var rc = LicenseUtils.GetRegisteredOwnerInfo(productId, ref owner, ref company);
      if (!rc) return false;
      UnsafeNativeMethods.ON_wString_Set(ownerWStringPointer, owner);
      UnsafeNativeMethods.ON_wString_Set(companyWStringPointer, company);
      return true;
    }
    private static bool ShowExpiredMessageHelper(Rhino.Runtime.Mode mode, ref int result)
    {
      var success = LicenseUtils.ShowRhinoExpiredMessage(mode, ref result);
      return success;
    }


    // GetInternetTime
    internal delegate bool GetInternetTimeCallback(ref double result);
    private static readonly GetInternetTimeCallback GetInternetTimeProc = GetInternetTimeHelper;
    private static bool GetInternetTimeHelper(ref double result)
    {
      try {
        DateTime internetTime = GetInternetTime();
        result = internetTime.ToOADate();
        return true;
      }
      catch {
        return false;
      }
    }
    private static DateTime GetInternetTime()
    {
      return LicenseUtils.ZooClient.GetCurrentTime();
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="productTitle"></param>
    /// <param name="standAlone"></param>
    /// <param name="parent"></param>
    /// <param name="productId"></param>
    /// <param name="licenseId"></param>
    /// <param name="productBuildType"></param>
    /// <param name="textMask"></param>
    /// <param name="path"></param>
    /// <param name="validator"></param>
    /// <param name="capabilities"></param>
    /// <returns></returns>
    private static bool AskUserForLicenseHelper([MarshalAs(UnmanagedType.LPWStr)] string productTitle,
                                                bool standAlone,
                                                IntPtr parent,
                                                Guid productId,
                                                Guid licenseId,
                                                int productBuildType,
                                                [MarshalAs(UnmanagedType.LPWStr)] string textMask,
                                                [MarshalAs(UnmanagedType.LPWStr)] string path,
                                                IntPtr validator,
                                                uint capabilities)
    {
      var helper = new ValidatorHelper(validator);
      var svc = HostUtils.GetPlatformService<Rhino.UI.IDialogService>();
      if (svc == null)
        return false;
      var parent_control = svc.WrapAsIWin32Window(parent);
      var rc = LicenseUtils.AskUserForLicense(productBuildType, standAlone, parent_control, textMask, helper.ValidateProductKey, helper.OnLeaseChanged, helper.VerifyLicenseKey, helper.VerifyPreviousVersionLicenseKey, path, productTitle, productId, licenseId, (LicenseCapabilities)capabilities);
      return rc;
    }



    /// <summary>
    /// The CRhCmn_ZooClient; ReturnLicense(), CheckOutLicense(),
    /// CheckInLicense(), ConvertLicense(), and GetLicenseType() methods call
    /// this method with the appropriate mode.
    /// </summary>
    /// <param name="mode">Calling function Id</param>
    /// <param name="id">License type Id</param>
    /// <returns></returns>
    static int UuidHelper(int mode, Guid id)
    {
      // Keep these values in synch with the values located at the top
      // of the rh_licensemanager.cpp file in the rhcommon_c project.
      const int modeReturnLicense = 1;
      const int modeCheckOutLicense = 2;
      const int modeCheckInLicense = 3;
      const int modeConvertLicense = 4;
      const int modeGetLicenseType = 5;
      const int modeDeleteLicense = 6;
      switch (mode)
      {
        case modeReturnLicense:
          return LicenseUtils.ReturnLicense(id) ? 1 : 0;
        case modeCheckOutLicense:
          return LicenseUtils.CheckOutLicense(id) ? 1 : 0;
        case modeCheckInLicense:
          return LicenseUtils.CheckInLicense(id) ? 1 : 0;
        case modeConvertLicense:
          return LicenseUtils.ConvertLicense(id) ? 1 : 0;
        case modeGetLicenseType:
          return LicenseUtils.GetLicenseType(id);
        case modeDeleteLicense:
          return LicenseUtils.DeleteLicense(id) ? 1 : 0;
      }
      return 0;
    }
    #endregion rhcommon_c call back methods
  }

  /// <summary>
  /// Different licensing modes that Rhino can run in
  /// </summary>
  public enum LicenseTypes
  {
    /// <summary>
    /// Licensing mode not define
    /// </summary>
    Undefined = 0,
    /// <summary>
    /// Standalone license installed on this computer.
    /// </summary>
    Standalone,
    /// <summary>
    /// Classic Zoo license with the Zoo server automatically detected at runtime.
    /// </summary>
    ZooAutoDetect,
    /// <summary>
    /// Classic Zoo license with the Zoo server specified by hostname or IP
    /// </summary>
    ZooManualDetect,
    /// <summary>
    /// Cloud Zoo licenese
    /// </summary>
    CloudZoo
  }

  /// <summary>
  /// ZooClientParameters is a read-only set of parameters that control
  /// the flow of licensing inside ZooClient. Because this class flows through a number of
  /// other classes, functions, and UI, it is read-only so that inadvertent changes are not
  /// made to the data as it propagates from the caller.
  /// </summary>
  public class ZooClientParameters
  {
    /// <summary>
    /// ZooClientParameters Constructor
    /// </summary>
    /// <param name="productGuid">Guid used by ZooClient to identify the plug-in requesting a license from ZooClient. This Guid may be used by different versions of the
    /// plug-in. If different licenses are used by different versions of the plug-in, the plug-in must also specify a LicenseGuid.</param>
    /// <param name="licenseGuid">Guid used by ZooClient to identify the license saved by ZooClient. This differs from ProductGuid because different versions of a plug-in
    /// with the same Plug-in ID may need different licenses.</param>
    /// <param name="productTitle">Title of product displayed in user interface</param>
    /// <param name="productBuildType"></param>
    /// <param name="capabilities">Enum that declares whether this product supports evalation, licensing, purchasing, and cloud zoo</param>
    /// <param name="licenseEntryTextMask">Text mask used to limit input. EG: RH50-AAAA-AAAA-AAAA-AAAA-AAAA</param>
    /// <param name="productPath">Full path to DLL implementing product</param>
    /// <param name="parentWindow">Object used to parent UI DLLs</param>
    /// <param name="selectedLicenseType">Type of license currently used by this product</param>
    /// <param name="validateProductKey">Delegate called to validate the structure of a license key</param>
    /// <param name="onLeaseChangedDelegate">Delegate called when a cloud zoo lease changes</param>
    /// <param name="verifyLicenseKeyDelegate">Delegate called to verify the structure of a license key</param>
    /// <param name="verifyPreviousVersionLicenseKeyDelegate">Delegate called to verify the structure of an upgrade license key</param>
    public ZooClientParameters(Guid productGuid, Guid licenseGuid, string productTitle, int productBuildType,
      LicenseCapabilities capabilities, string licenseEntryTextMask, string productPath, object parentWindow,
      LicenseTypes selectedLicenseType, ValidateProductKeyDelegate validateProductKey,
      OnLeaseChangedDelegate onLeaseChangedDelegate, VerifyLicenseKeyDelegate verifyLicenseKeyDelegate,
      VerifyPreviousVersionLicenseDelegate verifyPreviousVersionLicenseKeyDelegate)
    {
      ProductGuid = productGuid;
      LicenseGuid = licenseGuid;
      ProductTitle = productTitle;
      ProductBuildType = productBuildType;
      Capabilities = capabilities;
      LicenseEntryTextMask = licenseEntryTextMask;
      ProductPath = productPath;
      ParentWindow = parentWindow;
      SelectedLicenseType = selectedLicenseType;
      ValidateProductKeyDelegate = validateProductKey;
      OnLeaseChanged = onLeaseChangedDelegate;
      VerifyLicenseKeyDelegate = verifyLicenseKeyDelegate;
      m_verifyPreviousVersionLicenseDelegate = verifyPreviousVersionLicenseKeyDelegate;
    }
    /// <summary>
    /// Guid used by ZooClient to identify the plug-in requesting a license from ZooClient. This Guid may be used by different versions of the
    /// plug-in. If different licenses are used by different versions of the plug-in, the plug-in must also specify a LicenseGuid.
    /// </summary>
    public Guid ProductGuid { get; }
    /// <summary>
    /// Guid used by ZooClient to identify the license saved by ZooClient. This differs from ProductGuid because different versions of a plug-in
    /// with the same Plug-in ID may need different licenses.
    /// </summary>
    public Guid LicenseGuid { get; }
    /// <summary>
    /// Title of the product, "Rhinoceros 6" for example.
    /// </summary>
    public string ProductTitle { get; }
    /// <summary>
    /// Product build type. Must be one of LicenseBuildType values.
    /// </summary>
    public int ProductBuildType { get; }
    /// <summary>
    /// LicenseCapabilities flags that set options for how licenses can be obtained for this product
    /// </summary>
    public LicenseCapabilities Capabilities { get; set;}
    /// <summary>
    /// Text mask in the form @"RH4A-AAAA-AAAA-AAAA-AAAA-AAAA" that informs the user what numbers they are looking for
    /// </summary>
    public string LicenseEntryTextMask { get; }
    /// <summary>
    /// Path to the application calling ZooClient
    /// </summary>
    public string ProductPath { get; }
    /// <summary>
    /// Parent window assigned to any licensing dialogs that appear. If null, the Rhino main window is used.
    /// </summary>
    public object ParentWindow { get; }
    /// <summary>
    /// License type selected by default when user is prompted to enter a license key
    /// </summary>
    public LicenseTypes SelectedLicenseType { get; set;  }
    /// <summary>
    /// Delegate called by ZooClient to validate a product key string
    /// </summary>
    private ValidateProductKeyDelegate ValidateProductKeyDelegate { get; }

    /// <summary>
    /// Called during GetLicense to verify that the license key is legitimate and can be used by this user.
    /// </summary>
    private VerifyLicenseKeyDelegate VerifyLicenseKeyDelegate { get; }
    /// <summary>
    ///  Delegate called by ZooClient when a cloud zoo lease is changed
    /// </summary>
    public OnLeaseChangedDelegate OnLeaseChanged { get; }

    /// <summary>
    /// Callback function called during GetLicense when a user enters a license key that requires a previous version license.
    /// A previous call to VerifyLicenseKey resulted in LicenseData.IsUpgradeFromPreviousVersion being set to true.
    /// </summary>
    private VerifyPreviousVersionLicenseDelegate m_verifyPreviousVersionLicenseDelegate;
    /// <summary>
    /// When a caller calls GetLicense, ZooClient may call VerifyPreviousVersionLicense to ensure 
    /// previousVersionLicense is legitimate and can be used to upgrade license.
    /// </summary>
    /// <param name="license">License key for current product. This was returned by a previous call to VerifyLicenseKey or ValidateProductKey.</param>
    /// <param name="previousVersionLicense">License key entered by user to show upgrade eligibility for license.</param>
    /// <param name="errorMessage">Error message to be displayed to user if something isn't correct.</param>
    /// <returns></returns>
    public bool VerifyPreviousVersionLicense(string license, string previousVersionLicense, out string errorMessage)
    {
      if (m_verifyPreviousVersionLicenseDelegate == null)
      {
        errorMessage = Localization.LocalizeString("VerifyPreviousVersionLicenseDelegate not implemented.", 37);
        return false;
      }
      return m_verifyPreviousVersionLicenseDelegate(license, previousVersionLicense, out errorMessage);
    }

    /// <summary>
    /// Called by GetLicense to ensure that the license key entered by the user is legitimate and can be used.
    /// </summary>
    /// <param name="licenseKey">License key string entered by user</param>
    /// <param name="validationCode">Validation code entered by user (only if a previous call to VerifyLicenseKey set LicenseData.RequiresOnlineValidation to true).</param>
    /// <param name="validationCodeInstallDate">Date that validation code was entered by user (only if a previous call to VerifyLicenseKey set LicenseData.RequiresOnlineValidation to true).</param>
    /// <param name="gracePeriodExpired">Date by which license validation must complete successfully.</param>
    /// <param name="licenseData">Output parameter where return data about the license is set.</param>
    /// <returns></returns>
    public ValidateResult VerifyLicenseKey(string licenseKey, string validationCode, DateTime validationCodeInstallDate,
      bool gracePeriodExpired, out LicenseData licenseData)
    {
      if (VerifyLicenseKeyDelegate != null)
        return VerifyLicenseKeyDelegate(licenseKey, validationCode, validationCodeInstallDate, gracePeriodExpired, out licenseData);
      if (ValidateProductKeyDelegate != null)
        return ValidateProductKeyDelegate(licenseKey, out licenseData);

      throw new InvalidOperationException("VerifyLicenseKey called with neither VerifyLicenseKeyDelegate nor ValidateProductKeyDelegate set");
    }

  }

  /// <summary>
  /// Interface implemented in ZooClient and added to Rhino via dependency injection
  /// </summary>
  public interface IZooClientUtilities
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <returns></returns>
    bool Initialize(object verify);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    string Echo(object verify, string message);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    bool ShowRhinoExpiredMessage(Rhino.Runtime.Mode mode, ref int result);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    bool GetLicense(object verify, ZooClientParameters parameters);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="productId"></param>
    /// <returns></returns>
    bool ReturnLicense(object verify, Guid productId);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="productPath"></param>
    /// <param name="productId"></param>
    /// <returns></returns>
    bool ReturnLicense(object verify, string productPath, Guid productId);

    /// <summary>
    /// Checks out a loaned out license from the owning Zoo.
    /// </summary>
    bool CheckOutLicense(object verify, Guid productId);

    /// <summary>
    /// Checks in a checked out license to the owning Zoo.
    /// </summary>
    bool CheckInLicense(object verify, Guid productId);

    /// <summary>
    /// Converts a standalone license to a network license.
    /// </summary>
    bool ConvertLicense(object verify, Guid productId);

    /// <summary>
    /// 31-Mar-2015 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/MR-1725
    /// Deletes a license along with its license file.
    /// </summary>
    bool DeleteLicense(object verify, Guid productId);

    /// <summary>
    /// Returns the type of a specified product license
    /// </summary>
    int GetLicenseType(object verify, Guid productId);

    /// <summary>
    /// Returns whether or not license checkout is enabled
    /// </summary>
    bool IsCheckOutEnabled(object verify);

    /// <summary>
    /// Returns the current status of every license for ui purposes
    /// </summary>
    LicenseStatus[] GetLicenseStatus(object verify);

    /// <summary>
    /// Returns the current status of a license for ui purposes
    /// </summary>
    LicenseStatus GetOneLicenseStatus(object verify, Guid productId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="productId"></param>
    void ShowBuyLicenseUi(object verify, Guid productId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    bool AskUserForLicense(object verify, ZooClientParameters parameters);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="verify"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    bool LicenseOptionsHandler(object verify, ZooClientParameters parameters);

    /// <summary>
    /// Shows user interface to validate and register a license.
    /// Returns true if the license is successfully validated; false otherwise
    /// </summary>
    bool ShowLicenseValidationUi(object verify, string cdkey);

    /// <summary>
    /// Returns the registered owner and organization of a license
    /// 4-Sept-2014 Dale Fugier, http://mcneel.myjetbrains.com/youtrack/issue/RH-28623
    /// </summary>
    bool GetRegisteredOwnerInfo(object verify, Guid productId, ref string registeredOwner, ref string registeredOrganization);

    /// <summary>
    /// Logs the user in to the cloud zoo. This logs out the current user and voids any existing leases.
    /// </summary>
    bool LoginToCloudZoo();

    /// <summary>
    /// Logs the user out of the cloud zoo. This logs out the current user and voids any existing leases.
    /// </summary>
    bool LogoutOfCloudZoo();

    /// <summary>
    /// Returns the logged in user's avatar picture. 
    /// Returns a default avatar if the user does not have an avatar or if the avatar could not be fetched.
    /// </summary>
    System.Drawing.Image LoggedInUserAvatar { get; }

    /// <summary>
    /// Returns the name of the logged in user, or null if the user is not logged in.
    /// </summary>
    string LoggedInUserName { get; }

    /// <summary>
    /// Returns true if the user is logged in; else returns false.
    /// A logged in user does not guarantee that the auth tokens managed by the CloudZooManager instance are valid.
    /// </summary>
    bool UserIsLoggedIn { get; }

    /// <summary>
    /// Gets the current time, trying to access an NTP server before using local time.
    /// </summary>
    /// <returns></returns>
    DateTime GetCurrentTime();
  }

  class DoNothingZooClientUtilities : IZooClientUtilities
  {
    public System.Drawing.Image LoggedInUserAvatar
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public string LoggedInUserName
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public bool UserIsLoggedIn
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public DateTime GetCurrentTime()
    {
      throw new NotImplementedException();
    }

    public DateTime GetInternetTime()
    {
      throw new NotImplementedException();
    }

    public bool CheckInLicense(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public bool CheckOutLicense(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public bool ConvertLicense(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public bool DeleteLicense(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public string Echo(object verify, string message)
    {
      throw new NotImplementedException();
    }

    public bool GetLicense(object verify, ZooClientParameters parameters)
    {
      throw new NotImplementedException();
    }

    public LicenseStatus[] GetLicenseStatus(object verify)
    {
      throw new NotImplementedException();
    }

    public int GetLicenseType(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public LicenseStatus GetOneLicenseStatus(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public bool GetRegisteredOwnerInfo(object verify, Guid productId, ref string registeredOwner, ref string registeredOrganization)
    {
      throw new NotImplementedException();
    }

    public bool Initialize(object verify)
    {
      throw new NotImplementedException();
    }

    public bool IsCheckOutEnabled(object verify)
    {
      throw new NotImplementedException();
    }

    public bool LoginToCloudZoo()
    {
      throw new NotImplementedException();
    }

    public bool LogoutOfCloudZoo()
    {
      throw new NotImplementedException();
    }

    public bool ReturnLicense(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }

    public bool ReturnLicense(object verify, string productPath, Guid productId)
    {
      throw new NotImplementedException();
    }

    public void ShowBuyLicenseUi(object verify, Guid productId)
    {
      throw new NotImplementedException();
    }
    public bool ShowLicenseValidationUi(object verify, string cdkey)
    {
      throw new NotImplementedException();
    }

    public bool ShowRhinoExpiredMessage(Rhino.Runtime.Mode mode, ref int result)
    {
      throw new NotImplementedException();
    }

    bool IZooClientUtilities.AskUserForLicense(object verify, ZooClientParameters parameters)
    {
      throw new NotImplementedException();
    }

    bool IZooClientUtilities.LicenseOptionsHandler(object verify, ZooClientParameters parameters)
    {
      throw new NotImplementedException();
    }
  }
}
#endif
