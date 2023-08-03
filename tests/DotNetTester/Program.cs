
using Rhino.Geometry;
using Rhino.FileIO;
using Rhino.Render;
using Rhino.Render.PostEffects;
using System.Globalization;

namespace DotNetTester
{
  internal class Program
  {
    static void Main(string[] args)
    {
      string pathThis = typeof(Program).Assembly.Location;
      int rhino3dmIndex = pathThis.IndexOf("rhino3dm");
      string pathLibRhino3dm = pathThis.Substring(0, rhino3dmIndex);
      pathLibRhino3dm += "rhino3dm\\src\\build\\librhino3dm_native_64\\Debug\\librhino3dm_native.dll";

      // Force load librhino3dm
      nint handleLibRhino3dm = System.Runtime.InteropServices.NativeLibrary.Load(pathLibRhino3dm);

      var points = new Point3d[] { new Point3d(0, 0, 0), new Point3d(5, 0, 0), new Point3d(5, 5, 0) };
      Rhino.Geometry.Curve curve = Rhino.Geometry.Curve.CreateControlPointCurve(points);
      bool isLinear = curve.IsLinear();
      Console.WriteLine($"curve.IsLinear = {isLinear}");

      // JohnC https://mcneel.myjetbrains.com/youtrack/issue/RH-69002
      // Test RDK document objects.

      string in_file  = "test_in.3dm";
      string out_file = "test_out.3dm";

      int binIndex = pathThis.IndexOf("bin");
      string dir = pathThis.Substring(0, binIndex);

      var file3dm = TestFile(dir, in_file);

      file3dm.Write(dir + out_file, new File3dmWriteOptions());

      TestFile(dir, out_file);

      SunEphemeris();
    }

    static File3dm TestFile(string dir, string file)
    {
      Console.WriteLine("");
      Console.WriteLine(file);
      Console.WriteLine("----------------");

      string in_file = dir + file;
      var file3dm = File3dm.Read(in_file);

      var rs = file3dm.Settings.RenderSettings;

      foreach (var m in file3dm.RenderMaterials)
      {
        var k  = m.Kind;
        var tn = m.TypeName;
        var tl = m.TopLevel;

        DisplayRenderContent(m);

        foreach (var c in m.Children)
        {
          DisplayRenderContent(c);

          if (c is File3dmRenderTexture tex)
          {
            IConvertible p = tex.GetParameter("color-one");
            if (p != null)
            {
              var s = Convert.ToString(p);
              Console.WriteLine(s);
            }
          }
        }

        // Can't do this because constructors are internal.
        // var tid = new Guid("19DB2D71-13E0-4E29-BF6A-C80F616129DE");
        // var t = new File3dmRenderTexture(tid, file3dm);
        // m.Children.Append(t);
      }

      GroundPlaneTest(rs.GroundPlane);
      DitheringTest(rs.Dithering);
      SafeFrameTest(rs.SafeFrame);
      SkylightTest(rs.Skylight);
      LinearWorkflowTest(rs.LinearWorkflow);
      RenderChannelsTest(rs.RenderChannels);
      SunTest(rs.Sun);
      CurrentEnvTest(rs);
      PostEffectCollectionTest(rs.PostEffects);

      var ge = file3dm.Manifest.GetEnumerator(Rhino.DocObjects.ModelComponentType.ModelGeometry);
      while (ge.MoveNext())
      {
        if (ge.Current is File3dmObject obj)
        {
          DecalTest(obj.Attributes.Decals);
        }
      }

      return file3dm;
    }

    static void DisplayRenderContent(File3dmRenderContent rc)
    {
      Console.WriteLine(rc.TypeName
                        + " "
                        + ", TypeId: "         + rc.TypeId.ToString()
                        + ", RenderEngineId: " + rc.RenderEngineId.ToString()
                        + ", PlugInId: "       + rc.PlugInId.ToString()
                        + ", Notes: "          + rc.Notes
                        + ", Tags: "           + rc.Tags
                        + ", CSN: "            + rc.ChildSlotName
      );
    }

