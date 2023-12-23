using LightBlueFox.Connect.CustomProtocol.Serialization;
using LightBlueFox.Connect.Structure.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public class ProtocolValidator : ConnectionValidator
    {
        private readonly byte[] MessageDefinitionHash;

        public override byte[] GetAnswerBytes() => MessageDefinitionHash.Reverse().ToArray();
        

        public override byte[] GetChallengeBytes() => MessageDefinitionHash;

        public override bool ValidateAnswer(ReadOnlySpan<byte> answer)
        {
            return answer.SequenceEqual(MessageDefinitionHash.Reverse().ToArray());
        }

        public override bool ValidateChallenge(ReadOnlySpan<byte> challenge)
        {
            return challenge.SequenceEqual(MessageDefinitionHash);
        }

        public ProtocolValidator(MessageDefinition[] definitions)
        {
            var sorted = definitions.OrderBy((m) => m.ID);


            if (definitions.Length == 0) throw new InvalidOperationException("ProtocolDefinition without messages is meaningless.");

            using(MemoryStream ms = new MemoryStream())
            {
                foreach (var def in sorted)
                {
                    ms.WriteByte(def.ID);
                    ms.Write(def.MessageTypeID, 0, 4);
                }
                MessageDefinitionHash = ms.ToArray();
            }
        }
    }
}
