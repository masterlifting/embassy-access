module EA.Russian.Kdmid.Web.Http

open System
open Infrastructure.Domain
open Web.Clients
open Web.Clients.Domain.Http
open EA.Russian.Domain.Kdmid

let createQueryParams (credentials: Credentials) =
    match credentials.Ems with
    | Some ems -> $"id=%i{credentials.Id}&cd=%s{credentials.Cd}&ems=%s{ems}"
    | None -> $"id=%i{credentials.Id}&cd=%s{credentials.Cd}"

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
    |> FormData.build