    static void GroundPlaneTest(GroundPlane gp)
    {
      Console.WriteLine("BEGIN GroundPlane");
      Console.WriteLine("On:                  {0}", gp.Enabled);
      Console.WriteLine("ShowUnderside:       {0}", gp.ShowUnderside);
      Console.WriteLine("Altitude:            {0}", gp.Altitude);
      Console.WriteLine("AutoAltitude:        {0}", gp.AutoAltitude);
      Console.WriteLine("ShadowOnly:          {0}", gp.ShadowOnly);
      Console.WriteLine("MaterialInstanceId:  {0}", gp.MaterialInstanceId);
      Console.WriteLine("TextureOffset:       {0}", gp.TextureOffset);
      Console.WriteLine("TextureOffsetLocked: {0}", gp.TextureOffsetLocked);
      Console.WriteLine("TextureSize:         {0}", gp.TextureSize);
      Console.WriteLine("TextureSizeLocked:   {0}", gp.TextureSizeLocked);
      Console.WriteLine("TextureRotation:     {0}", gp.TextureRotation);

      Console.WriteLine("GroundPlane set TextureSize");
      gp.TextureSize = new Vector2d(1.2, 3.4);
      Console.WriteLine("TextureSize:         {0}", gp.TextureSize);
      Console.WriteLine("END GroundPlane");
    }

    static void DitheringTest(Dithering dit)
    {
      Console.WriteLine("BEGIN Dithering");
      Console.WriteLine("On:      {0}", dit.On);
      Console.WriteLine("Method:  {0}", dit.Method);

      Console.WriteLine("Dithering set Method FloydSteinberg");
      dit.On = true;
      dit.Method = Dithering.Methods.FloydSteinberg;
      Console.WriteLine("Method:  {0}", dit.Method);
      Console.WriteLine("Dithering set Method SimpleNoise");
      dit.Method = Dithering.Methods.SimpleNoise;
      Console.WriteLine("Method:  {0}", dit.Method);
      Console.WriteLine("END Dithering");
    }

    static void SafeFrameTest(SafeFrame sf)
    {
      Console.WriteLine("BEGIN SafeFrame");
      Console.WriteLine("On:                {0}", sf.Enabled);
      Console.WriteLine("PerspectiveOnly:   {0}", sf.PerspectiveOnly);
      Console.WriteLine("FieldGridOn:       {0}", sf.FieldsOn);
      Console.WriteLine("ActionFrameOn:     {0}", sf.ActionFrameOn);
      Console.WriteLine("ActionFrameLinked: {0}", sf.ActionFrameLinked);
      Console.WriteLine("ActionFrameXScale: {0}", sf.ActionFrameXScale);
      Console.WriteLine("ActionFrameYScale: {0}", sf.ActionFrameYScale);
      Console.WriteLine("TitleFrameOn:      {0}", sf.TitleFrameOn);
      Console.WriteLine("TitleFrameLinked:  {0}", sf.TitleFrameLinked);
      Console.WriteLine("TitleFrameXScale:  {0}", sf.TitleFrameXScale);
      Console.WriteLine("TitleFrameYScale:  {0}", sf.TitleFrameYScale);

      Console.WriteLine("SafeFrame set ActionFrameXScale 0.45");
      sf.ActionFrameXScale = 0.45;
      Console.WriteLine("ActionFrameXScale: {0}", sf.ActionFrameXScale);
      Console.WriteLine("END SafeFrame");
    }

    static void SkylightTest(Skylight sl)
    {
      Console.WriteLine("BEGIN Skylight");
      Console.WriteLine("On:                  {0}", sl.Enabled);
      Console.WriteLine("ShadowIntensity:     {0}", sl.ShadowIntensity);

      Console.WriteLine("Skylight set ShadowIntensity to 23");
      sl.ShadowIntensity = 23.0;
      Console.WriteLine("ShadowIntensity: {0}", sl.ShadowIntensity);
      Console.WriteLine("END Skylight");
    }

