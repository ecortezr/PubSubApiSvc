namespace ApiService.Domain.Messages;

public record PermissionRecord (
    int Id,
    string Name
);

public enum NameOperationEnum
{
    modified,
    request,
    get
}

public record PermissionTopicMessage (
    Guid Id,
    NameOperationEnum NameOperation,
    PermissionRecord? Permission
);

