[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.Payload

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients

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

                let! queryParams = uri |> Http.Route.toQueryParams

                let! id =
                    queryParams
                    |> Map.tryFind "id"
                    |> Option.map (function
                        | AP.IsInt id when id > 1000 -> id |> Ok
                        | _ -> "Kdmid payload 'ID' query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue ("Kdmid payload 'ID' query parameter" |> NotFound |> Error)

                let! cd =
                    queryParams
                    |> Map.tryFind "cd"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers cd -> cd |> Ok
                        | _ -> "Kdmid payload 'CD' query parameter" |> NotSupported |> Error)
                    |> Option.defaultValue ("Kdmid payload 'CD' query parameter" |> NotFound |> Error)

                let! ems =
                    queryParams
                    |> Map.tryFind "ems"
                    |> Option.map (function
                        | AP.IsLettersOrNumbers ems -> ems |> Some |> Ok
                        | _ -> "Kdmid payload 'EMS' query parameter" |> NotSupported |> Error)
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
        |> Http.Route.toUri
        |> Result.bind Http.Route.toQueryParams
        |> Result.map (Seq.rev >> Seq.map (fun x -> x.Key + "=" + x.Value) >> String.concat " ")
