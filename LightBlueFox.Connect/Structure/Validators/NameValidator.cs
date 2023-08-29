using System.Buffers.Binary;

namespace LightBlueFox.Connect.Structure.Validators
{
    /// <summary>
    /// A very basic implementation of a <see cref="ConnectionValidator"/> that uses an agreed upon string, which is then compared with the other end of the connection.
    /// </summary>
    public class NameValidator: ConnectionValidator, IEquatable<NameValidator?>
    {
        /// <summary>
        /// The agreed upon "code-word" to use to validate the connection.
        /// </summary>
        public readonly string CodeWord;

        #region Validator Implementation
        
        /// <summary>
        /// The challenge is simply a 4 byte hash of the <see cref="CodeWord"/>.
        /// </summary>
        public override byte[] GetChallengeBytes()
        {
            byte[] res = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(res, 0, 4), CodeWord.GetHashCode(StringComparison.Ordinal));
            return res;
        }

        /// <summary>
        /// The answer is the 4 byte hash of the reverse <see cref="CodeWord"/>. The string is reversed so that simply returning the challenge as an answer is not valid.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetAnswerBytes()
        {
            byte[] res = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(res), ReverseString(CodeWord).GetHashCode());
            return res;
        }

        public override bool ValidateChallenge(ReadOnlySpan<byte> challenge)
        {
            return challenge.SequenceEqual(GetChallengeBytes());
        }

        public override bool ValidateAnswer(ReadOnlySpan<byte> answer)
        {
            return answer.SequenceEqual(GetAnswerBytes());
        }
        #endregion

        #region Utils, Equality Overrides & GetHashCode
        /// <summary>
        /// Simple string reverse implementation
        /// </summary>
        private static string ReverseString(string s)
        {
            char[] chars = s.ToCharArray();
            Array.Reverse(chars);
            return new String(chars);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as NameValidator);
        }

        public bool Equals(NameValidator? other)
        {
            return other is not null &&
                   CodeWord == other.CodeWord;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CodeWord, "!!!");
        }

        public NameValidator(string appName)
        {
            this.CodeWord = appName;
        }

        public static bool operator ==(NameValidator? left, NameValidator? right)
        {
            return EqualityComparer<NameValidator>.Default.Equals(left, right);
        }

        public static bool operator !=(NameValidator? left, NameValidator? right)
        {
            return !(left == right);
        }

        #endregion
    }
}