    static void SunTest(Sun sun)
    {
      Console.WriteLine("BEGIN Sun");

      sun.Enabled = true;

      Console.WriteLine("Enable On:             {0}", sun.Enabled);
      Console.WriteLine("ManualControlOn:       {0}", sun.ManualControlOn);
      Console.WriteLine("North:                 {0}", sun.North);
      Console.WriteLine("Vector:                {0}", sun.Vector);
      Console.WriteLine("Azimuth:               {0}", sun.Azimuth);
      Console.WriteLine("Altitude:              {0}", sun.Altitude);
      Console.WriteLine("Latitude:              {0}", sun.Latitude);
      Console.WriteLine("Longitude:             {0}", sun.Longitude);
      Console.WriteLine("TimeZone:              {0}", sun.TimeZone);
      Console.WriteLine("DaylightSavingOn:      {0}", sun.DaylightSavingOn);
      Console.WriteLine("DaylightSavingMinute:  {0}", sun.DaylightSavingMinutes);

      var dt = sun.GetDateTime(DateTimeKind.Local);
      var format = "LocalDateTime:         {0}.{1}.{2} {3}:{4}:{5}";
      var s = string.Format(CultureInfo.InvariantCulture, format, dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
      Console.WriteLine(s);

      Console.WriteLine("Sun set lat/long to Greenwich");
      sun.Latitude = 51.4934;
      sun.Longitude = 0.0098;

      // Sun set date and time.
      dt = new DateTime(1983, 5, 30, 17, 41, 19, DateTimeKind.Local);
      sun.SetDateTime(dt, dt.Kind);

      Console.WriteLine("Intensity:             {0}", sun.Intensity);
      Console.WriteLine("Hash:                  {0}", sun.Hash);

      var light = sun.Light;
      Console.WriteLine("Light:                 {0}", light.Direction);
      Console.WriteLine("Color:                 {0}", Sun.ColorFromAltitude(sun.Altitude));
      Console.WriteLine("END Sun");
    }

    static void CurrentEnvTest(RenderSettings rs)
    {
      var Standard  = RenderSettings.EnvironmentPurpose.Standard;
      var Rendering = RenderSettings.EnvironmentPurpose.ForRendering;

      InternalCurrentEnvTest(rs, RenderSettings.EnvironmentUsage.Background,  Standard);
      InternalCurrentEnvTest(rs, RenderSettings.EnvironmentUsage.Skylighting, Standard);
      InternalCurrentEnvTest(rs, RenderSettings.EnvironmentUsage.Reflection,  Standard);
      InternalCurrentEnvTest(rs, RenderSettings.EnvironmentUsage.Background,  Rendering);
      InternalCurrentEnvTest(rs, RenderSettings.EnvironmentUsage.Skylighting, Rendering);
      InternalCurrentEnvTest(rs, RenderSettings.EnvironmentUsage.Reflection,  Rendering);
    }
    
    static void InternalCurrentEnvTest(RenderSettings rs, RenderSettings.EnvironmentUsage usage, RenderSettings.EnvironmentPurpose purpose)
    {
      Console.WriteLine("BEGIN CurrentEnv");
      Console.WriteLine("Override:       {0}", rs.RenderEnvironmentOverride(usage));
      Console.WriteLine("Environment id: {0}", rs.RenderEnvironmentId(usage, purpose));
    
      Console.WriteLine("CurrentEnv SetRenderEnvironmentId Guid.Empty");
      rs.SetRenderEnvironmentId(usage, Guid.Empty);
      Console.WriteLine("Environment id: {0}", rs.RenderEnvironmentId(usage, purpose));
    
      Console.WriteLine("CurrentEnv SetRenderEnvironmentId 12345678");
      var id = new Guid("{ 0x12345678, 0xFFFF, 0xABBA, { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 } }");
      rs.SetRenderEnvironmentId(usage, id);
      Console.WriteLine("Environment id: {0}", rs.RenderEnvironmentId(usage, purpose));
    
      Console.WriteLine("END CurrentEnv");
    }

    static void LinearWorkflowTest(LinearWorkflow lw)
    {
      Console.WriteLine("BEGIN Linear Workflow");
      Console.WriteLine("PreProcessTexturesOn: {0}", lw.PreProcessTextures);
      Console.WriteLine("PreProcessColorsOn:   {0}", lw.PreProcessColors);
      Console.WriteLine("PostProcessGammaOn:   {0}", lw.PostProcessGammaOn);
      Console.WriteLine("PreProcessGamma:      {0}", lw.PreProcessGamma);
      Console.WriteLine("PostProcessGamma:     {0}", lw.PostProcessGamma);
      Console.WriteLine("DataCRC:              {0}", lw.Hash);

      Console.WriteLine("Linear Workflow set pre gamma 1.2, post gamma 3.4");
      lw.PreProcessGamma  = 1.2f;
      lw.PostProcessGamma = 3.4f;

      Console.WriteLine("PreProcessGamma:      {0}", lw.PreProcessGamma);
      Console.WriteLine("PostProcessGamma:     {0}", lw.PostProcessGamma);
      Console.WriteLine("END Linear Workflow");
    }

    static void RenderChannelsTest(RenderChannels rch)
    {
      Console.WriteLine("BEGIN Render Channels");
      Console.WriteLine("Mode: {0}", rch.Mode);
      Console.WriteLine("Custom List:");
      int count = 0;
      var list = rch.CustomList;
      foreach (var id in list)
      {
        Console.WriteLine("  {1}: {0}", id, count++);
      }

      Console.WriteLine("Render Channels set mode custom");
      rch.Mode = RenderChannels.Modes.Custom;
      Console.WriteLine("Mode: {0}", rch.Mode);
      Console.WriteLine("Render Channels set mode automatic");

      Console.WriteLine("END Render Channels");
    }

    static void PostEffectCollectionTest(PostEffectCollection peps)
    {
      Console.WriteLine("BEGIN Post Effect collection");

      var id1 = Guid.Empty;
      var id2 = Guid.Empty;

      foreach (var pep in peps)
      {
        PostEffectTest(pep);
        id2 = id1;
        id1 = pep.Id;
      }

      var data1 = peps.PostEffectDataFromId(id1);
      var type1 = data1.Type;
      Console.WriteLine("id1: {0}", data1.LocalName);

      var data2 = peps.PostEffectDataFromId(id2);
      Console.WriteLine("id2: {0}", data2.LocalName);

      peps.MovePostEffectBefore(id1, id2);

      peps.GetSelectedPostEffect(type1, out id1);
      if (id1 != Guid.Empty)
      {
        data1 = peps.PostEffectDataFromId(id1);
        Console.WriteLine("Early Post Effect selection is {0}", data1.LocalName);
      }

      peps.SetSelectedPostEffect(type1, id2);

      peps.GetSelectedPostEffect(type1, out id1);
      data1 = peps.PostEffectDataFromId(id1);
      Console.WriteLine("{0} Post Effect selection is {1}", type1, data1.LocalName);

      Console.WriteLine("END Post Effect collection");
    }

    static void PostEffectTest(PostEffectData pep)
    {
      var ci = CultureInfo.InvariantCulture;
    
      Console.WriteLine("  BEGIN Post Effect");

      Console.WriteLine("    Id:        {0}", pep.Id);
      Console.WriteLine("    Type:      {0}", pep.Type);
      Console.WriteLine("    LocalName: {0}", pep.LocalName);
      Console.WriteLine("    On:        {0}", pep.On);
      Console.WriteLine("    Shown:     {0}", pep.Shown);
      Console.WriteLine("    DataCRC:   {0}", pep.DataCRC(0));

      var p = pep.GetParameter("radius");
      if (p != null) Console.WriteLine("    Radius:    {0}", p.ToDouble(ci));

      pep.SetParameter("radius", 0.33);

      p = pep.GetParameter("radius");
      if (p != null) Console.WriteLine("    New Radius:{0}", p.ToDouble(ci));

      p = pep.GetParameter("brightness");
      if (p != null) Console.WriteLine("    Brightness:{0}", p.ToDouble(ci));

      p = pep.GetParameter("bias");
      if (p != null) Console.WriteLine("    Bias:      {0}", p.ToDouble(ci));

      Console.WriteLine("  END Post Effect");
    }

    static void DecalTest(Decals decals)
    {
      foreach (var decal in decals)
      {
        Console.WriteLine("  CRC:          {0}", decal.CRC);
        Console.WriteLine("  Mapping:      {0}", decal.DecalMapping);
        Console.WriteLine("  Projection:   {0}", decal.DecalProjection);
        Console.WriteLine("  Origin:       {0}", decal.Origin);
        Console.WriteLine("  Transparency: {0}", decal.Transparency);
        Console.WriteLine("  Texture Id:   {0}", decal.TextureInstanceId);
        Console.WriteLine("  Radius:       {0}", decal.Radius);
        Console.WriteLine("  Height:       {0}", decal.Height);
        Console.WriteLine("  VectorUp:     {0}", decal.VectorUp);
        Console.WriteLine("  VectorAcross: {0}", decal.VectorAcross);
        Console.WriteLine("  MapToInside:  {0}", decal.MapToInside);

        Console.WriteLine("  BEGIN Custom Data");

        // This test Guid is a renderer (Toast) that is known to create custom decal data.
        var render_engine_id  = Guid.Parse("BBBBBBAD-BBAD-BBAD-BBAD-BADBADBADBAD");
        var list = decal.CustomData(render_engine_id);
        foreach (var item in list)
        {
          if (item.Value is Rhino.Display.Color4f c4)
          {
            Console.WriteLine($"    {item.Name} = {c4.R}, {c4.G}, {c4.B}, {c4.A}");
          }
          else
          {
            Console.WriteLine($"    {item.Name} = {item.Value}");
          }
        }

        Console.WriteLine("  END Custom Data");

        double min_u = 0.0, min_v = 0.0, max_u = 0.0, max_v = 0.0;
        decal.UVBounds(ref min_u, ref min_v, ref max_u, ref max_v);
        Console.WriteLine("  Min U:        {0}", min_u);
        Console.WriteLine("  Min V:        {0}", min_v);
        Console.WriteLine("  Max U:        {0}", max_u);
        Console.WriteLine("  Max V:        {0}", max_v);

        decal.HorzSweep(out var sta, out var end);
        Console.WriteLine("  H-Sweep Sta:  {0}", sta);
        Console.WriteLine("  H-Sweep End:  {0}", end);

        decal.VertSweep(out sta, out end);
        Console.WriteLine("  V-Sweep Sta:  {0}", sta);
        Console.WriteLine("  V-Sweep End:  {0}", end);
      }
    }

    static private void SunEphemeris()
    {
      var sun = new Sun
      {
        Accuracy = Sun.Accuracies.Maximum,
        ManualControlOn = false,
        DaylightSavingOn = false,
        Latitude = 47.606,
        Longitude = -122.331,
        TimeZone = -8.0
      };

      int day = 1, year = 2013;
      for (int i = 0; i < 12; i++)
      {
        int month = i + 1;

        var dt = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Local);
        sun.SetDateTime(dt, DateTimeKind.Local);

        Console.WriteLine("Noon on {0}.{1}.{2} - Azimuth: {3}, Altitude: {4}",
                           year, month, day, sun.Azimuth, sun.Altitude);
      }
    }
  }
}
