using System;

namespace Enyim.Caching.Configuration
{
	internal static class ConfigurationHelper
	{
		public static void CheckForInterface(Type type, Type interfaceType)
		{
			if (Array.IndexOf(type.GetInterfaces(), interfaceType) == -1)
				throw new System.Configuration.ConfigurationErrorsException("The type " + type.AssemblyQualifiedName + " must implement " + interfaceType.AssemblyQualifiedName);
		}
	}
}
