using System;
using System.Reflection;

namespace Neueec.AssetUsagesTool.Addresssables
{
	public class UsageData
	{
		public string AddressablesKey;
		public bool IsFormattedKey;
		public Type Type;
		public FieldInfo Field;
		public AddressablesKeyAttribute Attribute;

		public UsageData(string addressablesKey, bool isFormattedKey, Type type, FieldInfo field, AddressablesKeyAttribute attribute)
		{
			AddressablesKey = addressablesKey;
			IsFormattedKey = isFormattedKey;
			Type = type;
			Field = field;
			Attribute = attribute;
		}

		public override string ToString()
		{
			return $"{Type.FullName} field {Field.Name}";
		}
	}
}
