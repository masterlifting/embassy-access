module EmbassyAccess.Domain

open System
open Infrastructure

type City =
    | Belgrade
    | Berlin
    | Budapest
    | Sarajevo
    | Podgorica
    | Tirana
    | Paris
    | Rome
    | Dublin
    | Bern
    | Helsinki
    | Hague
    | Ljubljana

type Country =
    | Serbia of City
    | Germany of City
    | Bosnia of City
    | Montenegro of City
    | Albania of City
    | Hungary of City
    | Ireland of City
    | Switzerland of City
    | Finland of City
    | France of City
    | Netherlands of City
    | Slovenia of City

    member this.City =
        match this with
        | Serbia city -> city
        | Germany city -> city
        | Bosnia city -> city
        | Montenegro city -> city
        | Albania city -> city
        | Hungary city -> city
        | Ireland city -> city
        | Switzerland city -> city
        | Finland city -> city
        | France city -> city
        | Netherlands city -> city
        | Slovenia city -> city

type Embassy =
    | Russian of Country
    | Spanish of Country
    | Italian of Country
    | French of Country
    | German of Country
    | British of Country

    member this.Country =
        match this with
        | Russian country -> country
        | Spanish country -> country
        | Italian country -> country
        | French country -> country
        | German country -> country
        | British country -> country

type RequestId =
    | RequestId of Guid

    member this.Value =
        match this with
        | RequestId id -> id

    static member New = RequestId <| Guid.NewGuid()

type Confirmation = { Description: string }

type Appointment =
    { Value: string
      Date: DateOnly
      Time: TimeOnly
      Confirmation: Confirmation option
      Description: string option }

type ConfirmationOption =
    | FirstAvailable
    | Range of DateTime * DateTime
    | Appointment of Appointment

type RequestState =
    | Created
    | InProcess
    | Completed
    | Failed of Error'

type Request =
    { Id: RequestId
      Value: string
      Attempt: int
      State: RequestState
      Embassy: Embassy
      Appointments: Set<Appointment>
      Description: string option
      Modified: DateTime }

