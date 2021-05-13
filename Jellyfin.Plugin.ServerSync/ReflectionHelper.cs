using System;
using System.Reflection;

namespace Jellyfin.Plugin.ServerSync
{
    public class ReflectionHelper
    {
        public FieldInfo getPrivateFieldFromExternalAssembly(string fieldName, string fullClassName, string assemblyName)
        {
            Type searchedType = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    searchedType = a.GetType(fullClassName);
                    break;
                }
            }

            if (searchedType == null)
                return null;

            return searchedType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
