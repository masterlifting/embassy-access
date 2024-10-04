[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Filter

open System
open EmbassyAccess

type internal OrderBy<'a> =
    | Date of ('a -> DateTime)
    | String of ('a -> string)
    | Int of ('a -> int)
    | Bool of ('a -> bool)
    | Guid of ('a -> Guid)

type internal SortBy<'a> =
    | Asc of OrderBy<'a>
    | Desc of OrderBy<'a>

type internal predicate<'a> = 'a -> bool

type internal Pagination<'a> =
    { Page: int
      PageSize: int
      SortBy: SortBy<'a> }

type internal SearchAppointmentsRequest =
    { Pagination: Pagination<Domain.Request>
      Embassy: Domain.Embassy
      HasStates: predicate<Domain.RequestState>
      HasConfirmationState: predicate<Domain.ConfirmationState> }

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
      HasStates: predicate<Domain.RequestState>
      HasConfirmationStates: predicate<Domain.ConfirmationState> }

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

module Telegram =

  type Chat =
    | Search of Domain.RequestId
