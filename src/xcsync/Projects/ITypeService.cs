namespace xcsync.Projects;

interface ITypeService {
	TypeMapping? AddType (TypeMapping newType);
	IEnumerable<TypeMapping> QueryTypes (string? cliType, string? objcType);
}
