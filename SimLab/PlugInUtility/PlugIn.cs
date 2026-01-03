using SimLabApi;
using System.Reflection;

namespace SimLab.PlugInUtility;

public class PlugIn {
    /// <summary>
    /// Parses a plugInMethodPath in the format:
    ///   "<dll-filename>;<namespace-name>.<class-name>;<method-name>"
    /// where <namespace-name> is optional, e.g. the middle part can be just "<class-name>".
    /// 
    /// Examples of valid formats:
    ///   "ExamplePlugin.dll;PlugInNamespace.PlugInClass;PlugInMethod"
    ///   "ExamplePlugin.dll;PlugInClass;PlugInMethod"
    ///   "C:\\path\\to\\Plugin.dll;My.PlugIn.Namespace.PlugInClass;PlugInMethod"
    /// 
    /// Return values:
    ///   dllName    -> exactly as supplied (with or without path)
    ///   className  -> if a namespace exists, will be "Namespace.Class"; otherwise just "Class"
    ///   methodName -> the method name
    ///   error      -> in case of failure, an error message; null if parsing is successful
    /// </summary>
    /// <param name="plugInMethodPath">The full plug-in method path string to parse.</param>
    /// <param name="dllName">Output parameter for the DLL file name.</param>
    /// <param name="className">Output parameter for the class name (with or without namespace).</param>
    /// <param name="methodName">Output parameter for the method name.</param>
    /// <param name="error">Output parameter for error message in case of failure.</param>
    /// <returns>True if parsing is successful; false otherwise.</returns>
    public static bool ParseMethodPath( 
        string plugInMethodPath,
        out string dllName,
        out string className,
        out string methodName,
        out string? error) {

        dllName = string.Empty;
        className = string.Empty;
        methodName = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(plugInMethodPath)) {
            error = "Empty input parameter 'plugInMethodPath'.";
            return false;
        }

        // Remove outer quotes if the user passes the whole parameter in quotes
        // e.g. "Plugin.dll;Ns.Class;Method"
        plugInMethodPath = plugInMethodPath.Trim().Trim('"');

        // Expect exactly 3 parts: dll ; [namespace.]class ; method
        var parts = plugInMethodPath.Split(';');
        if (parts.Length != 3) {
            error = "Invalid format. Expected: <dll>;<namespace>.<class>;<method> (namespace is optional).";
            return false;
        }

        var dllPart = parts[0].Trim();
        var typePart = parts[1].Trim();
        var methodPart = parts[2].Trim();

        if (string.IsNullOrWhiteSpace(dllPart)) {
            error = "Missing DLL file name.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(typePart)) {
            error = "Missing class name (with or without namespace).";
            return false;
        }
        if (string.IsNullOrWhiteSpace(methodPart)) {
            error = "Missing method name.";
            return false;
        }

        // Parsing the class: "<namespace>.<class>" or just "<class>"
        // We take the LAST dot as the separator between the namespace and the class,
        // because the namespace may have multiple segments (e.g., A.B.C.MyClass).
        int lastDot = typePart.LastIndexOf('.');
        if (lastDot < 0) {
            // No namespace; the whole typePart is the class name
            className = typePart;
        } else {
            var ns = typePart.Substring(0, lastDot).Trim();
            var cls = typePart.Substring(lastDot + 1).Trim();

            if (string.IsNullOrWhiteSpace(cls)) {
                error = "Missing class name after the dot in the second segment.";
                return false;
            }

            className = string.IsNullOrWhiteSpace(ns) ? cls : $"{ns}.{cls}";
        }

        dllName = dllPart;
        methodName = methodPart;
        return true;
    }

    /// <summary>
    /// Retrieves the specified public static method from the given DLL and class.
    /// </summary>
    /// <param name="dllName">The DLL file name.</param>
    /// <param name="className">The class name (with or without namespace).</param>
    /// <param name="methodName">The method name.</param>   
    /// <param name="error">Output parameter for error message in case of failure.</param>
    /// <returns>The MethodInfo of the specified method if found; null otherwise.</returns>
    public static MethodInfo? GetMethod(
        string dllName, 
        string className, 
        string methodName,
        out string? error) {

        MethodInfo? pluginMethod = null;
        error = null;

        if (!File.Exists(dllName)) {
            error = $"DLL file '{dllName}' not found.";
            return null;
        }

        try {
            Assembly asm = Assembly.LoadFrom(dllName);
            Type? type = asm.GetType(className);
            if (type != null) {
                pluginMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (pluginMethod == null) {
                    error = $"Public static method '{methodName}' not found in DLL '{dllName}'.";
                    return null;
                }
            } else {
                error = $"Class '{className}' not found in DLL '{dllName}'.";
                return null;
            }
        } catch (Exception ex) {
            error = $"Error loading DLL: {ex.Message}";
            return null;
        }
        return pluginMethod;
    }

    /// <summary>
    /// Executes the specified plug-in method, passing the provided ISimLabApi instance.
    /// </summary>
    /// <param name="pluginMethod">The MethodInfo of the plug-in method to execute.</param>
    /// <param name="api">The ISimLabApi instance to pass to the plug-in method.</param>
    /// <param name="error">Output parameter for error message in case of failure.</param>
    /// <returns>True if execution is successful; false otherwise.</returns>    
    public static bool Execute(MethodInfo pluginMethod, ISimLabApi api, out string? error) {
        error = null;
        try {
            pluginMethod.Invoke(null, new object?[] { api });
        } catch (Exception ex) {
            error = ex.Message;
            return false;
        }
        return true;
    }
}
