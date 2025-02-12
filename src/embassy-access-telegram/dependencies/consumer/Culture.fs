[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open System.Globalization
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain

let private SupportedCultures = [ "en-US"; "ru-RU" ]

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      getSystemCultures: unit -> Async<Result<(string * string) seq, Error'>>
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let getSystemCultures () =
                async {
                    try
                        let cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                       |> Seq.filter (fun c -> SupportedCultures |> List.contains c.Name)
                        let result = cultures |> Seq.map (fun c -> (c.Name, c.DisplayName))
                        return Ok result
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
                  getSystemCultures = getSystemCultures
                  sendResult = deps.sendResult }
        }
