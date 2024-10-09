[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Query

open Persistence.Domain.Query
open EmbassyAccess

module Request =

    module Filter =
        type SearchAppointments =
            { Pagination: Pagination<Domain.Request>
              Embassy: Domain.Embassy
              HasStates: Predicate<Domain.ProcessState>
              HasConfirmationState: Predicate<Domain.ConfirmationState> }

            static member create embassy =
                { Pagination =
                    { Page = 1
                      PageSize = 20
                      SortBy = OrderBy<Domain.Request>.Date _.Modified |> Desc }
                  Embassy = embassy
                  HasStates =
                    function
                    | Domain.InProcess -> false
                    | _ -> true
                  HasConfirmationState =
                    function
                    | Domain.Auto _ -> false
                    | _ -> true }

        type MakeAppointment =
            { Pagination: Pagination<Domain.Request>
              Embassy: Domain.Embassy
              HasStates: Predicate<Domain.ProcessState>
              HasConfirmationStates: Predicate<Domain.ConfirmationState> }

            static member create embassy =
                { Pagination =
                    { Page = 1
                      PageSize = 20
                      SortBy = OrderBy<Domain.Request>.Date _.Modified |> Asc }
                  Embassy = embassy
                  HasStates =
                    function
                    | Domain.InProcess -> true
                    | _ -> false
                  HasConfirmationStates =
                    function
                    | Domain.Auto _ -> true
                    | _ -> false }

    type GetOne =
        | Id of Domain.RequestId
        | First
        | Single

    type GetMany =
        | SearchAppointments of Domain.Embassy
        | MakeAppointments of Domain.Embassy
