open System
open Expecto
open EA.Russian.Tests

let private tests = testList "EA.Russian.Tests" [ Kdmid.tests ]

[<EntryPoint>]
let main args =
    Console.OutputEncoding <- Text.Encoding.UTF8
    runTestsWithCLIArgs [] args tests
