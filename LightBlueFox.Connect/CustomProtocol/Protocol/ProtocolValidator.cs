using LightBlueFox.Connect.Structure.Validators;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// Checks whether two remotely connected <see cref="ProtocolDefinition"/>s are compatible.
    /// </summary>
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

        /// <summary>
        /// Creates a new validator from a complete list of all defined messages.
        /// </summary>
        /// <param name="definitions">A complete list of all defined messages for the <see cref="ProtocolDefinition"/>.</param>
        /// <exception cref="InvalidOperationException">Collection of message definitions may not be empty.</exception>
        public ProtocolValidator(MessageDefinition[] definitions)
        {
            var sorted = definitions.OrderBy((m) => m.ID);
            if (definitions.Length == 0) throw new InvalidOperationException("ProtocolDefinition without messages is meaningless.");

            using (MemoryStream ms = new MemoryStream())
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
