using System.ComponentModel;

namespace DapperDemo.Repository.Dapper;

public enum Response : byte
{
    [Description("An unknown error occurred.")]
    Failed = 0,

    [Description("Operation completed successfully.")]
    Successful = 1,

    [Description("The provided email address does not exist.")]
    EmailDoesNotExists = 2,

    [Description("The provided email address exists.")]
    EmailExists = 3,

    [Description("Incorrect password.")]
    WrongPassword = 4,

    [Description("An unexpected response occurred.")]
    Unknown = 99,
}