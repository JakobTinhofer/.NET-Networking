using System.Buffers.Binary;

namespace LightBlueFox.Connect.Structure.Validators
{
    public class NameValidator: ConnectionValidator, IEquatable<NameValidator?>
    {
        public readonly string AppName;

        private static string reverseString(string s)
        {
            char[] chars = s.ToCharArray();
            Array.Reverse(chars);
            return new String(chars);
        }

        public override byte[] GetChallengeBytes()
        {
            byte[] res = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(res, 0, 4), AppName.GetHashCode(StringComparison.Ordinal));
            return res;
        }

        public override byte[] GetAnswerBytes()
        {
            byte[] res = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(res), reverseString(AppName).GetHashCode());
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

        #region Equality Overrides & GetHashCode

        public override bool Equals(object? obj)
        {
            return Equals(obj as NameValidator);
        }

        public bool Equals(NameValidator? other)
        {
            return other is not null &&
                   AppName == other.AppName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AppName, "!!!");
        }

        public NameValidator(string appName)
        {
            this.AppName = appName;
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
