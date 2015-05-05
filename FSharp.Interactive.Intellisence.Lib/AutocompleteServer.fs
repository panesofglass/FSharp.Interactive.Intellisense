﻿namespace FSharp.Interactive.Intellisense.Lib

open System.Runtime.Remoting.Channels
open System.Runtime.Remoting
open System.Runtime.Remoting.Channels.Ipc
open System

(*
#r "C:\Users\Alexey\AppData\Local\Microsoft\VisualStudio\12.0Exp\Extensions\Aleksey Vlasenko\FSharp.Interactive.Intellisense\1.0\FSharp.Interactive.Intellisence.Lib.dll";;
FSharp.Interactive.Intellisense.Lib.AutocompleteServer.StartServer("channel");;
open FSharp.Interactive.Intellisense.Lib;;
*)
type AutocompleteServer() = 
    inherit AutocompleteService()
    override x.Test() = 5
    override x.GetBaseDirectory() = System.AppDomain.CurrentDomain.BaseDirectory
    
    static member StartServer(channelName : string) = 
        let channel = new IpcServerChannel("FSharp.Interactive.Intellisense.Lib")
        //Register the server channel.
        ChannelServices.RegisterChannel(channel, false)
        RemotingConfiguration.RegisterWellKnownServiceType
            (typeof<AutocompleteServer>, "AutocompleteService", WellKnownObjectMode.Singleton)
    
    static member StartClient(channelName) = 
        let channel = new IpcClientChannel()
        //Register the channel with ChannelServices.
        ChannelServices.RegisterChannel(channel, false)
        //Register the client type.
        RemotingConfiguration.RegisterWellKnownClientType
            (typeof<AutocompleteServer>, "ipc://FSharp.Interactive.Intellisense.Lib/AutocompleteService")
        //let T = Activator.GetObject(typeof<AutocompleteService>,"ipc://" + channelName + "/AutocompleteService") 
        let T = 
            Activator.GetObject
                (typeof<AutocompleteService>, "ipc://FSharp.Interactive.Intellisense.Lib/AutocompleteService")
        let x = T :?> AutocompleteService
        x