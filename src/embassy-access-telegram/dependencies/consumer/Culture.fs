[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: string -> Async<Result<unit, Error'>>
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let getAvailableCultures () = [ English, "English"; Russian, "Русский" ] |> Ok |> async.Return

            let setCurrentCulture (code: string) =
                async {
                    try
                        deps.ChatStorage
                        |> Chat.Command.create
                        return Ok()
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
                  getAvailableCultures = getAvailableCultures
                  setCurrentCulture = setCurrentCulture
                  sendResult = deps.sendResult }
        }
