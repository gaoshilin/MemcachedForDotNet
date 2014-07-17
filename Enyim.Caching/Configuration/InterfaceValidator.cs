using System;
using System.Configuration;

namespace Enyim.Caching.Configuration
{
    internal sealed class InterfaceValidator : ConfigurationValidatorBase
    {
        private readonly Type interfaceType;

        public InterfaceValidator(Type type)
        {
            if (!type.IsInterface)
                throw new ArgumentException(type + " must be an interface");

            interfaceType = type;
        }

        public override bool CanValidate(Type type)
        {
            return (type == typeof(Type)) || base.CanValidate(type);
        }

        public override void Validate(object value)
        {
            ConfigurationHelper.CheckForInterface((Type)value, interfaceType);
        }
    }

    internal sealed class InterfaceValidatorAttribute : ConfigurationValidatorAttribute
    {
        private readonly Type interfaceType;

        public InterfaceValidatorAttribute(Type type)
        {
            if (!type.IsInterface)
                throw new ArgumentException(type + " must be an interface");

            interfaceType = type;
        }

        public override ConfigurationValidatorBase ValidatorInstance
        {
            get { return new InterfaceValidator(interfaceType); }
        }
    }
}