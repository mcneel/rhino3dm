using System;
using Rhino.Runtime;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents an arbitrarily sized matrix of <see cref="double">double</see>-precision
  /// floating point numbers. If you are working with a 4x4 matrix, then you may want
  /// to use the <see cref="Transform"/> class instead.
  /// </summary>
  public class Matrix : IDisposable
  {
    IntPtr m_ptr; //ON_Matrix*
    int m_rows;
    int m_columns;

    /// <summary>
    /// Initializes a new instance of the matrix.
    /// </summary>
    /// <param name="rowCount">A positive integer, or 0, for the number of rows.</param>
    /// <param name="columnCount">A positive integer, or 0, for the number of columns.</param>
    /// <exception cref="ArgumentOutOfRangeException">If either rowCount, or columnCount
    /// or both are negative.</exception>
    /// <since>5.0</since>
    public Matrix(int rowCount, int columnCount)
    {
      if (rowCount < 0 )
        throw new ArgumentOutOfRangeException("rowCount", "must be >= 0");
      if (columnCount < 0)
        throw new ArgumentOutOfRangeException("columnCount", "must be >= 0");
      m_rows = rowCount;
      m_columns = columnCount;
      m_ptr = UnsafeNativeMethods.ON_Matrix_New(rowCount, columnCount);
    }

    /// <summary>
    /// Initializes a new instance of the matrix based on a 4x4 matrix <see cref="Transform"/>.
    /// </summary>
    /// <param name="xform">A 4x4 matrix to copy from.</param>
    /// <since>5.0</since>
    public Matrix(Transform xform)
    {
      m_rows = 4;
      m_columns = 4;
      m_ptr = UnsafeNativeMethods.ON_Matrix_New2(ref xform);
    }

    /// <summary>
    /// Create a duplicate of this matrix.
    /// </summary>
    /// <returns>An exact duplicate of this matrix.</returns>
    /// <since>5.1</since>
    [ConstOperation]
    public Matrix Duplicate()
    {
      int rc = RowCount;
      int cc = ColumnCount;
      Matrix dup = new Matrix(rc, cc);

      for (int r = 0; r < rc; r++)
      {
        for (int c = 0; c < cc; c++)
        {
          dup[r, c] = this[r, c];
        }
      }

      return dup;
    }

    #region IDisposable implementation
    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~Matrix()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    /// <since>5.0</since>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (m_ptr!=IntPtr.Zero)
      {
        UnsafeNativeMethods.ON_Matrix_Delete(m_ptr);
      }
      m_ptr = IntPtr.Zero;
    }
    #endregion

    /// <summary>
    /// Gets or sets the matrix value at the given row and column indices.
    /// </summary>
    /// <param name="row">Index of row to access.</param>
    /// <param name="column">Index of column to access.</param>
    /// <returns>The value at [row, column].</returns>
    /// <value>The new value at [row, column].</value>
    public double this[int row, int column]
    {
      get
      {
        if (row < 0 || row>=m_rows)
          throw new IndexOutOfRangeException("row index out of range");
        if (column < 0 || column>=m_columns)
          throw new IndexOutOfRangeException("column index out of range");

        return UnsafeNativeMethods.ON_Matrix_GetValue(m_ptr, row, column);
      }
      set
      {
        if (row < 0 || row >= m_rows)
          throw new IndexOutOfRangeException("row index out of range");
        if (column < 0 || column >= m_columns)
          throw new IndexOutOfRangeException("column index out of range");
        UnsafeNativeMethods.ON_Matrix_SetValue(m_ptr, row, column, value);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this matrix is valid.
    /// </summary>
    /// <since>5.0</since>
    public bool IsValid
    {
      get { return (m_ptr != IntPtr.Zero && m_columns > 0 && m_rows > 0); }
    }

    /// <summary>
    /// Gets a value indicating whether this matrix has the same number of rows
    /// and columns. 0x0 matrices are not considered square.
    /// </summary>
    /// <since>5.0</since>
    public bool IsSquare
    {
      get { return (m_rows > 0 && m_columns == m_rows); }
    }

    /// <summary>
    /// Gets the amount of rows.
    /// </summary>
    /// <since>5.0</since>
    public int RowCount { get { return m_rows; } }

    /// <summary>
    /// Gets the amount of columns.
    /// </summary>
    /// <since>5.0</since>
    public int ColumnCount { get { return m_columns; } }

    /// <summary>
    /// Sets all values inside the matrix to zero.
    /// </summary>
    /// <since>5.0</since>
    public void Zero()
    {
      UnsafeNativeMethods.ON_Matrix_Zero(m_ptr);
    }

    /// <summary>
    /// Sets diagonal value and zeros off all non-diagonal values.
    /// </summary>
    /// <param name="d">The new diagonal value.</param>
    /// <since>5.0</since>
    public void SetDiagonal(double d)
    {
      UnsafeNativeMethods.ON_Matrix_SetDiagonal(m_ptr, d);
    }

    /// <summary>
    /// Modifies this matrix to be its transpose.
    /// <para>This is like swapping rows with columns.</para>
    /// <para>http://en.wikipedia.org/wiki/Transpose</para>
    /// </summary>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool Transpose()
    {
      bool rc = UnsafeNativeMethods.ON_Matrix_Transpose(m_ptr);
      if (rc)
      {
        int tmp = m_rows;
        m_rows = m_columns;
        m_columns = tmp;
      }
      return rc;
    }

    /// <summary>
    /// Exchanges two rows.
    /// </summary>
    /// <param name="rowA">A first row.</param>
    /// <param name="rowB">Another row.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool SwapRows(int rowA, int rowB)
    {
      return UnsafeNativeMethods.ON_Matrix_Swap(m_ptr, true, rowA, rowB);
    }

    /// <summary>
    /// Exchanges two columns.
    /// </summary>
    /// <param name="columnA">A first column.</param>
    /// <param name="columnB">Another column.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool SwapColumns(int columnA, int columnB)
    {
      return UnsafeNativeMethods.ON_Matrix_Swap(m_ptr, false, columnA, columnB);
    }

    /// <summary>
    /// Modifies this matrix to become its own inverse.
    /// <para>Matrix might be non-invertible (singular) and the return value will be false.</para>
    /// </summary>
    /// <param name="zeroTolerance">The admitted tolerance for 0.</param>
    /// <returns>true if operation succeeded; otherwise false.</returns>
    /// <since>5.0</since>
    public bool Invert(double zeroTolerance)
    {
      bool rc = UnsafeNativeMethods.ON_Matrix_Invert(m_ptr, zeroTolerance);
      if (rc)
      {
        int tmp = m_rows;
        m_rows = m_columns;
        m_columns = tmp;
      }
      return rc;
    }

    /// <summary>
    /// Multiplies two matrices and returns a new product matrix.
    /// </summary>
    /// <param name="a">A first matrix to use in calculation.</param>
    /// <param name="b">Another matrix to use in calculation.</param>
    /// <returns>The product matrix.</returns>
    /// <exception cref="ArgumentException">
    /// When a.ColumnCount != b.RowCount.
    /// </exception>
    /// <since>5.0</since>
    public static Matrix operator *(Matrix a, Matrix b)
    {
      if (a.ColumnCount != b.RowCount)
        throw new ArgumentException("a.ColumnCount != b.RowCount");
      if (a.RowCount < 1 || a.ColumnCount < 1 || b.ColumnCount < 1)
        throw new ArgumentException("either a of b are Invalid");

      Matrix rc = new Matrix(a.RowCount, b.ColumnCount);
      UnsafeNativeMethods.ON_Matrix_Multiply(rc.m_ptr, a.m_ptr, b.m_ptr);
      return rc;
    }

    /// <summary>
    /// Adds two matrices and returns a new sum matrix.
    /// </summary>
    /// <param name="a">A first matrix to use in calculation.</param>
    /// <param name="b">Another matrix to use in calculation.</param>
    /// <returns>The sum matrix.</returns>
    /// <exception cref="ArgumentException">
    /// When the two matrices are not the same size.
    /// </exception>
    public static Matrix operator +(Matrix a, Matrix b)
    {
      if (a.ColumnCount != b.ColumnCount || a.RowCount != b.RowCount)
        throw new ArgumentException("ColumnCount and RowCount must be same for both matrices");
      if (a.RowCount < 1 || a.ColumnCount < 1)
        throw new ArgumentException("either a of b are Invalid");

      Matrix rc = new Matrix(a.RowCount, a.ColumnCount);
      UnsafeNativeMethods.ON_Matrix_Add(rc.m_ptr, a.m_ptr, b.m_ptr);
      return rc;
    }

    /// <summary>
    /// Modifies the current matrix by multiplying its values by a number.
    /// </summary>
    /// <param name="s">A scale factor.</param>
    /// <since>5.0</since>
    public void Scale(double s)
    {
      UnsafeNativeMethods.ON_Matrix_Scale(m_ptr, s);
    }

    /// <summary>Row reduces a matrix to calculate rank and determinant.</summary>
    /// <param name="zeroTolerance">
    /// (&gt;=0.0) zero tolerance for pivot test.  If a the absolute value of
    /// a pivot is &lt;= zeroTolerance, then the pivot is assumed to be zero.
    /// </param>
    /// <param name="determinant">value of determinant is returned here.</param>
    /// <param name="pivot">value of the smallest pivot is returned here.</param>
    /// <returns>Rank of the matrix.</returns>
    /// <remarks>
    /// The matrix itself is row reduced so that the result is an upper
    /// triangular matrix with 1's on the diagonal.
    /// </remarks>
    /// <since>5.0</since>
    public int RowReduce( double zeroTolerance, out double determinant, out double pivot)
    {
      determinant = 0;
      pivot = 0;
      return UnsafeNativeMethods.ON_Matrix_RowReduce(m_ptr, zeroTolerance, ref determinant, ref pivot);
    }

    /// <summary>
    /// Row reduces a matrix as the first step in solving M*X=b where
    /// b is a column of values.
    /// </summary>
    /// <param name="zeroTolerance">
    /// (&gt;=0.0) zero tolerance for pivot test. If the absolute value of a pivot
    /// is &lt;= zero_tolerance, then the pivot is assumed to be zero.
    /// </param>
    /// <param name="b">an array of RowCount values that is row reduced with the matrix.
    /// </param>
    /// <param name="pivot">the value of the smallest pivot is returned here.</param>
    /// <returns>Rank of the matrix.</returns>
    /// <remarks>
    /// The matrix itself is row reduced so that the result is an upper
    /// triangular matrix with 1's on the diagonal.
    /// </remarks>
    /// <since>5.0</since>
    public int RowReduce(double zeroTolerance, double[] b, out double pivot)
    {
      if (b.Length != RowCount)
        throw new ArgumentOutOfRangeException("b","b.Length!=RowCount");
      pivot = 0;
      return UnsafeNativeMethods.ON_Matrix_RowReduce2(m_ptr, zeroTolerance, b, ref pivot);
    }

    /// <summary>
    /// Row reduces a matrix as the first step in solving M*X=b where
    /// b is a column of 3d points.
    /// </summary>
    /// <param name="zeroTolerance">
    /// (&gt;=0.0) zero tolerance for pivot test. If the absolute value of a pivot
    /// is &lt;= zero_tolerance, then the pivot is assumed to be zero.
    /// </param>
    /// <param name="b">An array of RowCount 3d points that is row reduced with the matrix.
    /// </param>
    /// <param name="pivot">The value of the smallest pivot is returned here.</param>
    /// <returns>Rank of the matrix.</returns>
    /// <remarks>
    /// The matrix itself is row reduced so that the result is an upper
    /// triangular matrix with 1's on the diagonal.
    /// </remarks>
    /// <since>5.0</since>
    public int RowReduce(double zeroTolerance, Point3d[] b, out double pivot)
    {
      if (b.Length != RowCount)
        throw new ArgumentOutOfRangeException("b", "b.Length!=RowCount");
      pivot = 0;
      return UnsafeNativeMethods.ON_Matrix_RowReduce3(m_ptr, zeroTolerance, b, ref pivot);
    }

    /// <summary>
    /// Solves M*x=b where M is upper triangular with a unit diagonal and
    /// b is a column of values.
    /// </summary>
    /// <param name="zeroTolerance">(&gt;=0.0) used to test for "zero" values in b
    /// in under determined systems of equations.</param>
    /// <param name="b">The values in B[RowCount],...,B[B.Length-1] are tested to
    /// make sure they are within "zeroTolerance".</param>
    /// <returns>
    /// Array of length ColumnCount on success. null on error.
    /// </returns>
    /// <since>5.0</since>
    public double[] BackSolve(double zeroTolerance, double[] b)
    {
      double[] x = new double[ColumnCount];
      if (UnsafeNativeMethods.ON_Matrix_BackSolve(m_ptr, zeroTolerance, b.Length, b, x))
        return x;
      return null;
    }

    /// <summary>
    /// Solves M*x=b where M is upper triangular with a unit diagonal and
    /// b is a column of 3d points.
    /// </summary>
    /// <param name="zeroTolerance">(&gt;=0.0) used to test for "zero" values in b
    /// in under determined systems of equations.</param>
    /// <param name="b">The values in B[RowCount],...,B[B.Length-1] are tested to
    /// make sure they are "zero".</param>
    /// <returns>
    /// Array of length ColumnCount on success. null on error.
    /// </returns>
    /// <since>5.0</since>
    public Point3d[] BackSolvePoints(double zeroTolerance, Point3d[] b)
    {
      Point3d[] x = new Point3d[ColumnCount];
      if (UnsafeNativeMethods.ON_Matrix_BackSolve2(m_ptr, zeroTolerance, b.Length, b, x))
        return x;
      return null;
    }

    const int idxIsRowOrthogonal = 0;
    const int idxIsRowOrthoNormal = 1;
    const int idxIsColumnOrthogonal = 2;
    const int idxIsColumnOrthoNormal = 3;
    bool GetBool(int which)
    {
      return UnsafeNativeMethods.ON_Matrix_GetBool(m_ptr, which);
    }

    /// <summary>
    /// Gets a value indicating whether the matrix is row orthogonal.
    /// </summary>
    /// <since>5.0</since>
    public bool IsRowOrthogonal
    {
      get { return GetBool(idxIsRowOrthogonal); }
    }

    /// <summary>
    /// Gets a value indicating whether the matrix is column orthogonal.
    /// </summary>
    /// <since>5.0</since>
    public bool IsColumnOrthogonal
    {
      get { return GetBool(idxIsColumnOrthogonal); }
    }

    /// <summary>
    /// Gets a value indicating whether the matrix is row orthonormal.
    /// </summary>
    /// <since>5.0</since>
    public bool IsRowOrthoNormal
    {
      get { return GetBool(idxIsRowOrthoNormal); }
    }

    /// <summary>
    /// Gets a value indicating whether the matrix is column orthonormal.
    /// </summary>
    /// <since>5.0</since>
    public bool IsColumnOrthoNormal
    {
      get { return GetBool(idxIsColumnOrthoNormal); }
    }

    /// <summary>
    /// Gets the hash code for this matrix. The hash code will change 
    /// when the matrix changes so you cannot change matrices while they are stored in 
    /// hash tables.
    /// </summary>
    /// <returns>Hash code.</returns>
    [ConstOperation]
    public override int GetHashCode()
    {
      // David: ideally the hash code ought not be computed based on mutable fields, 
      //        but a matrix only has mutable fields. This implementation means a
      //        matrix cannot be stored in a hashtable while it is modified.
      int hash = 0;
      for (int i = 0; i < RowCount; i++)
        for (int j = 0; j < ColumnCount; j++)
          hash = -i ^ j ^ hash ^ this[i, j].GetHashCode();

      return hash;
    }
  }
}
