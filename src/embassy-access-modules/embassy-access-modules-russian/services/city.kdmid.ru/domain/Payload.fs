[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.Payload

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type Payload =
    { EmbassyId: Graph.NodeId
      SubDomain: string
      Id: int
      Cd: string
      Ems: string option }

    static member create(uri: Uri) =
        match uri.Host.Split '.' with
        | hostParts when hostParts.Length < 3 -> uri.Host |> NotSupported |> Error
        | hostParts ->
            let payload = ResultBuilder()

            payload {
                let subDomain = hostParts[0]

                let! embassyId =
                    match Constants.SUPPORTED_SUB_DOMAINS |> Map.tryFind subDomain with
                    | Some id -> id |> Graph.NodeIdValue |> Ok
                    | None -> subDomain |> NotSupported |> Error

                let! queryParams = uri |> Web.Http.Route.toQueryParams

                let! id =
                    queryParams
                    |> Map.tryFind "id"
                    |> Option.map (function
                        | AP.IsInt id when id > 1000 -> id |> Ok
                        | _ -> "id query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue ("id query parameter" |> NotFound |> Error)

                let! cd =
                    queryParams
                    |> Map.tryFind "cd"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers cd -> cd |> Ok
                        | _ -> "cd query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue ("cd query parameter" |> NotFound |> Error)

                let! ems =
                    queryParams
                    |> Map.tryFind "ems"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers ems -> ems |> Some |> Ok
                        | _ -> "ems query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue (None |> Ok)

                return
                    { EmbassyId = embassyId
                      SubDomain = subDomain
                      Id = id
                      Cd = cd
                      Ems = ems }
            }

    static member toValue(payload: string) =
        payload
        |> Web.Http.Route.toUri
        |> Result.bind Web.Http.Route.toQueryParams
        |> Result.map (Seq.rev >> Seq.map (fun x -> x.Key + "=" + x.Value) >> String.concat " ")
