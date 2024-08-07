#include <gtest/gtest.h>
#include "../../src/lib/opennurbs/opennurbs_public.h"
#include <sstream>
#include <iostream>
#include <filesystem>

namespace fs = std::filesystem;

TEST(ONTest, ONTestViewPort)
{

    ON::Begin();

    // errors printed to stdout
    ON_TextLog error_log;

    //create model
    ONX_Model model;

    // create viewport
    ON_3dmView view;

    view.m_name = L"Test View";
    ON_3dPoint loc(50, 50, 50);
    view.m_vp.SetCameraLocation(loc);

    model.m_settings.m_views.Append(view);

    ON_Sphere sphere1(ON_3dPoint(0, 0, 0), 10);
    ON_NurbsSurface s1;
    sphere1.GetNurbForm(s1);
    model.AddModelGeometryComponent(&s1, nullptr);
    ON_Sphere sphere2(ON_3dPoint(50, 50, 0), 10);
    ON_NurbsSurface s2;
    sphere2.GetNurbForm(s2);
    model.AddModelGeometryComponent(&s2, nullptr);
    ON_Sphere sphere3(ON_3dPoint(100, 100, 100), 10);
    ON_NurbsSurface s3;
    sphere3.GetNurbForm(s3);
    model.AddModelGeometryComponent(&s3, nullptr);

    fs::path dir = fs::current_path();

    if (dir.filename().string() == "build")
    {
        dir = dir.parent_path();
    }

     //save file
    int version = 0;
    const wchar_t* filename = L"file3dmWithViews_cpp.3dm";

    std::wstring full_path = dir.wstring() + L"\\" + filename;

    // writes model to archive
    model.Write( full_path.c_str(), version, &error_log );

    FILE *fp = ON_FileStream::Open3dmToRead(full_path.c_str());

    ON_BinaryFile archive(ON::archive_mode::read3dm, fp);

    ONX_Model model_read;
    ON_TextLog log;
    bool rc = model_read.Read(archive, &log);

    ON_3dPoint loc_read(model_read.m_settings.m_views[0].m_vp.CameraLocation() );

    std::cout << "loc      " + std::to_string(loc.x) + "," + std::to_string(loc.y) + "," + std::to_string(loc.z) << std::endl;
    std::cout << "loc_read " + std::to_string(loc_read.x) + "," + std::to_string(loc_read.y) + "," + std::to_string(loc_read.z) << std::endl;

    EXPECT_EQ(loc.x, loc_read.x);
    EXPECT_EQ(loc.y, loc_read.y);
    EXPECT_EQ(loc.z, loc_read.z);

    ON::End();
}