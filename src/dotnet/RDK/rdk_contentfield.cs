#pragma warning disable 1591
#if RHINO_SDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Runtime.InteropWrappers;


namespace Rhino.Render.Fields
{
  /// <summary>
  /// Dictionary containing RenderContent data fields, add fields to this
  /// dictionary in your derived RenderContent classes constructor.  Get field
  /// values using the TryGet[data type]() methods and set them using the Set()
  /// method.
  /// </summary>
  /// <example>
  /// [System.Runtime.InteropServices.Guid("ABE4059B-9BD7-451C-91B2-67C2F188860A")]
  /// public class CustomMaterial : RenderMaterial
  /// {
  ///   public override string TypeName { get { return "CSharp Custom Material"; } }
  ///   public override string TypeDescription { get { return "My first custom .NET material"; } }
  /// 
  ///   public CustomMaterial()
  ///   {
  ///     Fields.AddField("bool", false, "Yes/No");
  ///     Fields.AddField("color", Rhino.Display.Color4f.White, "Color");
  ///   }
  /// }
  /// </example>
  public sealed class FieldDictionary : IEnumerable
  {
    #region members
    /// <summary>
    /// RenderContent that owns this dictionary.
    /// </summary>
    private readonly RenderContent m_content;

    #endregion members

    #region Construction/creation
    /// <summary>
    /// Internal constructor, this object should only be created by the
    /// RenderContent constructor.
    /// </summary>
    /// <param name="content">Owner of this dictionary</param>
    internal FieldDictionary(RenderContent content)
    {
      m_content = content;
    }
    #endregion Construction/creation

    #region Private and internal helper methods

    internal Field FieldFromName(string name)
    {
      IntPtr field_pointer = FieldPointerFromName(name);
      if (IntPtr.Zero == field_pointer)
      {
        return null;
      }

      Field result = Field.FieldFromPointer(m_content, field_pointer);
      return result;
    }

    internal IntPtr FieldPointerFromName(string name)
    {
      IntPtr content_pointer = m_content.ConstPointer();
      if (IntPtr.Zero == content_pointer)
      {
        return IntPtr.Zero;
      }

      return UnsafeNativeMethods.Rdk_RenderContent_FindField(content_pointer, name);
    }

    #endregion Private and internal helper methods

    #region Public methods
    /// <summary>
    /// Call this method to determine if a this FieldsList contains a field
    /// with the specified field name.
    /// </summary>
    /// <param name="fieldName">Field to search for</param>
    /// <returns>
    /// Returns true if a field with that matches fieldName is found or false
    /// if it is not found.
    /// </returns>
    public bool ContainsField(string fieldName)
    {
      if (string.IsNullOrEmpty(fieldName))
      {
        return false;
      }

      var found = FieldFromName(fieldName);
      return (null != found);
    }
    /// <summary>
    /// Call this method to get the field with the matching name.
    /// </summary>
    /// <param name="fieldName">Field name to search for.</param>
    /// <returns>
    /// If the field exists in the Fields dictionary then the field is returned
    /// otherwise; null is returned.
    /// </returns>
    public Field GetField(string fieldName)
    {
      if (string.IsNullOrEmpty(fieldName))
      {
        return null;
      }

      return FieldFromName(fieldName);
    }

    public void RemoveField(string fieldName)
    {
      var field = GetField(fieldName);
      if (null != field)
      {
        UnsafeNativeMethods.Rdk_ContentField_Delete(FieldPointerFromName(fieldName));
      }
    }

    #endregion Public methods

