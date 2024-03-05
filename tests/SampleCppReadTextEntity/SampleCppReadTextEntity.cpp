#include <sstream>
#include <iostream>

#include "../../src/lib/opennurbs/opennurbs_public.h"

int main(int argc, char *argv[])
{
    std::cout << "Reading textEntities_r8.3dm!" << std::endl;
    ON::Begin();

    const std::wstring sFileName = L"textEntities_r8.3dm";

    ON_TextLog log;

    FILE *fp = ON_FileStream::Open3dmToRead(sFileName.c_str());

    if (!fp)
    {
        return false;
    }

    ON_BinaryFile archive(ON::archive_mode::read3dm, fp);

    ONX_Model model;
    bool rc = model.Read(archive, &log);

    ON::CloseFile(fp);
    fp = nullptr;

    if (!rc)
    {
        return false;
    }

    ON_ClassArray<ON_ModelComponentReference> m_compref_cache;
    ONX_ModelComponentIterator it(model, ON_ModelComponent::Type::ModelGeometry);
    unsigned int it_count = 0;
    int count = model.ActiveComponentCount(ON_ModelComponent::Type::ModelGeometry) + model.ActiveComponentCount(ON_ModelComponent::Type::RenderLight);

    for (ON_ModelComponentReference mcr = it.FirstComponentReference(); false == mcr.IsEmpty(); mcr = it.NextComponentReference())
    {
        if (m_compref_cache.Count() == 0)
        {
            m_compref_cache.Reserve(count);
            ONX_ModelComponentIterator iterator(model, ON_ModelComponent::Type::ModelGeometry);
            ON_ModelComponentReference compref = iterator.FirstComponentReference();
            while (!compref.IsEmpty())
            {
                m_compref_cache.Append(compref);
                compref = iterator.NextComponentReference();
            }

            ONX_ModelComponentIterator iterator2(model, ON_ModelComponent::Type::RenderLight);
            compref = iterator2.FirstComponentReference();
            while (!compref.IsEmpty())
            {
                m_compref_cache.Append(compref);
                compref = iterator2.NextComponentReference();
            }
        }

        //const ON_ModelComponent *model_component = mcr.ModelComponent();
        //std::cout << model_component->ObjectType() << std::endl;
    }

    for(int i = 0; i < count; i ++)
    {
        ON_ModelComponentReference compref = m_compref_cache[i];

        const ON_ModelComponent* model_component = compref.ModelComponent();
        const ON_ModelGeometryComponent* geometryComponent = ON_ModelGeometryComponent::Cast(model_component);
       // std::cout << geometryComponent->ObjectType() << std::endl;
        
        ON_Object* obj = const_cast<ON_Geometry*>(geometryComponent->Geometry(nullptr));
        ON_Geometry* geometry = ON_Geometry::Cast(obj);
        std::cout << geometry->ObjectType() << std::endl;
        ON_Annotation* annotation = ON_Annotation::Cast(obj);
        std::wstring pt(annotation->PlainText());
        wprintf(annotation->PlainText());
        std::cout << std::endl;

        wprintf(annotation->RichText());
        std::cout << std::endl;

        wprintf(annotation->PlainTextWithFields());
        std::cout << std::endl;
        //std::cout << pt.c_str() << std::endl;
        //std::cout << annotation->RichText().Array() << std::endl;
        //std::cout << annotation->PlainTextWithFields().Array() << std::endl;
    }

    std::cout << "Bye!" << std::endl;

    ON::End();
}