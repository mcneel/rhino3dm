#pragma warning disable 1591

using System;

namespace Rhino.UI.Controls.ThumbnailUI
{
  [Obsolete("IRhRdkThumbnailList is obsolete", false)]
  // C# version of IRhRdkThumbnailList
  public interface IRhRdkThumbnailList
  {
    /*
    virtual void GetGridMetrics(int& w, int& h, int& ox, int& oy) const override;
    virtual int GetStatisticsHeaderHeight(void) const override;
    virtual UUID Uuid(void) const override;
    virtual void Clear(void) override;
    virtual void Add(IRhRdkThumbnail& t);
    virtual IRhRdkThumbnail* Get(const UUID& u) const override;
    virtual IRhRdkThumbnailList::Modes Mode(void) const override;
    virtual void SetMode(IRhRdkThumbnailList::Modes m, bool b) override;
    virtual IRhRdkThumbnailList::Shapes Shape(void) const override;
    virtual IRhRdkContentThumbnailList::Sizes Size(void) const override;
    virtual void SetClientText(const wchar_t* w) override;
    virtual void SetCustomBitmapSize(int w, int h) override;
    virtual bool ShowLabels(void) const override;
    virtual void SetShowLabels(bool b) override;
    */

    void GetGridMetrics(ref int w, ref int h, ref int ox, ref int oy);

    void GetStatisticsHeaderHeight();

    Guid UUID();

    void Clear();

    void Add(Thumbnail t);

    IRhRdkThumbnail Get(ref Guid u);

    IRhRdkThumbnailList_Modes Mode();

    void SetMode(IRhRdkThumbnailList_Modes m, bool b);

    IRhRdkThumbnailList_Shapes Shape();

    IRhRdkContentThumbnailList_Sizes GetSize();

    void SetClientText(string w);

    void SetCustomBitmapSize(int w, int h);

    bool ShowLabels();

    void SetShowLabels(bool b);

  }

  [Obsolete("IRhRdkThumbnailList_Modes is obsolete", false)]
  public enum IRhRdkThumbnailList_Modes
  {
    /// <summary>Big thumbnails like Explorer icon mode (default).</summary>
    Grid,
    /// <summary>Small thumbnails and info on right like Explorer report mode.</summary>
    List,
    /// <summary>Tree view mode.</summary>
    Tree,
  };

  [Obsolete("IRhRdkThumbnailList_Shapes is obsolete", false)]
  public enum IRhRdkThumbnailList_Shapes
  {
    /// <summary>Square thumbnails.</summary>
    Square,
    /// <summary>Wide thumbnails.</summary>
    Wide,
  };
}
