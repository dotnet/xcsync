namespace xcsync.Projects;

interface ITypeService {
	TypeMapping? AddType (TypeMapping newType);
	IEnumerable<TypeMapping> QueryTypes (string? clrType, string? objcType);
}
