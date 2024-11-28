[<RequireQualifiedAccess>]
module EA.Core.Persistence.Command

open Infrastructure
open Persistence.Domain
open EA.Core.Domain

module Request =
    type Operation =
        | Create of Request
        | CreateOrUpdate of Request
        | Update of Request
        | Delete of RequestId

    module internal InMemory =

        let create request (requests: External.Request array) =
            let requestExt = request |> EA.Core.Mapper.Request.toExternal

            match
                requests
                |> Seq.exists (fun x ->
                    x.Service.EmbassyId = requestExt.Service.EmbassyId
                    && x.Service.Payload = requestExt.Service.Payload)
            with
            | true ->
                Error
                <| Operation
                    { Message =
                        $"Request for {requestExt.Service.EmbassyName} with {requestExt.Service.Payload} already exists."
                      Code = Some ErrorCodes.ALREADY_EXISTS }
            | _ -> (requests |> Array.append [| requestExt |], request) |> Ok

        let update request (requests: External.Request array) =
            match requests |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
            | None ->
                Error
                <| Operation
                    { Message = $"{request.Id} not found to update."
                      Code = Some ErrorCodes.NOT_FOUND }
            | Some index ->
                let data =
                    requests
                    |> Array.mapi (fun i x ->
                        if i = index then
                            EA.Core.Mapper.Request.toExternal request
                        else
                            x)

                Ok(data, request)

        let delete requestId (requests: External.Request array) =
            match requestId with
            | RequestId requestId ->
                match requests |> Array.tryFindIndex (fun x -> x.Id = requestId) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{requestId} not found to delete."
                          Code = Some ErrorCodes.NOT_FOUND }
                | Some index ->
                    requests[index]
                    |> EA.Core.Mapper.Request.toInternal
                    |> Result.map (fun request ->
                        let data = requests |> Array.removeAt index
                        (data, request))

        let createOrUpdate request (requests: External.Request array) =
            match requests |> Array.tryFindIndex (fun x -> x.Id = request.Id.Value) with
            | None -> create request requests
            | Some _ -> update request requests
