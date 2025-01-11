using Unity.Entities;

namespace EntityJobs
{
    public static class SystemExtensions
    {
        public static Entity? GetEntityViaReflection(this in SystemHandle systemHandle)
        {
            var systemHandleEntityField = typeof(SystemHandle)
                .GetField("m_Entity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (systemHandleEntityField == null) return null;
            
            var entity = (Entity)systemHandleEntityField.GetValue(systemHandle);
            return entity;
        }
    }
}