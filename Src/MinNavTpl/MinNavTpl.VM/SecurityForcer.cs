namespace MinNavTpl.VM;

public class SecurityForcer : ISecurityForcer
{
  public SecurityForcer() : this(true, true) { }
  public SecurityForcer(bool isRead, bool isEdit) => (CanRead, CanEdit) = (isRead, isEdit);

  public bool CanRead { get; }
  public bool CanEdit { get; }

  public string PermisssionCSV => $"{(CanRead ? "Read+" : "")}{(CanEdit ? "Edit+" : "")}".Trim(new[] { '+', ' ' });

  public bool HasAccessTo(PermissionFlag ownedPermissions, PermissionFlag resource) => ownedPermissions.HasFlag(resource);
}
