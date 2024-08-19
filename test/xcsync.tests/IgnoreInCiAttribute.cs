public class IgnoreInCiFactAttribute : FactAttribute
{
	public IgnoreInCiFactAttribute()
	{
#if ContinuousIntegrationBuild
			Skip = "Ignored in CI";
#endif
	}
}