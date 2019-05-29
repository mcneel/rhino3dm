using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhino.NodeInCode
{
  /// <summary>
  /// Provides access to all Grasshopper runtime components.
  /// </summary>
  public abstract class Components
  {
    private static volatile Components g_provider;

    readonly static object m_lock_object = new object();

    /// <summary>
    /// Gets access to the unique 
    /// </summary>
    internal static Components Instance
    {
      get
      {
        lock (m_lock_object)
        {
          return g_provider ?? (g_provider = Initialize());
        }
      }
    }

    private static Components Initialize()
    {
      Components components = Runtime.HostUtils.GetPlatformService<Components>
        ("NodeInCode.dll", "NodeInCode.NodeInCodeEntryPoint");

      if (components == null)
        throw new ApplicationException(
          "NodeInCode cannot be loaded from NodeInCode DLL.\n" +
          "Make sure that Rhino can load the NodeInCode.dll and try again.");

      return components;
    }

    /// <summary>
    /// Returns a collection with all component functions.
    /// For infrastructure usage only.
    /// </summary>
    // We use this method to start with two underscores so it will be hidden from Python's Intelliclunk.
    [CLSCompliant(false)]
    protected virtual NodeInCodeTable __getAllFunctions() { return null; }

    /// <summary>
    /// Returns a collection with all component functions.
    /// </summary>
    public static NodeInCodeTable NodeInCodeFunctions
    {
      get
      {
        return Instance.__getAllFunctions();
      }
    }

    /// <summary>
    /// Finds a component given its full name.
    /// </summary>
    /// <param name="fullName">The name, including its library name and a period if it is made by a third-party.</param>
    public static ComponentFunctionInfo FindComponent(string fullName)
    {
      if (string.IsNullOrWhiteSpace(fullName))
      {
        throw new ArgumentNullException("fullName");
      }

      return Instance.__getAllFunctions().GetValueAt(fullName);
    }
  }


  /// <summary>
  /// Defines the base class for a function representing a component.
  /// This class is abstract.
  /// </summary>
  public abstract class ComponentFunctionInfo
  {
    /// <summary>
    /// Instantiates a new instance of the function class. This is not meant for public consumption.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="namespace">Namespace.</param>
    /// <param name="description">Description.</param>
    /// <param name="inputNames">Input parameter names.</param>
    /// <param name="inputDescriptions">Input parameter descriptions.</param>
    /// <param name="inputTypeNames">Input parameter type names.</param>
    /// <param name="inputsOptional">Indications for optional inputs.</param>
    /// <param name="outputNames">Output names.</param>
    /// <param name="outputDescriptions">Output descriptions.</param>
    /// <param name="outputTypeNames">Output type names.</param>
    /// <param name="componentGuid">Component guid.</param>
    protected ComponentFunctionInfo(
      string name, string @namespace, string description,
      IReadOnlyList<string> inputNames, IReadOnlyList<string> inputDescriptions, IReadOnlyList<string> inputTypeNames, IReadOnlyList<bool> inputsOptional,
      IReadOnlyList<string> outputNames, IReadOnlyList<string> outputDescriptions, IReadOnlyList<string> outputTypeNames, Guid componentGuid
      )
    {
      if (string.IsNullOrEmpty(name)) throw new ArgumentException("name");
      Name = name;
      
      //namespace is null for default components
      Namespace = @namespace;

      Description = description ?? string.Empty;

      InputNames = inputNames ?? new List<string>().AsReadOnly();
      InputDescriptions = inputDescriptions ?? new List<string>().AsReadOnly();
      InputTypeNames = inputTypeNames ?? new List<string>().AsReadOnly();
      InputsOptional = inputsOptional ?? new List<bool>().AsReadOnly();

      OutputNames = outputNames ?? new List<string>().AsReadOnly();
      OutputDescriptions = outputDescriptions ?? new List<string>().AsReadOnly();
      OutputTypeNames = outputTypeNames ?? new List<string>().AsReadOnly();

      if (componentGuid == Guid.Empty) throw new ArgumentException("componentGuid");
      ComponentGuid = componentGuid;

      m_delegate_warnings = new Lazy<Delegate>(CreateDelegateWarnings);
      m_delegate_nowarnings = new Lazy<Delegate>(CreateDelegateNoWarnings);
      m_delegate_tree_warnings = new Lazy<Delegate>(CreateDelegateTreeWarnings);
      m_delegate_tree_nowarnings = new Lazy<Delegate>(CreateDelegateTreeNoWarnings);
    }

    Lazy<Delegate> m_delegate_warnings;
    Lazy<Delegate> m_delegate_nowarnings;
    Lazy<Delegate> m_delegate_tree_warnings;
    Lazy<Delegate> m_delegate_tree_nowarnings;

    private Delegate CreateDelegateWarnings()
    {
      return CreateDelegate_Impl(
        ExpressionUtil.GetMethod<IEnumerable, object>(x => EvaluateWithWarningsAsErrors(x))
        );
    }
    private Delegate CreateDelegateNoWarnings()
    {
      return CreateDelegate_Impl(
        ExpressionUtil.GetMethod<IEnumerable, object>(x => EvaluateSilenceWarnings(x))
        );
    }
    private Delegate CreateDelegateTreeWarnings()
    {
      return CreateDelegate_Impl(
        ExpressionUtil.GetMethod<IEnumerable, object>(x => EvaluateTreeWithWarningsAsErrors(x))
        );
    }
    private Delegate CreateDelegateTreeNoWarnings()
    {
      return CreateDelegate_Impl(
        ExpressionUtil.GetMethod<IEnumerable, object>(x => EvaluateTreeSilenceWarnings(x))
        );
    }

    /// <summary>
    /// The function name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The function namespace.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Determines if the component is a default one.
    /// </summary>
    public bool IsDefault
    { get { return Namespace == null; } }

    /// <summary>
    /// The function description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The function input parameter names.
    /// </summary>
    public IReadOnlyList<string> InputNames { get; }

    /// <summary>
    /// The function input parameter descriptions.
    /// </summary>
    public IReadOnlyList<string> InputDescriptions { get; }

    /// <summary>
    /// The function input type names.
    /// </summary>
    public IReadOnlyList<string> InputTypeNames { get; }

    /// <summary>
    /// Indications for each function input parameter whether it is optional.
    /// </summary>
    public IReadOnlyList<bool> InputsOptional { get; }

    /// <summary>
    /// Grasshopper returns several items in general. This property returns the output names.
    /// </summary>
    public IReadOnlyList<string> OutputNames { get; }

    /// <summary>
    /// Grasshopper returns several items in general. This property returns the output descriptions.
    /// </summary>
    public IReadOnlyList<string> OutputDescriptions { get; }

    /// <summary>
    /// Grasshopper returns several items in general. This property returns the output type names.
    /// </summary>
    public IReadOnlyList<string> OutputTypeNames { get; }

    /// <summary>
    /// The unique identifier of the Grasshopper component.
    /// It is the original developer's responsibility to ensure that this ID is unique.
    /// </summary>
    public Guid ComponentGuid { get; private set; }

    /// <summary>
    /// Returns a string representing this function.
    /// </summary>
    /// <returns>The namespace and the name.</returns>
    public override string ToString()
    {
      return FullName;
    }

    /// <summary>
    /// Returns the name of the component prefixed by, if existing, the namespace.
    /// </summary>
    public string FullName
    {
      get
      {
        return string.IsNullOrEmpty(Namespace) ? Name : (Namespace + "." + Name);
      }
    }

    /// <summary>
    /// Shows the full name of the component, including optional periods.
    /// Removes spaces and common operator signs.
    /// </summary>
    public string FullScriptingName
    {
      get
      {
        var rc = string.IsNullOrEmpty(Namespace) ? CleanName(Name) : (CleanName(Namespace) + "_" + CleanName(Name));

        return rc;
      }
    }

    static string CleanName(string toBeCleaned)
    {
      string result = Regex.Replace(toBeCleaned, @"[\.\+\-\ \@\~\^\\\/\'\`\(\)]+", m => string.Empty);
      return result;
    }

    /// <summary>
    /// Evaluates the component with a set of arguments.
    /// There needs to be an argument for each input param, and each output param gives an entry in the output array.
    /// </summary>
    /// <param name="args">The arguments list. Each item is assigned to each input param, in order.</param>
    /// <param name="keepTree">A value indicating whether trees should be considered valid inputs, and should be returned.
    /// In this case, output variables are not simplified to common types.</param>
    /// <param name="warnings">A possible list of warnings, or null.</param>
    /// <returns>An array of objects, each representing an output result.</returns>
    /// <remarks>For component that store no state, this method is thread safe.</remarks>
    public abstract object[] Evaluate(IEnumerable args, bool keepTree, out string[] warnings);

    /// <summary>
    /// Performs list logical reduction when trees are not used.
    /// </summary>
    /// <param name="input">The computed data.</param>
    /// <param name="warnings">A series of warnings.</param>
    /// <param name="treatWarningsAsErrors">If warnings should be considered, then this is set to true.</param>
    /// <returns>The simplified object.</returns>
    public static object SimplifyTreeOutput(object[] input, string[] warnings, bool treatWarningsAsErrors)
    {
      if (input == null) return null;

      object rc;
      if (treatWarningsAsErrors && warnings != null && warnings.Length > 0) throw new ApplicationException(warnings[0]);

      if (input.Length == 1)
        rc = input[0];
      else
        rc = input;
      return rc;
    }

    /// <summary>
    /// Infrastructure method that simplifies output and performs warning checks.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <returns>Items.</returns>
    protected object EvaluateWithWarningsAsErrors(IEnumerable args)
    {
      string[] warnings;

      object[] result = Evaluate(args, false, out warnings);
      return SimplifyTreeOutput(result, warnings, true);
    }

    /// <summary>
    /// Infrastructure method that simplifies output and performs warning checks.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <returns>Items.</returns>
    protected object EvaluateSilenceWarnings(IEnumerable args)
    {
      string[] warnings;

      object[] result = Evaluate(args, false, out warnings);
      return SimplifyTreeOutput(result, warnings, false);
    }

    /// <summary>
    /// Infrastructure method that simplifies output and performs warning checks.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <returns>Items.</returns>
    protected object EvaluateTreeWithWarningsAsErrors(IEnumerable args)
    {
      string[] warnings;
      object[] rc = Evaluate(args, true, out warnings);
      if (warnings != null && warnings.Length > 0) throw new ApplicationException(warnings[0]);

      return rc;
    }

    /// <summary>
    /// Infrastructure method that simplifies output and performs warning checks.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <returns>Items.</returns>
    protected object[] EvaluateTreeSilenceWarnings(IEnumerable args)
    {
      string[] warnings;
      return Evaluate(args, true, out warnings);
    }

    private Delegate CreateDelegate_Impl(MethodInfo method)
    {
      var pes = new ParameterExpression[InputNames.Count];
      for (int i = 0; i < InputNames.Count; i++)
      {
        pes[i] = Expression.Parameter(typeof(object), InputNames[i]);
      }

      var instance = Expression.Constant(this);

      var array = Expression.NewArrayInit(typeof(object), pes);

      var callExpression = Expression.Call(instance, method, array);

      LambdaExpression lambdaExpression = Expression.Lambda(callExpression, pes);

      return lambdaExpression.Compile();
    }

    /// <summary>
    /// Returns a delegate that can be directly invoked using a list of arguments.
    /// This flattens trees.
    /// </summary>
    public Delegate Delegate
    {
      get
      {
        return m_delegate_warnings.Value;
      }
    }

    /// <summary>
    /// Returns a delegate that can be directly invoked using a list of arguments.
    /// This flattens trees.
    /// </summary>
    public Delegate DelegateNoWarnings
    {
      get
      {
        return m_delegate_nowarnings.Value;
      }
    }

    /// <summary>
    /// Returns a delegate that can be directly invoked using a list of arguments.
    /// This considers trees and simplifies single-output components.
    /// </summary>
    public Delegate DelegateTree
    {
      get
      {
        return m_delegate_tree_warnings.Value;
      }
    }


    /// <summary>
    /// Returns a delegate that can be directly invoked using a list of arguments.
    /// This considers trees and simplifies single-output components.
    /// </summary>
    public Delegate DelegateTreeNoWarnings
    {
      get
      {
        return m_delegate_tree_nowarnings.Value;
      }
    }

    private static class ExpressionUtil
    {
      // Returns the MethodInfo of the given lambda. Use instead of Type.GetMethod("name", paramTypes).
      public static MethodInfo GetMethod<T, TResult>(Expression<Func<T, TResult>> lambda)
      {
        var call = lambda.Body as MethodCallExpression;
        if (call == null)
          throw new ArgumentException("Expression body was expected to be a method call, but was '" + lambda.Body + "' (" + call.GetType() + ")");
        return call.Method;
      }
    }
  }
}