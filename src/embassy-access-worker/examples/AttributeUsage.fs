module EA.Worker.Examples.AttributeUsage

open EA.Worker.Attributes
open System.Reflection

/// Example of how to extract the name from a NameAttribute
let extractNameExample () =
    
    // Example function with NameAttribute
    [<Name("ExampleHandler")>]
    let exampleFunction () = ()
    
    // Get the method info
    let methodInfo = typeof<unit -> unit>.GetMethod("exampleFunction")
    
    // Extract the name using the helper function
    let nameFromAttribute = AttributeHelpers.tryGetName methodInfo
    
    match nameFromAttribute with
    | Some name -> printfn "Found name from attribute: %s" name
    | None -> printfn "No NameAttribute found"
    
    // Get NodeId from attribute
    let nodeIdFromAttribute = AttributeHelpers.tryGetNodeId methodInfo
    
    match nodeIdFromAttribute with
    | Some nodeId -> printfn "NodeId from attribute: %s" nodeId.Value
    | None -> printfn "No NodeId available from attribute"

// Example of creating a handler automatically from attribute
let createHandlerFromAttribute (handleFunction: 'a) =
    let methodInfo = handleFunction.GetType().GetMethod("Invoke")
    let name = AttributeHelpers.getNameOrDefault methodInfo
    let nodeId = name |> Infrastructure.Domain.Tree.NodeIdValue
    
    {|
        Id = nodeId
        Name = name
        Handler = handleFunction
    |}