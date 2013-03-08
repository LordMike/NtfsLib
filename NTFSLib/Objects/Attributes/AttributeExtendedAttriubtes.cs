using System.Collections.Generic;
using System.Diagnostics;
using NTFSLib.Objects.Enums;

namespace NTFSLib.Objects.Attributes
{
    public class AttributeExtendedAttriubtes : Attribute
    {
        public ExtendedAttribute[] ExtendedAttributes { get; set; }

        public override AttributeResidentAllow AllowedResidentStates
        {
            get
            {
                return AttributeResidentAllow.Resident;
            }
        }

        internal override void ParseAttributeResidentBody(byte[] data, int maxLength, int offset)
        {
            base.ParseAttributeResidentBody(data, maxLength, offset);

            Debug.Assert(maxLength >= 8);

            List<ExtendedAttribute> extendedAttributes = new List<ExtendedAttribute>();
            int pointer = offset;
            while (pointer + 8 <= offset + maxLength)       // 8 is the minimum size of an ExtendedAttribute
            {
                if (ExtendedAttribute.GetSize(data, pointer) < 0)
                    break;

                ExtendedAttribute ea = ExtendedAttribute.ParseData(data, (int) ResidentHeader.ContentLength, pointer);

                extendedAttributes.Add(ea);

                pointer +=ea.Size;
            }

            ExtendedAttributes = extendedAttributes.ToArray();
        }
    }
}