[<AutoOpen>]
module EA.Worker.Attributes

open System
open System.Reflection

/// <summary>
/// Attribute to specify a name for worker task handlers, typically used as a key for Node.Root Id
/// </summary>
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property)>]
type NameAttribute(name: string) =
    inherit Attribute()
    
    /// The name associated with the handler
    member _.Name = name

/// <summary>
/// Utility functions for working with NameAttribute
/// </summary>
module AttributeHelpers =
    
    /// <summary>
    /// Extracts the name from a NameAttribute applied to a method or function
    /// </summary>
    /// <param name="methodInfo">The MethodInfo to check for NameAttribute</param>
    /// <returns>Some name if NameAttribute is found, None otherwise</returns>
    let tryGetName (methodInfo: MethodInfo) =
        methodInfo.GetCustomAttribute<NameAttribute>()
        |> Option.ofObj
        |> Option.map (fun attr -> attr.Name)
    
    /// <summary>
    /// Gets the name from NameAttribute or falls back to method name
    /// </summary>
    /// <param name="methodInfo">The MethodInfo to check</param>
    /// <returns>The name from attribute or method name as fallback</returns>
    let getNameOrDefault (methodInfo: MethodInfo) =
        tryGetName methodInfo
        |> Option.defaultValue methodInfo.Name
    
    /// <summary>
    /// Creates a Tree.NodeIdValue from a NameAttribute name
    /// </summary>
    /// <param name="methodInfo">The MethodInfo to extract name from</param>
    /// <returns>Some Tree.NodeIdValue if NameAttribute is found, None otherwise</returns>
    let tryGetNodeId (methodInfo: MethodInfo) =
        tryGetName methodInfo
        |> Option.map Infrastructure.Domain.Tree.NodeIdValue