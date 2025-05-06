module EA.Italian.Services.Prenotami.Web.Http

open System
open Web.Clients
open Web.Clients.Domain.Http

let private setCookie cookie httpClient =
    let headers = Map [ "Cookie", cookie ] |> Some
    httpClient |> Http.Headers.set headers

let setRequiredCookie httpClient (response: Response<string>) =
    response.Headers
    |> Http.Headers.tryFind "Set-Cookie" []
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
