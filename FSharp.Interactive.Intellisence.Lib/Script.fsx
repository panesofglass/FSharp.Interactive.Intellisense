﻿// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#load "Library1.fs"
open System
open System.Collections.Generic
open System.Reflection
//open FSharp.Interactive.Intellisense.Lib

// Define your library scripting code here


let getLastSegment (statement:String, line:String) = 
    let lastStatmentDotIndex = statement.LastIndexOf('.')
    let nextDotIndex = line.IndexOf('.', lastStatmentDotIndex + 1)
    if nextDotIndex > 0 then
        line.Substring(lastStatmentDotIndex + 1, nextDotIndex - (lastStatmentDotIndex + 1))
    else
        line.Substring(lastStatmentDotIndex + 1)

let getTypeCompletionsForAssembly(statement:String, assembly:Assembly) : IEnumerable<String> =
    let assemblyTypeNames = assembly.GetTypes()
                                |> Seq.filter(fun t -> t.IsPublic)
                                |> Seq.map (fun t -> if t.FullName.LastIndexOf('`') > 0 then t.FullName.Remove(t.FullName.LastIndexOf('`')) else t.FullName) 
                                |> Seq.filter(fun t -> t.StartsWith(statement))
                                |> Seq.map(fun n -> getLastSegment(statement, n))
                                |> Seq.distinct
    assemblyTypeNames

let removePropertyPrefix (memberName:String) =
    if memberName.StartsWith("get_") || memberName.StartsWith("set_") then 
        memberName.Remove(0, "get_".Length)
    else
        memberName

let fsiAssembly = 
    System.AppDomain.CurrentDomain.GetAssemblies() 
    |> Seq.find (fun assm -> assm.GetName().Name = "FSI-ASSEMBLY")

let fAssembly = 
    System.AppDomain.CurrentDomain.GetAssemblies() 
    |> Seq.find (fun assm -> assm.GetName().Name = "Fsi")

System.AppDomain.CurrentDomain.GetAssemblies()
|> Seq.map (fun assm -> assm.GetName().Name)
|> Seq.toList

fsiAssembly.GetReferencedAssemblies()
|> Seq.map (fun assm -> assm.FullName)
////|> Seq.filter(fun n -> n.ToLowerInvariant().Contains("dll"))
|> Seq.toList

#r @"C:\Projects\TorrentRT\packages\FSharp.Data.2.1.1\lib\net40\FSharp.Data.dll";;
open FSharp.Data;

type Rss = XmlProvider<"https://www.torrentz.com/feed?f=trailer">
let feed = Rss.Load("https://www.torrentz.com/feed?f=%D0%BF%D0%BE%D0%B1%D0%B5%D0%B3")

let getVariableNames() =
    fsiAssembly.GetTypes()//FSI types have the name pattern FSI_####, where #### is the order in which they were created
    |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_"))
    |> Seq.sortBy (fun ty -> ty.Name.Split('_').[1] |> int)
    |> Seq.collect (fun ty ->
        let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
        ty.GetProperties(flags) 
        |> Seq.filter (fun pi -> pi.GetIndexParameters().Length > 0  |> not && pi.Name.Contains("@") |> not))
    |> Seq.map (fun pi -> pi.Name)
    |> Seq.distinct

getVariableNames() |> Seq.toList;;

let getMethodNames() =
    fsiAssembly.GetTypes()//FSI types have the name pattern FSI_####, where #### is the order in which they were created
    |> Seq.filter (fun ty -> ty.Name.StartsWith("FSI_")) 
    |> Seq.collect (fun ty ->
        let flags = BindingFlags.Static ||| BindingFlags.NonPublic ||| BindingFlags.Public
        ty.GetMethods(flags) 
        |> Seq.filter (fun pi -> pi.Name.Contains("@") |> not && pi.Name.StartsWith("get_") |> not && pi.Name.StartsWith("set_") |> not))
    |> Seq.map (fun pi -> pi.Name)
    |> Seq.distinct
    |> Seq.toList

getMethodNames() |> Seq.toList;;



let fsharpcore =  fsiAssembly.GetReferencedAssemblies() |> Seq.head

fsiAssembly.GetReferencedAssemblies() |> Seq.map(fun a -> a.FullName) |> Seq.toList

AppDomain.CurrentDomain.GetAssemblies() 
                       |> Seq.filter(fun a -> a.GetName().FullName = fsharpcore.FullName) 
                       |> Seq.toList

let getCompletionsForTypes(statement:String) : IEnumerable<String> = seq {
    for assemblyName in fsiAssembly.GetReferencedAssemblies() do
        //printfn "%s" assemblyName.FullName
        let matches = AppDomain.CurrentDomain.GetAssemblies() 
                       |> Seq.filter(fun a -> a.GetName().FullName = assemblyName.FullName)
        if matches |> Seq.isEmpty |> not then
            let assembly = matches |> Seq.head
            let assemblyTypeNames = getTypeCompletionsForAssembly(statement, assembly) 
            yield! assemblyTypeNames

            if statement.LastIndexOf('.') > 0 then
                let typeName = statement.Remove(statement.LastIndexOf('.'))
                let type_ = assembly.GetType(typeName)
                if not(type_ = null) then
                    let memberNames = type_.GetMembers(BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy ||| BindingFlags.Public)
                                        |> Seq.map(fun m -> removePropertyPrefix m.Name)
                                        |> Seq.filter (fun name -> name.StartsWith(getLastSegment(statement, typeName + "." + name))) // <- bug here
                                        |> Seq.distinct
                    yield! memberNames
}

let getCompletions(statement:String) : IEnumerable<String> =
    seq {
        yield! getCompletionsForTypes(statement)

        if statement.LastIndexOf('.') < 0 then
            yield! getVariableNames() |> Seq.filter(fun n -> n.StartsWith(statement))
            yield! getMethodNames() |> Seq.filter(fun n -> n.StartsWith(statement))
    } 
    |> Seq.distinct

getCompletions("System.Console.Rea") |> Seq.toList // <- todo fix filtering by method name
// todo : remove add_ and remove_ prefixes




