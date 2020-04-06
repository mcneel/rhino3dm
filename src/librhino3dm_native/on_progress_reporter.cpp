#include "stdafx.h"

typedef void (CALLBACK* PROGRESSREPORTERPROC)(int serialNumber, double fractionComplete);

static PROGRESSREPORTERPROC g_report_proc = nullptr;

static void CRhCmnProgressCallback(ON__UINT_PTR context, double fraction_complete )
{
  if (0 != context && g_report_proc)
  {
    int serial_number = (int)context;
    //Do function call to .NET
    g_report_proc(serial_number, fraction_complete);
  }
}


CRhCmnProgressReporter::CRhCmnProgressReporter(int serialNumber)
: m_serial_number(serialNumber)
{
  if ( serialNumber> 0)
    SetSynchronousProgressCallbackFunction(CRhCmnProgressCallback, serialNumber);
}


RH_C_FUNCTION void ON_ProgressReporter_SetReportCallback(PROGRESSREPORTERPROC callback)
{
  g_report_proc = callback;
}