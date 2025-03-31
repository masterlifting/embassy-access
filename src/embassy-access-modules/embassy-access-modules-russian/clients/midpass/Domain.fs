module EA.Russian.Clients.Domain.Midpass

open System
open System.Collections.Concurrent
open Web.Clients.Domain

type Client = Http.Client
type ClientFactory = ConcurrentDictionary<string, Client>

type Dependencies = { Number: string;}
