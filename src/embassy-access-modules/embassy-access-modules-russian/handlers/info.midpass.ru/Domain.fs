module internal EA.Embassies.Russian.Midpass.Domain

open EA.Core.Domain

type Request = {
    Country: Country
    StatementNumber: string
}