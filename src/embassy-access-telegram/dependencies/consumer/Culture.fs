[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open System.Threading
open System.Globalization
open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      getAvailableCultures: unit -> Async<Result<CultureInfo seq, Error'>>
      setCurrentCulture: string -> Async<Result<unit, Error'>>
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let getSystemCultures () =
                async {
                    try
                        return
                            CultureInfo.GetCultures(CultureTypes.AllCultures)
                            |> Seq.filter (fun c -> Constants.SUPPORTED_CULTURES.Contains c.Name)
                            |> Ok
                    with ex ->
                        return
                            Error
                            <| Operation
                                { Message = ex |> Exception.toMessage
                                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
                }
                
            let setCurrentCulture (code: string) =
                async {
                    try
                        Thread.CurrentThread.CurrentCulture <- CultureInfo.CreateSpecificCulture code
                        Thread.CurrentThread.CurrentUICulture <- CultureInfo.CreateSpecificCulture code
                        return Ok ()
                    with ex ->
                        return
                            Error
                            <| Operation
                                { Message = ex |> Exception.toMessage
                                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
                }

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  getAvailableCultures = getSystemCultures
                  setCurrentCulture = setCurrentCulture
                  sendResult = deps.sendResult }
        }
