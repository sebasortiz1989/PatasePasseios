using System.ComponentModel;

namespace DapperDemo.Repository.Dapper;

/// <summary>
/// The outcome of an operation, returned instead of throwing for anything the user can cause.
/// </summary>
/// <remarks>
/// The <see cref="DescriptionAttribute"/> text is <b>user-facing</b>: <c>EnumExtensions.GetDescription()</c>
/// puts it straight on the sign-in and sign-up screens. It is therefore written in Brazilian
/// Portuguese, like every other string the user reads, while the member names stay English like
/// the rest of the code.
/// </remarks>
public enum Response : byte
{
    [Description("Não foi possível concluir. Tente de novo.")]
    Failed = 0,

    [Description("Tudo certo.")]
    Successful = 1,

    [Description("Não encontramos uma conta com esse e-mail.")]
    EmailDoesNotExists = 2,

    [Description("Já existe uma conta com esse e-mail.")]
    EmailExists = 3,

    [Description("Senha incorreta.")]
    WrongPassword = 4,

    [Description("Este backup foi criado por outra versão do aplicativo.")]
    IncompatibleVersion = 5,

    [Description("Algo inesperado aconteceu. Tente de novo.")]
    Unknown = 99,
}