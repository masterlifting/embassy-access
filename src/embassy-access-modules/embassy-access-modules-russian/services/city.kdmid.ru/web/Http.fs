module internal EA.Embassies.Russian.Kdmid.Web.Http

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open Web.Clients.Domain.Http
open EA.Embassies.Russian.Kdmid.Domain

let private createKdmidClient subDomain =

    let hostName = $"%s{subDomain}.kdmid.ru"
    let host = $"https://%s{hostName}"

    let headers =
        Map
            [ "Host", [ hostName ]
              "Origin", [ host ]
              "Accept",
              [ "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/png,image/svg+xml,*/*;q=0.8" ]
              "Accept-Language", [ "en-US,en;q=0.9,ru;q=0.8" ]
              "Connection", [ "keep-alive" ]
              "Sec-Fetch-Dest", [ "document" ]
              "Sec-Fetch-Mode", [ "navigate" ]
              "Sec-Fetch-Site", [ "same-origin" ]
              "Sec-Fetch-User", [ "?1" ]
              "Upgrade-Insecure-Requests", [ "1" ]
              "User-Agent", [ "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:129.0) Gecko/20100101 Firefox/129.0" ] ]
        |> Some


    { Host = host; Headers = headers } |> Http.Client.init

let createClient =
    ResultAsync.bind (fun (payload, request: EA.Core.Domain.Request.Request) ->
        payload.SubDomain
        |> createKdmidClient
        |> Result.map (fun httpClient -> httpClient, payload, request))

let createQueryParams id cd ems =
    match ems with
    | Some ems -> $"id=%i{id}&cd=%s{cd}&ems=%s{ems}"
    | None -> $"id=%i{id}&cd=%s{cd}"

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
