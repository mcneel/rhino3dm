#include "bnd_file3dmplugindata.h"

// the entry's own user data: the first of that plug-in's that is RDK document data
// (the old File3dmRdkDocumentData was the table entry itself, by index)
std::wstring BND_File3dmPlugInData_RdkXml(const BND_File3dmPlugInData& data)
{
  ON_wString xml;
  const ONX_Model* model = data.m_ptr;
  for (int i = 0; model && i < model->m_userdata_table.Count(); i++)
  {
    const ONX_Model_UserData* ud = model->m_userdata_table[i];
    if (ud && ud->m_uuid == data.m_id && ONX_Model::GetRDKDocumentInformation(*ud, xml))
      return std::wstring(xml.Array());
  }
  return std::wstring();
}
