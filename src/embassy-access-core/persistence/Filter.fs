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
      HasAppointments: bool
      HasConfirmations: bool
      WithAutoConfirmation: bool
      WithManualConfirmation: bool
      HasStates: predicate<Domain.RequestState> option
      WasModified: predicate<DateTime> option }