module External =

    type City() =
        [<Literal>]
        static let Belgrade = nameof City.Belgrade 

        [<Literal>]
        static let Berlin = nameof City.Berlin

        [<Literal>]
        static let Budapest = nameof City.Budapest

        [<Literal>]
        static let Sarajevo = nameof City.Sarajevo

        [<Literal>]
        static let Podgorica = nameof City.Podgorica

        [<Literal>]
        static let Tirana = nameof City.Tirana

        [<Literal>]
        static let Paris = nameof City.Paris

        [<Literal>]
        static let Rome = nameof City.Rome

        [<Literal>]
        static let Dublin = nameof City.Dublin

        [<Literal>]
        static let Bern = nameof City.Bern

        [<Literal>]
        static let Helsinki = nameof City.Helsinki

        [<Literal>]
        static let Hague = nameof City.Hague

        [<Literal>]
        static let Ljubljana = nameof City.Ljubljana

        member val Name: string = String.Empty with get, set

        static member fromDU city =
            let result = City()

            match city with
            | City.Belgrade -> result.Name <- Belgrade
            | City.Berlin -> result.Name <- Berlin
            | City.Budapest -> result.Name <- Budapest
            | City.Sarajevo -> result.Name <- Sarajevo
            | City.Podgorica -> result.Name <- Podgorica
            | City.Tirana -> result.Name <- Tirana
            | City.Paris -> result.Name <- Paris
            | City.Rome -> result.Name <- Rome
            | City.Dublin -> result.Name <- Dublin
            | City.Bern -> result.Name <- Bern
            | City.Helsinki -> result.Name <- Helsinki
            | City.Hague -> result.Name <- Hague
            | City.Ljubljana -> result.Name <- Ljubljana

            result

        member this.toDU() =
            match this.Name with
            | Belgrade -> City.Belgrade |> Ok
            | Berlin -> City.Berlin |> Ok
            | Budapest -> City.Budapest |> Ok
            | Sarajevo -> City.Sarajevo |> Ok
            | Podgorica -> City.Podgorica |> Ok
            | Tirana -> City.Tirana |> Ok
            | Paris -> City.Paris |> Ok
            | Rome -> City.Rome |> Ok
            | Dublin -> City.Dublin |> Ok
            | Bern -> City.Bern |> Ok
            | Helsinki -> City.Helsinki |> Ok
            | Hague -> City.Hague |> Ok
            | Ljubljana -> City.Ljubljana |> Ok
            | _ -> Error <| NotSupported $"City {this.Name}."

    type Country() =
        [<Literal>]
        static let Serbia = nameof Country.Serbia

        [<Literal>]
        static let Germany = nameof Country.Germany

        [<Literal>]
        static let Bosnia = nameof Country.Bosnia

        [<Literal>]
        static let Montenegro = nameof Country.Montenegro

        [<Literal>]
        static let Albania = nameof Country.Albania

        [<Literal>]
        static let Hungary = nameof Country.Hungary

        [<Literal>]
        static let Ireland = nameof Country.Ireland

        [<Literal>]
        static let Switzerland = nameof Country.Switzerland

        [<Literal>]
        static let Finland = nameof Country.Finland

        [<Literal>]
        static let France = nameof Country.France

        [<Literal>]
        static let Netherlands = nameof Country.Netherlands

        [<Literal>]
        static let Slovenia = nameof Country.Slovenia

        member val Name: string = String.Empty with get, set
        member val City: City = City() with get, set

        static member fromDU country =
            let result = Country()

            let city =
                match country with
                | Country.Serbia city ->
                    result.Name <- Serbia
                    city
                | Country.Germany city ->
                    result.Name <- Germany
                    city
                | Country.Bosnia city ->
                    result.Name <- Bosnia
                    city
                | Country.Montenegro city ->
                    result.Name <- Montenegro
                    city
                | Country.Albania city ->
                    result.Name <- Albania
                    city
                | Country.Hungary city ->
                    result.Name <- Hungary
                    city
                | Country.Ireland city ->
                    result.Name <- Ireland
                    city
                | Country.Switzerland city ->
                    result.Name <- Switzerland
                    city
                | Country.Finland city ->
                    result.Name <- Finland
                    city
                | Country.France city ->
                    result.Name <- France
                    city
                | Country.Netherlands city ->
                    result.Name <- Netherlands
                    city
                | Country.Slovenia city ->
                    result.Name <- Slovenia
                    city

            result.City <- City.fromDU city
            result

        member this.toDU() =
            this.City.toDU ()
            |> Result.bind (fun city ->
                match this.Name with
                | Serbia -> Country.Serbia city |> Ok
                | Germany -> Country.Germany city |> Ok
                | Bosnia -> Country.Bosnia city |> Ok
                | Montenegro -> Country.Montenegro city |> Ok
                | Albania -> Country.Albania city |> Ok
                | Hungary -> Country.Hungary city |> Ok
                | Ireland -> Country.Ireland city |> Ok
                | Switzerland -> Country.Switzerland city |> Ok
                | Finland -> Country.Finland city |> Ok
                | France -> Country.France city |> Ok
                | Netherlands -> Country.Netherlands city |> Ok
                | Slovenia -> Country.Slovenia city |> Ok
                | _ -> Error <| NotSupported $"Country {this.Name}.")

    type Embassy() =
        [<Literal>]
        static let Russian = nameof Embassy.Russian

        [<Literal>]
        static let Spanish = nameof Embassy.Spanish

        [<Literal>]
        static let Italian = nameof Embassy.Italian

        [<Literal>]
        static let French = nameof Embassy.French

        [<Literal>]
        static let German = nameof Embassy.German

        [<Literal>]
        static let British = nameof Embassy.British

        member val Name: string = String.Empty with get, set
        member val Country: Country = Country() with get, set

        static member fromDU embassy =
            let result = Embassy()

            let country =
                match embassy with
                | Embassy.Russian country ->
                    result.Name <- Russian
                    country
                | Embassy.Spanish country ->
                    result.Name <- Spanish
                    country
                | Embassy.Italian country ->
                    result.Name <- Italian
                    country
                | Embassy.French country ->
                    result.Name <- French
                    country
                | Embassy.German country ->
                    result.Name <- German
                    country
                | Embassy.British country ->
                    result.Name <- British
                    country

            result.Country <- Country.fromDU country
            result

        member this.toDU() =
            this.Country.toDU ()
            |> Result.bind (fun country ->
                match this.Name with
                | Russian -> Embassy.Russian country |> Ok
                | Spanish -> Embassy.Spanish country |> Ok
                | Italian -> Embassy.Italian country |> Ok
                | French -> Embassy.French country |> Ok
                | German -> Embassy.German country |> Ok
                | British -> Embassy.British country |> Ok
                | _ -> Error <| NotSupported $"Embassy {this.Name}.")

    type Confirmation() =
        member val Description: string = String.Empty with get, set

    type Appointment() =
        member val Value: string = String.Empty with get, set
        member val Confirmation: Confirmation option = None with get, set
        member val DateTime: DateTime = DateTime.UtcNow with get, set
        member val Description: string = String.Empty with get, set

    type RequestState() =

        [<Literal>]
        static let Created = nameof RequestState.Created

        [<Literal>]
        static let InProcess = nameof RequestState.InProcess

        [<Literal>]
        static let Completed = nameof RequestState.Completed

        [<Literal>]
        static let Failed = nameof RequestState.Failed

        member val Value: string = String.Empty with get, set
        member val Error: Domain.External.Error option = None with get, set

        static member fromDU state =
            let result = RequestState()

            match state with
            | RequestState.Created -> result.Value <- Created
            | RequestState.InProcess -> result.Value <- InProcess
            | RequestState.Completed -> result.Value <- Completed
            | RequestState.Failed error ->
                result.Value <- Failed
                result.Error <- Some <| Domain.External.Error.fromDU error

            result

        member this.toDU() =
            match this.Value with
            | Created -> RequestState.Created |> Ok
            | InProcess -> RequestState.InProcess |> Ok
            | Completed -> RequestState.Completed |> Ok
            | Failed ->
                match this.Error with
                | Some error -> error.toDU () |> Result.map RequestState.Failed
                | None -> Error <| NotSupported "Failed state without error"
            | _ -> Error <| NotSupported $"Request state {this.Value}."

    type Request() =
        member val Id: Guid = Guid.Empty with get, set
        member val Value: string = String.Empty with get, set
        member val Attempt: int = 0 with get, set

        member val State: RequestState =
            let state = RequestState()
            state.Value <- "Created"
            state with get, set

        member val Embassy: Embassy = Embassy() with get, set
        member val Appointments: Appointment array = [||] with get, set
        member val Description: string = String.Empty with get, set
        member val Modified: DateTime = DateTime.UtcNow with get, set
