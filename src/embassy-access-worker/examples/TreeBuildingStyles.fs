module EA.Worker.Examples.TreeBuildingStyles

open Infrastructure.Prelude.Tree
open Infrastructure.Prelude.Tree.NodeBuilder

// Sample handler functions for demonstration
let sampleHandler1 = fun () -> "Handler1"
let sampleHandler2 = fun () -> "Handler2"

/// Demonstrates all the different functional tree building approaches
module TreeBuildingExamples =
    
    // Approach 1: Using standard pipe operator |> with helper functions
    let approach1_PipeOperator = 
        Tree.Node.Create("ROOT", Some sampleHandler1)
        |> withChildren [
            Tree.Node.Create("CHILD1", None)
            |> withChild (Tree.Node.Create("GRANDCHILD1", Some sampleHandler2))
            
            Tree.Node.Create("CHILD2", None)
            |> withChild (Tree.Node.Create("GRANDCHILD2", Some sampleHandler2))
        ]
    
    // Approach 2: Using custom operators ++ and +++
    let approach2_CustomOperators = 
        Tree.Node.Create("ROOT", Some sampleHandler1) +++ [
            Tree.Node.Create("CHILD1", None) ++ Tree.Node.Create("GRANDCHILD1", Some sampleHandler2)
            Tree.Node.Create("CHILD2", None) ++ Tree.Node.Create("GRANDCHILD2", Some sampleHandler2)
        ]
    
    // Approach 3: Using tree-specific operators |+ and |++
    let approach3_TreeOperators = 
        Tree.Node.Create("ROOT", Some sampleHandler1) |++ [
            Tree.Node.Create("CHILD1", None) |+ Tree.Node.Create("GRANDCHILD1", Some sampleHandler2)
            Tree.Node.Create("CHILD2", None) |+ Tree.Node.Create("GRANDCHILD2", Some sampleHandler2)
        ]
    
    // Approach 4: Using reverse operators (right-to-left composition)
    let approach4_ReverseOperators = 
        [
            Tree.Node.Create("GRANDCHILD1", Some sampleHandler2) +| Tree.Node.Create("CHILD1", None)
            Tree.Node.Create("GRANDCHILD2", Some sampleHandler2) +| Tree.Node.Create("CHILD2", None)
        ] ++| Tree.Node.Create("ROOT", Some sampleHandler1)
    
    // Approach 5: Using CreateWithChildren for concise nested syntax
    let approach5_NestedCreation = 
        Tree.Node.CreateWithChildren("ROOT", Some sampleHandler1, [
            Tree.Node.CreateWith("CHILD1", None, Tree.Node.Create("GRANDCHILD1", Some sampleHandler2))
            Tree.Node.CreateWith("CHILD2", None, Tree.Node.Create("GRANDCHILD2", Some sampleHandler2))
        ])
    
    // Approach 6: Using fluent method chaining
    let approach6_FluentChaining = 
        Tree.Node.Create("ROOT", Some sampleHandler1)
            .WithChildren([
                Tree.Node.Create("CHILD1", None)
                    .WithChild(Tree.Node.Create("GRANDCHILD1", Some sampleHandler2))
                
                Tree.Node.Create("CHILD2", None)
                    .WithChild(Tree.Node.Create("GRANDCHILD2", Some sampleHandler2))
            ])

/// Helper functions for demonstrating usage patterns
module UsagePatterns =
    
    // Pattern 1: Building with data transformation pipeline
    let buildFromData (items: (string * string option) list) =
        items
        |> List.map (fun (id, handlerName) -> 
            Tree.Node.Create(id, handlerName |> Option.map (fun _ -> sampleHandler1)))
        |> fun children -> Tree.Node.Create("ROOT", Some sampleHandler1) |> withChildren children
    
    // Pattern 2: Conditional tree building
    let buildConditional includeOptionalBranch =
        let mandatoryChildren = [
            Tree.Node.Create("MANDATORY1", Some sampleHandler1)
            Tree.Node.Create("MANDATORY2", Some sampleHandler2)
        ]
        
        let optionalChildren = 
            if includeOptionalBranch then 
                [Tree.Node.Create("OPTIONAL", None)]
            else []
        
        Tree.Node.Create("ROOT", None)
        |> withChildren (mandatoryChildren @ optionalChildren)
    
    // Pattern 3: Recursive tree building
    let rec buildRecursive depth maxDepth =
        if depth >= maxDepth then
            Tree.Node.Create($"LEAF_{depth}", Some sampleHandler2)
        else
            Tree.Node.Create($"NODE_{depth}", None)
            |> withChild (buildRecursive (depth + 1) maxDepth)

// Example usage demonstrating all approaches produce equivalent trees
let demonstrateEquivalence () =
    let printTree (node: Tree.Node<_>) = 
        // Simple tree structure printer for verification
        $"Tree: {node.Id} -> Children: {node.Children.Count}"
    
    [
        "Pipe Operator", TreeBuildingExamples.approach1_PipeOperator
        "Custom Operators", TreeBuildingExamples.approach2_CustomOperators  
        "Tree Operators", TreeBuildingExamples.approach3_TreeOperators
        "Reverse Operators", TreeBuildingExamples.approach4_ReverseOperators
        "Nested Creation", TreeBuildingExamples.approach5_NestedCreation
        "Fluent Chaining", TreeBuildingExamples.approach6_FluentChaining
    ]
    |> List.map (fun (name, tree) -> $"{name}: {printTree tree}")
    |> String.concat "\n"