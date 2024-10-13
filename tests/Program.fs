open System
open Expecto
open EA.Embassies

let private tests = testList "Tests" [ Russian.Test.list ]

[<EntryPoint>]
let main args =
    Console.OutputEncoding <- Text.Encoding.UTF8
    runTestsWithCLIArgs [] args tests
