﻿module Program

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq
open Fake.IO
open Fake.Core

let path xs = Path.Combine(Array.ofList xs)

let solutionRoot = Files.findParent __SOURCE_DIRECTORY__ "README.md";

let project name = path [ solutionRoot; $"Feliz.{name}" ]

let mocha = path [ solutionRoot; "src" ]
let tests = path [ solutionRoot; "tests" ]
let headlessRunner = path [ solutionRoot; "headless" ]

let publish projectDir =
    path [ projectDir; "bin" ] |> Shell.deleteDir
    path [ projectDir; "obj" ] |> Shell.deleteDir

    if Shell.Exec(Tools.dotnet, "pack --configuration Release", projectDir) <> 0 then
        failwithf "Packing '%s' failed" projectDir
    else
        let nugetKey =
            match Environment.environVarOrNone "NUGET_KEY" with
            | Some nugetKey -> nugetKey
            | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"

        let nugetPath =
            Directory.GetFiles(path [ projectDir; "bin"; "Release" ])
            |> Seq.head
            |> Path.GetFullPath

        if Shell.Exec(Tools.dotnet, sprintf "nuget push %s -s nuget.org -k %s" nugetPath nugetKey, projectDir) <> 0
        then failwith "Publish failed"

let dotnetExpectoTest() = 
    if Shell.Exec(Tools.dotnet, "run -c EXPECTO", tests) <> 0
    then failwith "Failed running tests using dotnet and Expecto"

let headlessTests() = 
    if Shell.Exec(Tools.npm, "run nagareyama-headless-tests", solutionRoot) <> 0
    then failwith "Headless tests failed :/"

[<EntryPoint>]
let main (args: string[]) = 
    Console.OutputEncoding <- System.Text.Encoding.UTF8
    try
        // run tasks
        match args with 
        | [| "publish-mocha" |] -> publish mocha
        | [| "publish-headless-runner" |] -> publish headlessRunner
        | [| "dotnet-test" |] -> dotnetExpectoTest()
        | [| "headless-tests" |] -> headlessTests()
        | _ -> printfn "Unknown args: %A" args
        
        // exit succesfully
        0
    with 
    | ex -> 
        // something bad happened
        printfn "Error occured"
        printfn "%A" ex
        1