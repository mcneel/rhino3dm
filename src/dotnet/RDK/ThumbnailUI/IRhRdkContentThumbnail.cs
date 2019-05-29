#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Render;

namespace Rhino.UI.Controls.ThumbnailUI
{
    [Obsolete("IRhRdkContentThumbnail is obsolete", false)]
    // C# version of IRhRdkContentThumbnail
    public interface IRhRdkContentThumbnail : IRhRdkThumbnail
    {
        /*
        virtual const CRhRdkContent* TopLevelContent(void) const = 0;
        virtual const CRhRdkContent* ChildContent(void) const = 0;
        virtual const CRhRdkPreviewAppearance& Appearance(void) const = 0;
        virtual UUID GroupId(void) const = 0;
        */

        Rhino.Render.RenderContent TopLevelContent();

        Rhino.Render.RenderContent ChildContent();

        // Not yet defined
        // CRhRdkPreviewAppearance& Appearance(void) const = 0;

        Guid GroupId();

    }
}
