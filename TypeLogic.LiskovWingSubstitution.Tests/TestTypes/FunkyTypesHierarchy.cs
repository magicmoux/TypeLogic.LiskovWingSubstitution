namespace TypeLogic.LiskovWingSubstitution.Tests.TestTypes
{
    public interface IEntityType
    {
    }

    public interface IGenericEntityType<T> : IEntityType
        where T : IEntityType
    {
    }

    public class EntityType
        : IEntityType
    {
    }

    public class SpecificEntityType
        : IGenericEntityType<EntityType>
    {
    }
}