    #region Overloaded AddField methods for the supported data types
    /// <summary>
    /// Add a new StringField to the dictionary.  This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public StringField Add(string key, string value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new StringField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public StringField Add(string key, string value, string prompt)
    {
      return new StringField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new StringField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public StringField AddTextured(string key, string value, string prompt)
    {
      return new StringField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new BoolField to the dictionary. This will be a data only field
    /// and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public BoolField Add(string key, bool value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new BoolField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public BoolField Add(string key, bool value, string prompt)
    {
      return new BoolField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new BoolField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public BoolField AddTextured(string key, bool value, string prompt)
    {
      return new BoolField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new IntField to the dictionary. This will be a data only field
    /// and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public IntField Add(string key, int value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new IntField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public IntField Add(string key, int value, string prompt)
    {
      return new IntField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new IntField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public IntField AddTextured(string key, int value, string prompt)
    {
      return new IntField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new FloatField to the dictionary. This will be a data only field
    /// and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public FloatField Add(string key, float value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// AddField a new FloatField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public FloatField Add(string key, float value, string prompt)
    {
      return new FloatField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new FloatField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public FloatField AddTextured(string key, float value, string prompt)
    {
      return new FloatField(m_content, key, prompt, value, true);
    }
    /// <summary>
    /// AddField a new DoubleField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public DoubleField Add(string key, double value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new DoubleField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public DoubleField Add(string key, double value, string prompt)
    {
      return new DoubleField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new DoubleField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public DoubleField AddTextured(string key, double value, string prompt)
    {
      return new DoubleField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new Color4fField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Color4fField Add(string key, Color4f value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Color4fField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Color4fField Add(string key, Color4f value, string prompt)
    {
      return new Color4fField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new Color4fField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Color4fField AddTextured(string key, Color4f value, string prompt)
    {
      return new Color4fField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new Color4fField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Color4fField Add(string key, System.Drawing.Color value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Color4fField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Color4fField Add(string key, System.Drawing.Color value, string prompt)
    {
      return Add(key, new Color4f(value), prompt);
    }

    /// <summary>
    /// Add a new Color4fField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Color4fField AddTextured(string key, System.Drawing.Color value, string prompt)
    {
      return AddTextured(key, new Color4f(value), prompt);
    }

    /// <summary>
    /// Add a new Vector2dField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Vector2dField Add(string key, Vector2d value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Vector2dField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Vector2dField Add(string key, Vector2d value, string prompt)
    {
      return new Vector2dField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new Vector2dField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Vector2dField AddTextured(string key, Vector2d value, string prompt)
    {
      return new Vector2dField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new Vector3dField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Vector3dField Add(string key, Vector3d value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Vector3dField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Vector3dField Add(string key, Vector3d value, string prompt)
    {
      return new Vector3dField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new Vector3dField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Vector3dField AddTextured(string key, Vector3d value, string prompt)
    {
      return new Vector3dField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new Point2dField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point2dField Add(string key, Point2d value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Point2dField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point2dField Add(string key, Point2d value, string prompt)
    {
      return new Point2dField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new Point2dField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point2dField AddTextured(string key, Point2d value, string prompt)
    {
      return new Point2dField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new Point3dField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point3dField Add(string key, Point3d value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Point3dField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point3dField Add(string key, Point3d value, string prompt)
    {
      return new Point3dField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new Point3dField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point3dField AddTextured(string key, Point3d value, string prompt)
    {
      return new Point3dField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new Point4dField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point4dField Add(string key, Point4d value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new Point4dField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point4dField Add(string key, Point4d value, string prompt)
    {
      return new Point4dField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new Point4dField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public Point4dField AddTextured(string key, Point4d value, string prompt)
    {
      return new Point4dField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new GuidField to the dictionary. This will be a data only field
    /// and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public GuidField Add(string key, Guid value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new GuidField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public GuidField Add(string key, Guid value, string prompt)
    {
      return new GuidField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new GuidField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public GuidField AddTextured(string key, Guid value, string prompt)
    {
      return new GuidField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new TransformField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public TransformField Add(string key, Transform value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new TransformField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public TransformField Add(string key, Transform value, string prompt)
    {
      return new TransformField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new TransformField to the dictionary. This overload will cause
    /// the field to be tagged as "textured" so that the texturing UI will
    /// appear in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public TransformField AddTextured(string key, Transform value, string prompt)
    {
      return new TransformField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// Add a new DateTimeField to the dictionary. This will be a data only
    /// field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public DateTimeField Add(string key, DateTime value)
    {
      return Add(key, value, string.Empty);
    }

    /// <summary>
    /// Add a new DateTimeField to the dictionary.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public DateTimeField Add(string key, DateTime value, string prompt)
    {
      return new DateTimeField(m_content, key, prompt, value, false);
    }

    /// <summary>
    /// Add a new DateTimeField to the dictionary. This overload will cause the
    /// field to be tagged as "textured" so that the texturing UI will appear
    /// in automatic UIs.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <param name="prompt">
    /// Prompt to display in the user interface (Content Browsers) if this
    /// is null or an empty string the this field is a data only field and will
    /// not appear in the user interface.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public DateTimeField AddTextured(string key, DateTime value, string prompt)
    {
      return new DateTimeField(m_content, key, prompt, value, true);
    }

    /// <summary>
    /// AddField a new ByteArrayField to the dictionary. This will be a data
    /// only field and not show up in the content browsers.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">Initial value for this field.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary
    /// </exception>
    public ByteArrayField Add(string key, byte[] value)
    {
      return new ByteArrayField(m_content, key, string.Empty, value, false);
    }

    #endregion Overloaded AddField methods for the supported data types

    #region Overloaded Set methods for supported data types
    /// <summary>
    /// Set the field value with a change notification of 
    /// RenderContent.ChangeContexts.Program.  Will throw an exception if the
    /// key name is invalid or if T is not a supported data type.
    /// </summary>
    /// <typeparam name="T">Data type of the value parameter.</typeparam>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    private void SetHelper<T>(string key, T value)
    {
      // Look for the Field in this dictionary first, if it is not found then
      // ask the RenderContent C++ object for the field.
      Field field = FieldFromName(key);
      // Field not in this dictionary or in the C++ field list
      if (null == field) throw new InvalidOperationException("Fields dictionary does not contain this key " + key + ".");
      // Set the field value
      field.Set(value);
    }

    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, string value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete ("Use version without ChangeContexts")]
    public void Set(string key, string value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, bool value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, bool value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, int value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, int value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, float value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, float value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, double value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, double value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Color4f value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Color4f value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, System.Drawing.Color value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, System.Drawing.Color value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Vector2d value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Vector2d value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Vector3d value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Vector3d value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Point2d value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Point2d value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Point3d value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Point3d value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Point4d value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Point4d value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Guid value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Guid value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, Transform value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, Transform value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, DateTime value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, DateTime value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    public void Set(string key, byte[] value) { SetHelper(key, value); }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw a InvalidOperationException exception if the key
    /// name is not valid.
    /// </summary>
    /// <param name="key">Key name for the field value to change.</param>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete("Use version without ChangeContexts")]
    public void Set(string key, byte[] value, RenderContent.ChangeContexts changeContext) { SetHelper(key, value); }
    #endregion Overloaded Set methods for supported data types

    #region Tag methods
    /// <summary>
    /// Sets an object that contains data to associate with the field.  THIS IS NOW OBSOLETE - if you were using this, please email andy@mcneel.com and let me know why.
    /// </summary>
    /// <param name="key">Key name for the field to tag.</param>
    /// <param name="tag">Data to associate with the field.</param>
    /// <returns>True if the field is found and the tag was set otherwise false is returned.</returns>
    [Obsolete]
    public bool SetTag(string key, object tag)
    {
      return false;
    }
    /// <summary>
    /// Gets object that contains data associate with a field. THIS IS NOW OBSOLETE - if you were using this, please email andy@mcneel.com and let me know why.
    /// </summary>
    /// <param name="key">Key name of the field to get.</param>
    /// <param name="tag">Data associated with the field.</param>
    /// <returns>
    /// Returns true if the field is found and its tag was retrieved otherwise;
    /// returns false.
    /// </returns>
    [Obsolete]
    public bool TryGetTag(string key, out object tag)
    {
      tag = null;
      return false;
    }
    #endregion Tag methods

    #region Overloaded TryGetValue methods
    /// <summary>
    /// Parametrized version of TryGetValue.
    /// </summary>
    /// <typeparam name="T">Type of field to find.</typeparam>
    /// <param name="key">Name of field to find.</param>
    /// <param name="value">out parameter to be set.</param>
    /// <since>6.12</since>
    /// <returns>true if field was found. If false out parameter value will be set to default(T).</returns>
    public bool TryGetValue<T>(string key, out T value) {
      Field f = FieldFromName(key);
      value = f == null ? default(T) : f.GetValue<T>();
      return f!=null;
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out string value)
    {
      StringField field = FieldFromName(key) as StringField;
      value = null == field ? string.Empty : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out bool value)
    {
      BoolField field = FieldFromName(key) as BoolField;
      value = null != field && field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out int value)
    {
      IntField field = FieldFromName(key) as IntField;
      value = null == field ? 0 : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out double value)
    {
      DoubleField field = FieldFromName(key) as DoubleField;
      value = null == field ? 0 : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out float value)
    {
      FloatField field = FieldFromName(key) as FloatField;
      value = null == field ? 0 : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Color4f value)
    {
      Color4fField field = FieldFromName(key) as Color4fField;
      value = null == field ? Color4f.Empty : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out System.Drawing.Color value)
    {
      Color4fField field = FieldFromName(key) as Color4fField;
      value = null == field ? System.Drawing.Color.Empty : field.SystemColorValue;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Vector2d value)
    {
      Vector2dField field = FieldFromName(key) as Vector2dField;
      value = null == field ? Vector2d.Unset : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Vector3d value)
    {
      Vector3dField field = FieldFromName(key) as Vector3dField;
      value = null == field ? Vector3d.Unset : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Point2d value)
    {
      Point2dField field = FieldFromName(key) as Point2dField;
      value = null == field ? Point2d.Unset : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Point3d value)
    {
      Point3dField field = FieldFromName(key) as Point3dField;
      value = null == field ? Point3d.Unset : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Point4d value)
    {
      Point4dField field = FieldFromName(key) as Point4dField;
      value = null == field ? Point4d.Unset : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Guid value)
    {
      GuidField field = FieldFromName(key) as GuidField;
      value = null == field ? Guid.Empty : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out Transform value)
    {
      TransformField field = FieldFromName(key) as TransformField;
      value = null == field ? Transform.Unset : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out DateTime value)
    {
      DateTimeField field = FieldFromName(key) as DateTimeField;
      value = null == field ? DateTime.Now : field.Value;
      return (null != field);
    }
    /// <summary>
    /// Find a field with the specified key and get its value if found.
    /// </summary>
    /// <param name="key">Key name of the field to get a value for.</param>
    /// <param name="value">Output parameter which will receive the field value.</param>
    /// <returns>
    /// Returns true if the key is found and the value parameter is set to the
    /// field value.  Returns false if the field was not found.
    /// </returns>
    public bool TryGetValue(string key, out byte[] value)
    {
      ByteArrayField field = FieldFromName(key) as ByteArrayField;
      value = null == field ? null : field.Value;
      return (null != field);
    }
    #endregion Overloaded TryGetValue methods

    public IEnumerator<Field> GetEnumerator()
    {
        // Content pointer
        IntPtr content_pointer = m_content.ConstPointer();
        // Content C++ pointer has not been created yet so there is no place to
        // look for the field.
        if (IntPtr.Zero != content_pointer)
        {
            int fields = UnsafeNativeMethods.Rdk_RenderContent_FieldCount(content_pointer);

            for (int i = 0; i < fields; i++)
            {
                IntPtr fieldPointer = UnsafeNativeMethods.Rdk_RenderContent_Field(content_pointer, i);
                if (IntPtr.Zero != fieldPointer)
                {
                    yield return Field.FieldFromPointer(m_content, fieldPointer);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
  /////////////////////////////////////////////////////////////////////////////
  /// <summary>
  /// Generic data fields used to add publicly accessible properties to
  /// RenderContent.FieldDictionary.  These should be created by calling a
  /// FieldDictaionary.Add() method on a Render content object.  These are
  /// allocated after the RenderContent object's C++ object is created and
  /// added to the underlying C++ objects content dictionary, who ever
  /// allocates a field is responsible for deleting it so these objects clean
  /// up the C++ pointers when they are disposed of.
  /// </summary>
  public abstract class Field
  {
    internal enum ChangeContexts : int
    {
      UI = 0,        // Change occurred as a result of user activity in the content's UI.
      Drop = 1,      // Change occurred as a result of drag and drop.
      Program = 2,   // Change occurred as a result of internal program activity.
      Ignore = 3,    // Change can be disregarded.
      Tree = 4,      // Change occurred within the content tree (e.g., nodes reordered).
      Undo = 5,      // Change occurred as a result of an undo.
      FieldInit = 6, // Change occurred as a result of a field initialization.
      Serialize = 7, // Change occurred during serialization (loading).
    }

    #region Members
    /// <summary>
    /// Place holder for the initial field value, this will get used by
    /// CreateCppPointer(), it will call Set(m_initialValue) after creating the
    /// C++ pointer to initialize the field value.
    /// </summary>
    #endregion Members

    #region Properties

    private readonly string m_name;
    /// <summary>
    /// Field name value string passed to the constructor.
    /// </summary>
    public string Name => m_name;

    internal IntPtr FieldPointer
    {
      get
      {
        var p = m_render_content.Fields.FieldPointerFromName(Name);
        Debug.Assert(p != IntPtr.Zero);
        return p;
      }
    }

    /// <summary>
    /// Field name value string passed to the constructor
    /// </summary>
    [Obsolete("The Rhino.Render.Fields.Field.Key property is obsolete, use Name instead.")]
    public string Key => Name;

    /// <summary>
    /// Gets or sets an object that contains data to associate with the field.
    /// </summary>
    /// <returns>
    /// An object that contains information that is associated with the field.
    /// </returns>
    public object Tag { get; set; }

    #endregion Properties

    #region Set methods
    /// <summary>
    /// Set the field value with a change notification of 
    /// RenderContent.ChangeContexts.Program.  Will throw an exception if the
    /// key name is invalid or if T is not a supported data type.
    /// </summary>
    /// <typeparam name="T">Data type of the value parameter.</typeparam>
    /// <param name="value">New value for this field.</param>
    internal void Set<T>(T value)
    {
      // Convert the value to a variant, will throw an exception if the value
      // type is not supported
      using (var varient = new Variant(value))
      {
        // Get the variant C++ pointer
        var variant_pointer = varient.NonConstPointer();
        // Tell the C++ RDK to change the value
        var rc = UnsafeNativeMethods.Rdk_ContentField_SetVariantParameter(FieldPointer, variant_pointer);
        // If the C++ RDK failed to set the value throw an exception.
        //  Note: A return value of 1 means the value was changed, 2 means
        //        the current value is equal to "value" so nothing changed
        //        and a value of 0 means there was an error setting the field
        //        value.
        if (rc < 1) 
        {
          throw new InvalidOperationException("SetNamedParamter doesn't support this type.");
        }
      }
    }
    /// <summary>
    /// Set the field value and send the appropriate change notification to the
    /// render SDK.  Will throw an exception if the key name is invalid or if
    /// T is not a supported data type.
    /// </summary>
    /// <typeparam name="T">Data type of the value parameter.</typeparam>
    /// <param name="value">New value for this field.</param>
    /// <param name="changeContext">The reason why the value is changing.</param>
    [Obsolete ("Use Set method without ChangeContexts and Begin/End Change")]
    internal void Set<T>(T value, RenderContent.ChangeContexts changeContext)
    {
      // If not getting called as the result of an Add call and there is a
      // RenderContent associated with this field (there should always be one
      // but you can never be to careful) then call RenderContent.BeginChange()
      var call_end_change = m_render_content != null;
      if (call_end_change) m_render_content.BeginChange(changeContext);

      Set(value);

      if (call_end_change) 
      {
        m_render_content.EndChange();
      }
    }
    #endregion Set methods

    #region Methods to access value as specific data types
    public abstract object ValueAsObject();

    /// <summary>
    /// Parametrized version of GetValue calling appropriate ValueAs* methods.
    /// </summary>
    /// <since>6.12</since>
    /// <typeparam name="T">Type of value to get.</typeparam>
    /// <returns>Value of type T of the field</returns>
    public T GetValue<T>() {
      Type t = typeof(T);
      object result;
      if(t == typeof(int)) {
        result = ValueAsInt();
      }
      else if(t == typeof(bool)) {
        result = ValueAsBool();
      }
      else if(t == typeof(Color4f)) {
        result = ValueAsColor4f();
      }
      else if(t == typeof(float)) {
        result = ValueAsFloat();
      }
      else if(t == typeof(double)) {
        result = ValueAsDouble();
      }
      else if(t == typeof(string)) {
        result = ValueAsString();
      }
      else if(t == typeof(Vector2d)) {
        result = ValueAsVector2d();
      }
      else if(t == typeof(Vector3d)) {
        result = ValueAsVector3d();
      }
      else if(t == typeof(Point2d)) {
        result = ValueAsPoint2d();
      }
      else if(t == typeof(Point3d)) {
        result = ValueAsPoint3d();
      }
      else if(t == typeof(Point4d)) {
        result = ValueAsPoint4d();
      }
      else if(t == typeof(DateTime)) {
        result = ValueAsDateTime();
      }
      else if(t == typeof(Guid)) {
        result = ValueAsGuid();
      }
      else if(t == typeof(Transform)) {
        result = ValueAsTransform();
      }
      else if(t == typeof(byte[])) {
        result = ValueAsByteArray();
      }
      else {
        result = default(T);
      }

      return (T)result;
    }
    /// <summary>
    /// Get field value as a string.
    /// </summary>
    /// <returns>Returns the field value as a string if possible.</returns>
    protected string ValueAsString()
    {
      using (var string_holder = new StringHolder())
      {
        IntPtr string_pointer = string_holder.NonConstPointer();
        UnsafeNativeMethods.Rdk_ContentField_StringValue(FieldPointer, string_pointer);
        return string_holder.ToString();
      }
    }
    /// <summary>
    /// Return field value as a bool.
    /// </summary>
    /// <returns>Returns field value as a bool. </returns>
    protected bool ValueAsBool()
    {
      int result = UnsafeNativeMethods.Rdk_ContentField_BoolValue(FieldPointer);
      return (result == 1);
    }
    /// <summary>
    /// Return field value as integer.
    /// </summary>
    /// <returns>Return the field value as an integer.</returns>
    protected int ValueAsInt()
    {
      int result = UnsafeNativeMethods.Rdk_ContentField_IntValue(FieldPointer);
      return result;
    }
    /// <summary>
    /// Return field value as a double precision number.
    /// </summary>
    /// <returns>Return the field value as a double precision number.</returns>
    protected double ValueAsDouble()
    {
      // Call the C++ RDK and get the Variant value as a double
      double result = UnsafeNativeMethods.Rdk_ContentField_DoubleValue(FieldPointer);
      return result;
    }
    /// <summary>
    /// Return field value as floating point number.
    /// </summary>
    /// <returns>Return the field value as an floating point number.</returns>
    protected float ValueAsFloat()
    {
      float result = UnsafeNativeMethods.Rdk_ContentField_FloatValue(FieldPointer);
      return result;
    }
    /// <summary>
    /// Return field as a Rhino.Display.Color4f color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Display.Color4f color value.</returns>
    protected Color4f ValueAsColor4f()
    {
      Color4f result = Color4f.Empty;
      UnsafeNativeMethods.Rdk_ContentField_ColorValue(FieldPointer, ref result);
      return result;
    }
    /// <summary>
    /// Return field as a Rhino.Geometry.Vector2d color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Geometry.Vector2d color value.</returns>
    protected Vector2d ValueAsVector2d()
    {
      Vector2d result = Vector2d.Unset;
      UnsafeNativeMethods.Rdk_ContentField_Vector2dValue(FieldPointer, ref result);
      return result;
    }
    /// <summary>
    /// Return field as a Rhino.Geometry.Vector3d color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Geometry.Vector3d color value.</returns>
    protected Vector3d ValueAsVector3d()
    {
      Vector3d result = Vector3d.Unset;
      UnsafeNativeMethods.Rdk_ContentField_Vector3dValue(FieldPointer, ref result);
      return result;
    }
    /// <summary>
    /// Return field as a Rhino.Geometry.Point2d color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Geometry.Point2d color value.</returns>
    protected Point2d ValueAsPoint2d() { return new Point2d(ValueAsVector2d()); }
    /// <summary>
    /// Return field as a Rhino.Geometry.Point3d color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Geometry.Point3d color value.</returns>
    protected Point3d ValueAsPoint3d() { return new Point3d(ValueAsVector3d()); }
    /// <summary>
    /// Return field as a Rhino.Geometry.Point4d color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Geometry.Point4d color value.</returns>
    protected Point4d ValueAsPoint4d()
    {
      Point4d result = Point4d.Unset;
      // Call the C++ RDK and get the Variant value as a Point4d
      UnsafeNativeMethods.Rdk_ContentField_Point4dValue(FieldPointer, ref result);
      return result;
    }
    /// <summary>
    /// Return field value as Guid.
    /// </summary>
    /// <returns>Return the field value as an Guid.</returns>
    protected Guid ValueAsGuid()
    {
      Guid result = UnsafeNativeMethods.Rdk_ContentField_UUIDValue(FieldPointer);
      return result;
    }
    /// <summary>
    /// Return field as a Rhino.Geometry.Transform color value.
    /// </summary>
    /// <returns>Return field as a Rhino.Geometry.Transform color value.</returns>
    protected Transform ValueAsTransform()
    {
      Transform result = Transform.Unset;
      UnsafeNativeMethods.Rdk_ContentField_XformValue(FieldPointer, ref result);
      return result;
    }
    /// <summary>
    /// Return field as a DateTime value.
    /// </summary>
    /// <returns>Return field as a DateTime value.</returns>
    protected DateTime ValueAsDateTime()
    {
      int month = 0, year = 0, day = 0, hours = 0, minutes = 0, seconds = 0;
      UnsafeNativeMethods.Rdk_ContentField_TimeValue(FieldPointer, ref year, ref month, ref day, ref hours, ref minutes, ref seconds);
      DateTime result = new DateTime(year, month, day, hours, minutes, seconds);
      return result;
    }
    /// <summary>
    /// Return field as a byte array.
    /// </summary>
    /// <returns>Return field as a byte array.</returns>
    protected byte[] ValueAsByteArray()
    {
      int size_of_result = UnsafeNativeMethods.Rdk_ContentField_GetByteArrayValueSize(FieldPointer);
      // Allocate a buffer to receive a copy of the Variant data
      byte[] result = new byte[size_of_result];
      // Copy the C++ buffer into the result byte array
      UnsafeNativeMethods.Rdk_ContentField_GetByteArrayValue(FieldPointer, result, size_of_result);
      return result;
    }
    #endregion Methods to access value as specific data types

    protected Field(RenderContent renderContent, string name)
    {
      Debug.Assert(!string.IsNullOrEmpty(name));
      m_render_content = renderContent;
      m_name = name;
    }

    protected Field(RenderContent renderContent, IntPtr pointer)
    {
      m_render_content = renderContent;

      using (var string_holder = new StringHolder())
      {
        IntPtr string_pointer = string_holder.NonConstPointer();
        UnsafeNativeMethods.Rdk_ContentField_InternalName(pointer, string_pointer);
        m_name = string_holder.ToString();
      }

      Debug.Assert(!string.IsNullOrEmpty(m_name));
    }

    protected Field(RenderContent renderContent, string name, string prompt, object initialValue, bool isTextured)
    {
      Debug.Assert(!string.IsNullOrEmpty(name));

      m_render_content = renderContent;
      m_name = name;
      CreateCppPointer(renderContent, name, prompt, initialValue, isTextured);
    }

    private readonly RenderContent m_render_content;
    
    protected void CreateCppPointer(RenderContent content, string key, string prompt, object initialValue, bool isTextured)
    {
      // Get the RenderContent C++ pointer, fields get added to content field lists
      // so you have to have a valid Content C++ pointer when creating a field.
      IntPtr content_pointer = content.ConstPointer();
      // If there is a user interface prompt string then set the add to user interface flag.
      bool is_visible_to_auto_ui = !string.IsNullOrEmpty(prompt);
     
      Variant vValue = new Variant(initialValue);

      // Allocate the objects C++ pointer.
      IntPtr p = IntPtr.Zero;
      if (isTextured)
      {
        Variant nullVal = new Variant();
        Variant hundredVal = new Variant(100);
        p = UnsafeNativeMethods.Rdk_TexturedContentField_New(content_pointer, key, prompt, vValue.ConstPointer(), is_visible_to_auto_ui ? 0 : 0x8001, hundredVal.ConstPointer(), nullVal.ConstPointer(), nullVal.ConstPointer());
        if(nullVal is IDisposable nulldisp) {
          nulldisp.Dispose();
        }
        if(hundredVal is IDisposable hundreddisp) {
          hundreddisp.Dispose();
        }
      }
      else
      {
        p = UnsafeNativeMethods.Rdk_ContentField_New(content_pointer, key, prompt, vValue.ConstPointer(), is_visible_to_auto_ui ? 0 : 0x8001);

        if (isTextured)
        {
          UnsafeNativeMethods.Rdk_ContentField_SetIsTextured(p, 1);
        }
      }

      //Do not call Begin/EndChange here, because this is initial setup.
      Set(initialValue);

      // If m_initialValue can be disposed of then dispose of it now
      if (initialValue is IDisposable) 
      {
        (initialValue as IDisposable).Dispose();
      }
    }
    /// <summary>
    /// IDisposable required method
    /// </summary>
    internal void InternalDispose()
    {
    }

    /// <summary>
    /// Create a Field[data type] object from a content field pointer using its values
    /// variant type to figure out what kind of Field[data type] object to return.
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields dictionary the field belongs to. </param>
    /// <param name="fieldPointer">
    /// C++ pointer to the field object, will throw a ArgumentNullException if this
    /// value is null.
    /// </param>
    /// <returns></returns>
    static internal Field FieldFromPointer(RenderContent renderContent, IntPtr fieldPointer)
    {
      if (IntPtr.Zero == fieldPointer) 
      {
        throw new ArgumentNullException("fieldPointer");
      }

      IntPtr variant_pointer = UnsafeNativeMethods.Rdk_ContentField_Value(fieldPointer);

      switch (UnsafeNativeMethods.Rdk_Variant_Type(variant_pointer))
      {
        case (int)Variant.VariantTypes.Bool:
          return new BoolField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Color:
          return new Color4fField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Double:
          return new DoubleField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Float:
          return new FloatField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Integer:
          return new IntField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Matrix:
          return new TransformField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Point4d:
          return new Point4dField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.String:
          return new StringField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Time:
          return new DateTimeField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Uuid:
          return new GuidField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Vector2d:
          return new Vector2dField(renderContent, fieldPointer);
        case (int)Variant.VariantTypes.Vector3d:
          return new Vector3dField(renderContent, fieldPointer);
      }

      return new ByteArrayField(renderContent, fieldPointer);
    }
  }
  /// <summary>
  /// String field value class
  /// </summary>
  public class StringField : Field
  {
    internal StringField(RenderContent renderContent, IntPtr attachToPointer) 
      : base(renderContent, attachToPointer) 
    {
    }

    internal StringField(RenderContent rc, string name, string prompt, string value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public string Value
    {
      get { return ValueAsString(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// bool field value class
  /// </summary>
  public class BoolField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal BoolField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal BoolField(RenderContent rc, string name, string prompt, bool value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public bool Value
    {
      get { return ValueAsBool(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Integer field value class
  /// </summary>
  public class IntField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal IntField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal IntField(RenderContent rc, string name, string prompt, int value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public int Value
    {
      get { return ValueAsInt(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// float field value class
  /// </summary>
  public class FloatField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal FloatField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal FloatField(RenderContent rc, string name, string prompt, float value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public float Value
    {
      get { return ValueAsFloat(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// double field value class
  /// </summary>
  public class DoubleField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal DoubleField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal DoubleField(RenderContent rc, string name, string prompt, double value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public double Value
    {
      get { return ValueAsDouble(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Color4f field value class
  /// </summary>
  public class Color4fField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal Color4fField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal Color4fField(RenderContent rc, string name, string prompt, Color4f value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Color4f Value
    {
      get { return ValueAsColor4f(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public System.Drawing.Color SystemColorValue
    {
      get { return ValueAsColor4f().AsSystemColor(); }
      set { Value = new Color4f(value); }
    }
  }
  /// <summary>
  /// Vector2d field value class
  /// </summary>
  public class Vector2dField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal Vector2dField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal Vector2dField(RenderContent rc, string name, string prompt, Vector2d value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Vector2d Value
    {
      get { return ValueAsVector2d(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Vector3d field value class
  /// </summary>
  public class Vector3dField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal Vector3dField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal Vector3dField(RenderContent rc, string name, string prompt, Vector3d value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Vector3d Value
    {
      get { return ValueAsVector3d(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Point2d field value class
  /// </summary>
  public class Point2dField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal Point2dField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal Point2dField(RenderContent rc, string name, string prompt, Point2d value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Point2d Value
    {
      get { return ValueAsPoint2d(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Point3d field value class
  /// </summary>
  public class Point3dField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal Point3dField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal Point3dField(RenderContent rc, string name, string prompt, Point3d value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Point3d Value
    {
      get { return ValueAsPoint3d(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Point4d field value class
  /// </summary>
  public class Point4dField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal Point4dField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal Point4dField(RenderContent rc, string name, string prompt, Point4d value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Point4d Value
    {
      get { return ValueAsPoint4d(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Guid field value class
  /// </summary>
  public class GuidField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal GuidField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal GuidField(RenderContent rc, string name, string prompt, Guid value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Guid Value
    {
      get { return ValueAsGuid(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// Transform field value class
  /// </summary>
  public class TransformField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal TransformField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal TransformField(RenderContent rc, string name, string prompt, Transform value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public Transform Value
    {
      get { return ValueAsTransform(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// DateTime field value class
  /// </summary>
  public class DateTimeField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal DateTimeField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal DateTimeField(RenderContent rc, string name, string prompt, DateTime value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public DateTime Value
    {
      get { return ValueAsDateTime(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
  /// <summary>
  /// ByteArray field value class
  /// </summary>
  public class ByteArrayField : Field
  {
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="renderContent">RenderContent whose Fields owns this filed.</param>
    /// <param name="attachToPointer">Existing C++ pointer to attach to.</param>
    internal ByteArrayField(RenderContent renderContent, IntPtr attachToPointer) : base(renderContent, attachToPointer) { }

    internal ByteArrayField(RenderContent rc, string name, string prompt, byte[] value, bool textured)
      : base(rc, name, prompt, value, textured)
    {
    }

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public byte[] Value
    {
      get { return ValueAsByteArray(); }
      set { Set(value); }
    }
    public override object ValueAsObject() { return Value; }
  }
}

#endif
