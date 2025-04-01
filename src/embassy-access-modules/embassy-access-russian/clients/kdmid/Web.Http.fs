module EA.Russian.Clients.Kdmid.Web.Http

open System
open Infrastructure.Domain
open Web.Clients
open Web.Clients.Domain.Http
open EA.Russian.Clients.Domain.Kdmid

let createQueryParams (payload: Payload) =
    match payload.Ems with
    | Some ems -> $"id=%i{payload.Id}&cd=%s{payload.Cd}&ems=%s{ems}"
    | None -> $"id=%i{payload.Id}&cd=%s{payload.Cd}"

let getQueryParamsId queryParams =
    queryParams
    |> Http.Route.fromQueryParams
    |> Result.map (Map.tryFind "id")
    |> Result.bind (function
        | Some id -> Ok id
        | None -> Error <| NotFound "Kdmid query parameter 'id'.")

let private setCookie cookie httpClient =
    let headers = Map [ "Cookie", cookie ] |> Some
    httpClient |> Http.Headers.set headers

let setRequiredCookie httpClient (response: Response<string>) =
    response.Headers
    |> Http.Headers.tryFind "Set-Cookie" [ "AlteonP"; "__ddg1_" ]
    |> Option.map (fun cookie -> httpClient |> setCookie cookie |> Result.map (fun _ -> response.Content))
    |> Option.defaultValue (Ok response.Content)

let setSessionCookie httpClient (response: Response<byte array>) =
    response.Headers
    |> Http.Headers.tryFind "Set-Cookie" [ "ASP.NET_SessionId" ]
    |> Option.map (fun cookie -> httpClient |> setCookie cookie |> Result.map (fun _ -> response.Content))
    |> Option.defaultValue (Ok response.Content)

let buildFormData data =
    data
    |> Map.add "__EVENTTARGET" ""
    |> Map.add "__EVENTARGUMENT" ""
    |> Seq.map (fun x -> $"{Uri.EscapeDataString x.Key}={Uri.EscapeDataString x.Value}")
    |> String.concat "&"
