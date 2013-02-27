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
            do
            {
                ExtendedAttribute ea = ExtendedAttribute.ParseData(data, (int) ResidentHeader.ContentLength, pointer);

                extendedAttributes.Add(ea);

                pointer +=(int) ea.Size;
            } while (pointer <= offset + maxLength);

            ExtendedAttributes = extendedAttributes.ToArray();
        }
    }
}