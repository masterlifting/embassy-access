[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Filter

open System
open EmbassyAccess

type OrderBy<'a> =
    | Date of ('a -> DateTime)
    | String of ('a -> string)
    | Int of ('a -> int)
    | Bool of ('a -> bool)
    | Guid of ('a -> Guid)

type SortBy<'a> =
    | Asc of OrderBy<'a>
    | Desc of OrderBy<'a>

type predicate<'a> = 'a -> bool

type Pagination<'a> =
    { Page: int
      PageSize: int
      SortBy: SortBy<'a> }

type Request =
    { Pagination: Pagination<Domain.Request> option
      Ids: Domain.RequestId Set option
      Embassies: Domain.Embassy Set option
      HasAppointments: bool option
      HasConfirmations: bool option
      HasConfirmationState: predicate<Domain.ConfirmationState> option
      HasStates: predicate<Domain.RequestState> option
      WasModified: predicate<DateTime> option }

type SearchAppointmentsRequest =
    { Pagination: Pagination<Domain.Request>
      Embassy: Domain.Embassy
      HasStates: predicate<Domain.RequestState>
      HasConfirmationState: predicate<Domain.ConfirmationState>
      GroupBy: string }

    static member Create(embassy: Domain.Embassy) =
        { Pagination =
            { Page = 1
              PageSize = 20
              SortBy = OrderBy<Domain.Request>.Date _.Modified |> Asc }
          Embassy = embassy
          HasStates =
            function
            | Domain.InProcess -> false
            | _ -> true
          HasConfirmationState =
            function
            | Domain.Auto _ -> false
            | _ -> true
          GroupBy = "" }

type MakeAppointmentRequest =
    { Pagination: Pagination<Domain.Request>
      Embassy: Domain.Embassy
      HasStates: predicate<Domain.RequestState>
      HasConfirmationState: predicate<Domain.ConfirmationState> }
