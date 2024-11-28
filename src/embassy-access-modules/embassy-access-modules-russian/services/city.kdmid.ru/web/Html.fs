module internal EA.Embassies.Russian.Kdmid.Html

open Infrastructure
open Infrastructure.Parser
open EA.Embassies.Russian.Kdmid.Domain

let pageHasError page =
    page
    |> Html.getNode "//span[@id='ctl00_MainContent_lblCodeErr']"
    |> Result.bind (function
        | None -> Ok page
        | Some node ->
            match node.InnerText with
            | AP.IsString text ->
                Error
                <| Operation
                    { Message = text
                      Code = Some Constants.ErrorCode.PAGE_HAS_ERROR }
            | _ -> Ok page)
