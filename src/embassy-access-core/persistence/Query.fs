[<RequireQualifiedAccess>]
module EA.Persistence.Query

open Persistence.Domain.Query
open EA.Core.Domain

module Filter =
    module Request =
        type SearchAppointments =
            { Pagination: Pagination<Request>
              Embassy: Embassy
              HasStates: Predicate<ProcessState>
              HasConfirmationState: Predicate<ConfirmationState> }

            static member create embassy =
                { Pagination =
                    { Page = 1
                      PageSize = 20
                      SortBy = OrderBy<Request>.Date _.Modified |> Desc }
                  Embassy = embassy
                  HasStates =
                    function
                    | InProcess -> false
                    | _ -> true
                  HasConfirmationState =
                    function
                    | Auto _ -> false
                    | _ -> true }

        type MakeAppointment =
            { Pagination: Pagination<Request>
              Embassy: Embassy
              HasStates: Predicate<ProcessState>
              HasConfirmationStates: Predicate<ConfirmationState> }

            static member create embassy =
                { Pagination =
                    { Page = 1
                      PageSize = 20
                      SortBy = OrderBy<Request>.Date _.Modified |> Asc }
                  Embassy = embassy
                  HasStates =
                    function
                    | InProcess -> true
                    | _ -> false
                  HasConfirmationStates =
                    function
                    | Auto _ -> true
                    | _ -> false }

        module InMemory =
            let searchAppointments (query: SearchAppointments) (request: Request) =
                query.Embassy = request.Service.Embassy
                && query.HasStates request.ProcessState
                && query.HasConfirmationState request.ConfirmationState

            let makeAppointment (query: MakeAppointment) (request: Request) =
                query.Embassy = request.Service.Embassy
                && query.HasStates request.ProcessState
                && query.HasConfirmationStates request.ConfirmationState

module Request =
    type GetOne =
        | Id of RequestId
        | First
        | Single

    type GetMany =
        | SearchAppointments of Embassy
        | MakeAppointments of Embassy
        | ByIds of Set<RequestId>
        | ByEmbassy of Embassy
