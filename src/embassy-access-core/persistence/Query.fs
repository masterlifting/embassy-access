[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Query

open Persistence.Domain.Query
open EmbassyAccess

type internal SearchAppointmentsRequest =
    { Pagination: Pagination<Domain.Request>
      Embassy: Domain.Embassy
      HasStates: Predicate<Domain.RequestState>
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

type internal MakeAppointmentRequest =
    { Pagination: Pagination<Domain.Request>
      Embassy: Domain.Embassy
      HasStates: Predicate<Domain.RequestState>
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

type Request =
    | SearchAppointments of Domain.Embassy
    | MakeAppointments of Domain.Embassy
